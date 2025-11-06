using HandyControl.Controls;
using ImageSourceModuleCs;
using MG.CamCtrl;
using NLog;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using VM.Core;
using VM.PlatformSDKCS;
using Wpf_RunVision.Models;
using Wpf_RunVision.Services.Mysql;
using Wpf_RunVision.Services.Plc;
using Wpf_RunVision.Utils;

public class VisionServiceManager : IDisposable
{
    private readonly List<ICamera> _cameraInstances = new List<ICamera>();
    private readonly ConcurrentQueue<Bitmap> _imageQueue = new ConcurrentQueue<Bitmap>();

    private IPlcService _plcService;
    private CancellationTokenSource _cts;
    private Task _consumerTask;
    private ProjectConfigs _projectConfigs;

    public bool IsInitialized { get; private set; } = false;
    public bool PlcStatus { get; set; }
    public bool DatabaseStatus { get; set; }
    public bool NasStatus { get; set; }

    private int RunImageIndex = 0;
    private bool _disposed = false;

    public VisionServiceManager()
    {
        _projectConfigs = ProjectConfigHelper.Instance?.CurrentConfigs;
        if (_projectConfigs == null)
        {
            MyLogger.Error("ProjectConfigHelper 初始化失败，无法获取配置！");
            return;
        }

        if (!ValidateConfig())
        {
            MyLogger.Error("VisionServiceManager 配置验证失败，初始化未完成！");
            return;
        }
    }

    #region 初始化逻辑（异步后台）
    public async Task InitializeAsync()
    {
        if (_projectConfigs == null)
        {
            MyLogger.Error("配置为空，无法初始化！");
            return;
        }

        try
        {
            MyLogger.Info("后台初始化服务开始...");
            InitializeCameras();
            InitializePlc();
            await InitializeDatabaseAsync();
            NasStatus = NasConnectionChecker.Instance.IsConnectedAsync(_projectConfigs.ImageSaveModel.ImageSavePath);
            MyLogger.Info(NasStatus ? "NAS连接成功！" : "NAS连接失败！");

            IsInitialized = DatabaseStatus && PlcStatus && _cameraInstances.Any() && _plcService != null;
            if (!IsInitialized)
            {
                MyLogger.Error($"核心组件初始化未全部成功 - 数据库：{DatabaseStatus}，PLC：{PlcStatus}，相机数量：{_cameraInstances.Count}");
            }
            else
            {
                MyLogger.Info("VisionServiceManager 初始化完成！");
            }
        }
        catch (Exception ex)
        {
            MyLogger.Error($"InitializeAsync 异常：{ex.Message}", ex);
            IsInitialized = false;
        }
    }
    #endregion

    #region 配置验证（增强校验）
    private bool ValidateConfig()
    {
        if (_projectConfigs.SolutionConfig == null)
        {
            MyLogger.Error("解决方案配置为空，无法初始化！");
            return false;
        }
        if (_projectConfigs.CamerasConfigs?.Count == 0)
        {
            MyLogger.Error("相机配置列表为空，无法初始化！");
            return false;
        }
        if (string.IsNullOrEmpty(_projectConfigs.PlcConfig?.Ip) || string.IsNullOrEmpty(_projectConfigs.PlcConfig?.Port))
        {
            MyLogger.Error("PLC IP或端口配置为空，无法初始化！");
            return false;
        }
        if (string.IsNullOrEmpty(_projectConfigs.DatabaseConfig?.Ip) || string.IsNullOrEmpty(_projectConfigs.DatabaseConfig?.Port))
        {
            MyLogger.Error("数据库 IP或端口配置为空，无法初始化！");
            return false;
        }
        if (string.IsNullOrEmpty(_projectConfigs.ImageSaveModel?.ImageSavePath))
        {
            MyLogger.Error("存图路径配置为空！");
            return false;
        }
        return true;
    }
    #endregion

