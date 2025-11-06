using Basler.Pylon;
using MG.CamCtrl;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Wpf_RunVision.Services.Mysql;
using Wpf_RunVision.Services.Plc;
using Wpf_RunVision.Utils;
using ICamera = MG.CamCtrl.ICamera;

namespace Wpf_RunVision.Services.Core
{
    public class VisionServiceManager : IDisposable
    {
        // 多相机实例列表
        private List<ICamera> _cameraInstances = new List<ICamera>();

        // PLC服务接口
        private IPlcService _plcService;

        // 拍摄完成的图片队列（线程安全）
        private readonly ConcurrentQueue<Bitmap> _imageQueue = new ConcurrentQueue<Bitmap>();

        // 生产者取消令牌
        private CancellationTokenSource _hardTriggerCts;

        private ProjectConfigs projectConfigs;

        public VisionServiceManager()
        {
            // 传入配置参数
            projectConfigs = ProjectConfigHelper.Instance.CurrentConfigs;

            // 配置验证
            if (!ValidateConfig()) return;

            // 实例化各服务
            实例化相机();
            实例化PLC();
            实例化数据库();
        }

        // 配置验证
        private bool ValidateConfig()
        {
            if (projectConfigs.CamerasConfigs == null || projectConfigs.CamerasConfigs.Count == 0)
            {
                MyLogger.Error("相机配置列表为空，无法初始化！");
                return false;
            }
            if (projectConfigs.PlcConfig == null || string.IsNullOrEmpty(projectConfigs.PlcConfig.Ip) || string.IsNullOrEmpty(projectConfigs.PlcConfig.Port))
            {
                MyLogger.Error("PLC配置为空，无法初始化！");
                return false;
            }
            if (projectConfigs.DatabaseConfig == null || string.IsNullOrEmpty(projectConfigs.DatabaseConfig.Ip) || string.IsNullOrEmpty(projectConfigs.DatabaseConfig.Port)
                || string.IsNullOrEmpty(projectConfigs.DatabaseConfig.LibraryName) || string.IsNullOrEmpty(projectConfigs.DatabaseConfig.CodeTableName)
                || string.IsNullOrEmpty(projectConfigs.DatabaseConfig.DataTableName))
            {
                MyLogger.Error("数据库配置为空，无法初始化！");
                return false;
            }
            if (projectConfigs.ImageSaveModel == null || string.IsNullOrEmpty(projectConfigs.ImageSaveModel.OkImageSavePath)
                || string.IsNullOrEmpty(projectConfigs.ImageSaveModel.NgImageSavePath))
            {
                MyLogger.Error("存图配置为空，无法初始化！");
                return false;
            }
            return true;
        }

        // 实例化数据库
        private void 实例化数据库()
        {
            MySqlDataService.Instance.Initialize(projectConfigs.DatabaseConfig.Ip, int.Parse(projectConfigs.DatabaseConfig.Port), projectConfigs.DatabaseConfig.LibraryName, "root", projectConfigs.DatabaseConfig.Password);
        }

        // 实例化PLC
        private void 实例化PLC()
        {
            _plcService = PlcFactory.Create(projectConfigs.PlcConfig.Brand);
            _plcService.Connect(projectConfigs.PlcConfig.Ip, int.Parse(projectConfigs.PlcConfig.Port));
        }

        // 异步实例化相机
        private void 实例化相机()
        {
            foreach (var item in projectConfigs.CamerasConfigs)
            {
                try
                {
                    // 日志打印配置的相机品牌
                    MyLogger.Info($"初始化相机：品牌 = {item.Brand}, SN = {item.Sn}");

                    CameraBrand brand = item.Brand switch
                    {
                        "海康相机" => CameraBrand.HIK,
                        "大恒相机" => CameraBrand.DaHeng,
                        _ => throw new ArgumentException($"不支持的相机品牌：{item.Brand}")
                    };

                    ICamera camera = CamFactory.CreatCamera(brand);

                    if (camera == null)
                    {
                        MyLogger.Error($"相机[{item.Sn}]创建失败：相机对象为空");
                        return;
                    }

                    bool initResult = camera.InitDevice(item.Sn);

                    // 检查初始化结果并记录日志
                    MyLogger.Info($"相机[{item.Sn}]初始化结果：{initResult}");

                    if (initResult)
                    {
                        _cameraInstances.Add(camera);
                    }
                    else
                    {
                        MyLogger.Error($"相机[{item.Sn}]初始化失败：设备初始化返回 false");
                    }
                }
                catch (Exception e)
                {
                    MyLogger.Error($"相机[{item.Sn}]初始化失败：{e.Message}");
                }
            }

        }

        // 停止并释放资源
        public void Stop()
        {
            _hardTriggerCts?.Cancel();
            Dispose();
        }

        // 释放资源
        public void Dispose()
        {
            foreach (var camera in _cameraInstances)
            {
                if (camera != null)  // 确保相机实例已初始化
                {
                    try
                    {
                        camera.Dispose();
                        camera.CloseDevice();
                        CamFactory.DestroyCamera(camera);
                    }
                    catch (Exception ex)
                    {
                        MyLogger.Error($"发生错误：{ex.Message}");
                    }
                }
            }

            _plcService?.Disconnect();
        }

        // 生产者任务：获取PLC硬触发信号
        private async Task ProducerTask(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                // 获取PLC硬触发信号，处理拍摄任务
                var image = await CaptureImageAsync(); // 模拟相机拍摄
                _imageQueue.Enqueue(image); // 将图像加入队列

                await Task.Delay(100); // 模拟等待时间
            }
        }

        // 消费者任务：处理图像
        private async Task ConsumerTask()
        {
            while (true)
            {
                if (_imageQueue.TryDequeue(out var image))
                {
                    await ProcessImageAsync(image); // 处理图像
                }
                await Task.Delay(10); // 延迟，防止CPU占用过高
            }
        }

        // 模拟图像捕获
        private Task<Bitmap> CaptureImageAsync()
        {
            // 这里是模拟相机拍照，实际项目中需要调用相机的捕获接口
            return Task.FromResult(new Bitmap(100, 100)); // 示例返回空白图像
        }

        // 模拟图像处理
        private Task ProcessImageAsync(Bitmap image)
        {
            // 这里是图像处理逻辑，例如图像保存、上传等
            return Task.CompletedTask;
        }
    }
}