    #region 组件初始化（优化异步/空判）
    private async Task InitializeDatabaseAsync()
    {
        if (!int.TryParse(_projectConfigs.DatabaseConfig.Port, out int port))
        {
            MyLogger.Error($"数据库端口配置无效：{_projectConfigs.DatabaseConfig.Port}");
            DatabaseStatus = false;
            return;
        }

        if (MySqlDataService.Instance == null)
        {
            MyLogger.Error("MySqlDataService 实例创建失败！");
            DatabaseStatus = false;
            return;
        }

        DatabaseStatus = await Task.Run(() =>
            MySqlDataService.Instance.Initialize(
                _projectConfigs.DatabaseConfig.Ip,
                port,
                _projectConfigs.DatabaseConfig.LibraryName,
                "root",
                _projectConfigs.DatabaseConfig.Password)
        ).ConfigureAwait(false);
    }

    private void InitializePlc()
    {
        if (!int.TryParse(_projectConfigs.PlcConfig.Port, out int port))
        {
            MyLogger.Error($"PLC端口配置无效：{_projectConfigs.PlcConfig.Port}");
            PlcStatus = false;
            return;
        }

        _plcService = PlcFactory.Create(_projectConfigs.PlcConfig.Brand);
        if (_plcService == null)
        {
            MyLogger.Error($"不支持的PLC品牌：{_projectConfigs.PlcConfig.Brand}");
            PlcStatus = false;
            return;
        }

        PlcStatus = _plcService.Connect(_projectConfigs.PlcConfig.Ip, port);
    }

    private void InitializeCameras()
    {
        foreach (var item in _projectConfigs.CamerasConfigs)
        {
            try
            {
                if (string.IsNullOrEmpty(item.Sn) || string.IsNullOrEmpty(item.Brand))
                {
                    MyLogger.Error($"相机配置无效（SN/品牌缺失）：SN={item.Sn}，品牌={item.Brand}");
                    continue;
                }

                CameraBrand brand = item.Brand switch
                {
                    "海康相机" => CameraBrand.HIK,
                    "大恒相机" => CameraBrand.DaHeng,
                    _ => throw new ArgumentException($"不支持的相机品牌：{item.Brand}")
                };

                var camera = CamFactory.CreatCamera(brand);
                if (camera == null)
                {
                    MyLogger.Error($"相机[{item.Sn}]创建失败！");
                    continue;
                }

                bool initResult = camera.InitDevice(item.Sn);
                MyLogger.Info($"相机[{item.Sn}]初始化结果：{initResult}");

                if (!initResult) continue;

                // 相机硬触发回调：直接异步写PLC（不阻塞回调线程）
                camera.StartWith_HardTriggerModel(TriggerSource.Line0, async bmp =>
                {
                    if (bmp == null)
                    {
                        MyLogger.Warn($"相机[{item.Sn}]收到空图像，跳过入队");
                        return;
                    }

                    _imageQueue.Enqueue(bmp);
                    //MyLogger.Debug($"相机[{item.Sn}] 收到图像（索引：{RunImageIndex}），已入队，队列剩余：{_imageQueue.Count}");

                    // 直接异步写PLC，不等待结果（避免阻塞回调）
                    if (!string.IsNullOrEmpty(item.PlcAddress) && _plcService != null && _plcService.IsConnected)
                    {
                        try
                        {
                            // 异步调用，不await，避免阻塞相机回调线程
                            var writeTask = _plcService.WriteAsync(item.PlcAddress, "1");
                            // 可选：捕获任务异常（不影响主线程）
                            _ = writeTask.ContinueWith(t =>
                            {
                                if (t.Exception != null)
                                {
                                    MyLogger.Error($"相机[{item.Sn}] PLC写操作失败：{t.Exception.InnerException?.Message}", t.Exception);
                                }
                                else
                                {
                                    MyLogger.Debug($"相机[{item.Sn}] PLC写操作成功：地址={item.PlcAddress}，值=1");
                                }
                            }, TaskContinuationOptions.OnlyOnFaulted);
                        }
                        catch (Exception ex)
                        {
                            MyLogger.Error($"相机[{item.Sn}] PLC写操作异常：{ex.Message}", ex);
                        }
                    }
                    else
                    {
                        MyLogger.Warn($"相机[{item.Sn}] PLC地址为空或未连接，跳过写操作");
                    }
                });

                _cameraInstances.Add(camera);
            }
            catch (Exception ex)
            {
                MyLogger.Error($"相机[{item.Sn}]初始化失败：{ex.Message}", ex);
            }
        }

        MyLogger.Info($"相机初始化完成，成功启动相机数量：{_cameraInstances.Count}");
    }
    #endregion

    #region 服务启停（移除队列相关逻辑）
    public Task StartAsync()
    {
        if (!IsInitialized)
        {
            MyLogger.Warn("VisionServiceManager 未初始化，无法启动！");
            return Task.CompletedTask;
        }

        if (_cts != null && !_cts.IsCancellationRequested)
        {
            MyLogger.Warn("VisionServiceManager 已处于运行状态，无需重复启动！");
            return _consumerTask ?? Task.CompletedTask;
        }

        _cts = new CancellationTokenSource();
        _consumerTask = ConsumerTask(_cts.Token);
        MyLogger.Info("VisionServiceManager 已启动（硬触发 + 回调模式）");
        return _consumerTask;
    }

    public void Stop()
    {
        MyLogger.Info("VisionServiceManager 开始停止...");

        _cts?.Cancel();

        // 等待任务终止（超时1秒）
        try
        {
            var tasks = new List<Task>();
            if (_consumerTask != null && !_consumerTask.IsCompleted) tasks.Add(_consumerTask);
            if (tasks.Any()) Task.WaitAll(tasks.ToArray(), 1000);
        }
        catch (AggregateException ex)
        {
            MyLogger.Warn($"任务停止过程中出现异常：{ex.Message}");
        }

        // 仅清空图像队列，移除PLC写队列清空逻辑
        while (_imageQueue.TryDequeue(out var img))
        {
            img.Dispose();
        }

        Dispose();
        MyLogger.Info("VisionServiceManager 已停止");
    }
    #endregion

    #region 图像消费任务（优化逻辑+异常防护）
    private async Task ConsumerTask(CancellationToken token)
    {
        MyLogger.Info("图像消费者任务已启动...");

        while (!token.IsCancellationRequested)
        {
            try
            {
                if (_imageQueue.TryDequeue(out var item))
                {
                    await ProcessImageAsync(item, token).ConfigureAwait(false);
                }
                else
                {
                    await Task.Delay(3, token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                MyLogger.Info("图像消费者任务被取消");
                break;
            }
            catch (Exception ex)
            {
                MyLogger.Error($"图像消费者异常：{ex.Message}", ex);
                await Task.Delay(100, token).ConfigureAwait(false);
            }
        }

        MyLogger.Info("图像消费者任务已退出");
    }

    private async Task ProcessImageAsync(Bitmap image, CancellationToken token)
    {
        if (image == null)
        {
            MyLogger.Warn("收到空图像，跳过处理");
            return;
        }

        try
        {
            MyLogger.Debug($"队列剩余：{_imageQueue.Count}，当前图像索引：{RunImageIndex}");

            var flowSteps = _projectConfigs.SolutionConfig.FlowSteps ?? new List<FlowStepModel>();
            if (!flowSteps.Any())
            {
                MyLogger.Warn("无流程步骤配置，跳过图像处理");
                return;
            }

            var scanStep = flowSteps.FirstOrDefault(step => !string.IsNullOrEmpty(step.StepName) && step.StepName.Contains("扫码"));
            if (scanStep != null)
            {
                var scanIndexes = ParseImageIndex(scanStep.ImageIndex);
                if (scanIndexes.Contains(RunImageIndex))
                {
                    await RunScanAsync(image, token).ConfigureAwait(false);
                    return;
                }
            }

            var detectSteps = flowSteps
                .Where(step => !string.IsNullOrEmpty(step.StepName) && !step.StepName.Contains("扫码"))
                .ToList();

            foreach (var step in detectSteps)
            {
                if (token.IsCancellationRequested) break;
                await RunDetectFlowAsync(step, image, token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            MyLogger.Info("图像处理任务被取消");
        }
        catch (Exception ex)
        {
            MyLogger.Error($"图像处理异常（索引：{RunImageIndex}）：{ex.Message}", ex);
        }
        finally
        {
            if (image != null)
            {
                image.Dispose();
            }
        }
    }
    #endregion

    #region 扫码流程（增强空判+取消支持）
    private async Task RunScanAsync(Bitmap bim, CancellationToken token)
    {
        await Task.Run(() =>
        {
            token.ThrowIfCancellationRequested();

            try
            {
                MyLogger.Info($"开始扫码流程（图像索引：{RunImageIndex}）...");

                if (VmSolution.Instance == null)
                {
                    MyLogger.Error("VmSolution 实例为空，无法执行扫码流程");
                    return;
                }

                VmProcedure pro = VmSolution.Instance["扫码"] as VmProcedure;
                ImageSourceModuleTool imageSourceTool = VmSolution.Instance["扫码.图像源1"] as ImageSourceModuleTool;
                if (pro == null || imageSourceTool == null)
                {
                    MyLogger.Error("扫码流程：VmProcedure或ImageSourceModuleTool获取失败");
                    return;
                }

                imageSourceTool.ModuParams.ImageSourceType = ImageSourceParam.ImageSourceTypeEnum.SDK;
                ImageBaseData image = BitmapToImageBaseData(bim);
                imageSourceTool.SetImageData(image);

                pro.Run();
                List<VmDynamicIODefine.IoNameInfo> ioNames = pro.ModuResult.GetAllOutputNameInfo();
                if (ioNames.Count == 0)
                {
                    MyLogger.Error("扫码流程：VM IO输出项个数为0");
                    return;
                }

                foreach (var ioName in ioNames)
                {
                    token.ThrowIfCancellationRequested();

                    var outputString = pro.ModuResult.GetOutputString(ioName.Name);
                    if (outputString.astStringVal == null || !outputString.astStringVal.Any())
                    {
                        MyLogger.Warn($"扫码流程：IO项[{ioName.Name}]无有效扫码结果");
                        continue;
                    }

                    string codeResult = outputString.astStringVal[0].strValue?.ToString() ?? "null";
                    MyLogger.Info($"扫码结果（IO项：{ioName.Name}）：{codeResult}");
                }
            }
            catch (OperationCanceledException)
            {
                MyLogger.Info("扫码流程被取消");
                throw;
            }
            catch (Exception ex)
            {
                MyLogger.Error("扫码失败：" + ex.Message, ex);
            }
        }, token).ConfigureAwait(false);
    }
    #endregion

    #region 检测流程（修复using用法+资源安全）
    private async Task RunDetectFlowAsync(FlowStepModel step, Bitmap bim, CancellationToken token)
    {
        var indexes = ParseImageIndex(step.ImageIndex);
        if (!indexes.Contains(RunImageIndex))
            return;

        await Task.Run(() =>
        {
            token.ThrowIfCancellationRequested();

            try
            {
                MyLogger.Info($"{step.StepName} 开始处理，PCS = {step.Pcs}，图像索引：{RunImageIndex}");

                if (VmSolution.Instance == null)
                {
                    MyLogger.Error($"{step.StepName}：VmSolution 实例为空");
                    return;
                }

                VmProcedure pro = VmSolution.Instance[step.StepName] as VmProcedure;
                ImageSourceModuleTool imageSourceTool = VmSolution.Instance[$"{step.StepName}.图像源1"] as ImageSourceModuleTool;
                if (pro == null || imageSourceTool == null)
                {
                    MyLogger.Error($"{step.StepName}：VmProcedure或ImageSourceModuleTool获取失败");
                    return;
                }

                imageSourceTool.ModuParams.ImageSourceType = ImageSourceParam.ImageSourceTypeEnum.SDK;
                ImageBaseData image = BitmapToImageBaseData(bim);
                imageSourceTool.SetImageData(image);

                pro.Run();
                List<VmDynamicIODefine.IoNameInfo> ioNames = pro.ModuResult.GetAllOutputNameInfo();
                if (ioNames.Count == 0)
                {
                    MyLogger.Error($"{step.StepName} - VM IO输出项个数为0");
                    return;
                }

                for (int pcsIndex = 0; pcsIndex <= step.Pcs; pcsIndex++)
                {
                    token.ThrowIfCancellationRequested();

                    using (var plcImage = GetPlcImage(pro, pcsIndex, step.StepName))
                    {
                        if (plcImage == null)
                        {
                            MyLogger.Warn($"{step.StepName} - 索引{pcsIndex}：未获取到有效图像，跳过");
                            continue;
                        }

                        var outputString = pro.ModuResult.GetOutputString($"out{pcsIndex}").ToString();
                        if (string.IsNullOrEmpty(outputString))
                        {
                            MyLogger.Error($"{step.StepName} - 索引{pcsIndex}：字符串输出为空！");
                            continue;
                        }

                        var resultList = outputString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(str =>
                                str.Equals("OK", StringComparison.OrdinalIgnoreCase) ? "0" :
                                str.Equals("NG", StringComparison.OrdinalIgnoreCase) ? "1" : "1"
                            )
                            .ToList();

                        MyLogger.Debug($"{step.StepName} -> 第 {pcsIndex}/{step.Pcs} 个PCS处理完毕，结果：{string.Join(',', resultList)}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                MyLogger.Info($"{step.StepName} 流程被取消");
                throw;
            }
            catch (Exception ex)
            {
                MyLogger.Error($"{step.StepName} 流程异常：{ex.Message}", ex);
            }
        }, token).ConfigureAwait(false);
    }

    private Bitmap GetPlcImage(VmProcedure pro, int pcsIndex, string stepName)
    {
        try
        {
            var outputImage = pro.ModuResult.GetOutputImageV2($"image{pcsIndex}");
            if (outputImage == null)
            {
                MyLogger.Error($"{stepName} - 索引{pcsIndex}：图像输出为空！");
                return null;
            }

            var bitmap = outputImage.ToBitmap();
            if (bitmap == null || bitmap.Width <= 0 || bitmap.Height <= 0)
            {
                MyLogger.Error($"{stepName} - 索引{pcsIndex}：图像转换为Bitmap失败，图像无效！");
                bitmap?.Dispose();
                return null;
            }

            return bitmap;
        }
        catch (Exception ex)
        {
            MyLogger.Error($"{stepName} - 索引{pcsIndex}：图像获取/转换失败：{ex.Message}", ex);
            return null;
        }
    }
    #endregion

    #region 辅助方法（核心修复：BitmapData 不支持 using）
    private List<int> ParseImageIndex(string? imageIndex)
    {
        if (string.IsNullOrEmpty(imageIndex))
            return new List<int>();

        return imageIndex
            .Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(s => int.TryParse(s.Trim(), out _))
            .Select(s => int.Parse(s.Trim()))
            .ToList();
    }

    public static ImageBaseData BitmapToImageBaseData(Bitmap bmp)
    {
        if (bmp == null) throw new ArgumentNullException(nameof(bmp), "输入Bitmap不能为空");

        ImageBaseData img = new ImageBaseData();
        img.Width = (int)(uint)bmp.Width;
        img.Height = (int)(uint)bmp.Height;

        // 8位灰度图处理（修复：BitmapData 用 try-finally 释放锁定）
        if (bmp.PixelFormat == PixelFormat.Format8bppIndexed)
        {
            img.Pixelformat = (int)(uint)VMPixelFormat.VM_PIXEL_MONO_08;
            BitmapData bmpData = null;

            try
            {
                bmpData = bmp.LockBits(
                    new Rectangle(0, 0, bmp.Width, bmp.Height),
                    ImageLockMode.ReadOnly,
                    PixelFormat.Format8bppIndexed);

                int stride = bmpData.Stride;
                int dataLen = stride * bmp.Height;
                byte[] buffer = new byte[dataLen];
                Marshal.Copy(bmpData.Scan0, buffer, 0, dataLen);

                byte[] compact = new byte[bmp.Width * bmp.Height];
                for (int i = 0; i < bmp.Height; i++)
                {
                    Buffer.BlockCopy(buffer, i * stride, compact, i * bmp.Width, bmp.Width);
                }

                img.ImageData = compact;
                img.DataLen = (uint)compact.Length;
            }
            finally
            {
                if (bmpData != null)
                {
                    bmp.UnlockBits(bmpData);
                }
            }
        }
        // 24位RGB图处理（同理修复 BitmapData using 问题）
        else if (bmp.PixelFormat == PixelFormat.Format24bppRgb)
        {
            img.Pixelformat = (int)(uint)VMPixelFormat.VM_PIXEL_RGB24_C3;
            BitmapData bmpData = null;

            try
            {
                bmpData = bmp.LockBits(
                    new Rectangle(0, 0, bmp.Width, bmp.Height),
                    ImageLockMode.ReadOnly,
                    PixelFormat.Format24bppRgb);

                int stride = bmpData.Stride;
                int dataLen = stride * bmp.Height;
                byte[] buffer = new byte[dataLen];
                Marshal.Copy(bmpData.Scan0, buffer, 0, dataLen);

                byte[] compact = new byte[bmp.Width * bmp.Height * 3];
                for (int y = 0; y < bmp.Height; y++)
                {
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        int bmpIdx = y * stride + x * 3;
                        if (bmpIdx + 2 >= buffer.Length)
                        {
                            MyLogger.Warn($"图像数据不完整：x={x}, y={y}，索引超出范围");
                            continue;
                        }

                        int vmIdx = (y * bmp.Width + x) * 3;
                        compact[vmIdx] = buffer[bmpIdx + 2];
                        compact[vmIdx + 1] = buffer[bmpIdx + 1];
                        compact[vmIdx + 2] = buffer[bmpIdx];
                    }
                }

                img.ImageData = compact;
                img.DataLen = (uint)compact.Length;
            }
            finally
            {
                if (bmpData != null)
                {
                    bmp.UnlockBits(bmpData);
                }
            }
        }
        else
        {
            throw new NotSupportedException($"不支持的像素格式：{bmp.PixelFormat}，仅支持 Format8bppIndexed 和 Format24bppRgb");
        }

        return img;
    }
    #endregion

    #region 标准IDisposable实现（优化资源释放）
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _cts?.Cancel();

            foreach (var cam in _cameraInstances)
            {
                try
                {
                    cam?.CloseDevice();
                    cam?.Dispose();
                }
                catch (Exception ex)
                {
                    MyLogger.Error($"相机释放失败：{ex.Message}", ex);
                }
            }
            _cameraInstances.Clear();

            try
            {
                if (_plcService != null)
                {
                    _plcService.Disconnect();
                    if (_plcService is IDisposable plcDisposable)
                    {
                        plcDisposable.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                MyLogger.Error($"PLC资源释放异常：{ex.Message}", ex);
            }

            try
            {
                MySqlDataService.Instance?.Dispose();
            }
            catch (Exception ex)
            {
                MyLogger.Error($"数据库资源释放异常：{ex.Message}", ex);
            }

            _cts?.Dispose();
        }

        _disposed = true;
    }

    ~VisionServiceManager()
    {
        Dispose(false);
    }
    #endregion
}