using MG.CamCtrl;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Wpf_RunVision.Models;
using Wpf_RunVision.Services.Plc;
using Wpf_RunVision.Utils;

namespace Wpf_RunVision.Services
{
    /// <summary>
    /// 核心视觉服务类
    /// 1️⃣ 管理多相机初始化/释放
    /// 2️⃣ 支持顺序拍照（软触发）
    /// 3️⃣ 支持硬触发模式采图，触发回调入队
    /// 4️⃣ 支持生产者-消费者模式处理拍摄的图片队列
    /// </summary>
    public class VisionCoreService
    {
        #region 字段

        // 多相机实例列表
        private readonly List<CameraInstance> _cameraInstances = new List<CameraInstance>();

        // PLC服务接口
        private readonly IPlcService _plcService;

        // 拍摄完成的图片队列（线程安全）
        private readonly ConcurrentQueue<Bitmap> _imageQueue = new ConcurrentQueue<Bitmap>();

        // 生产者取消令牌
        private CancellationTokenSource _hardTriggerCts;

        private ProjectConfigs _projectConfigs;


        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化核心视觉服务
        /// </summary>
        /// <param name="projectConfigs">项目配置，包括相机列表和PLC配置</param>
        public VisionCoreService(ProjectConfigs projectConfigs)
        {
            _projectConfigs = projectConfigs;
            if (_projectConfigs.CamerasConfigs == null || _projectConfigs.CamerasConfigs.Count == 0)
            {
                MyLogger.Error("相机配置列表为空，无法初始化！");
                return;
            }
            if (_projectConfigs.PlcConfig == null || string.IsNullOrEmpty(_projectConfigs.PlcConfig.Ip) || string.IsNullOrEmpty(_projectConfigs.PlcConfig.Port))
            {
                MyLogger.Error("PLC配置为空，无法初始化！");
                return;
            }

            // 初始化相机
            foreach (var item in projectConfigs.CamerasConfigs)
            {
                try
                {
                    CameraBrand brand;
                    switch (item.Brand)
                    {
                        case "海康相机": brand = CameraBrand.HIK; break;
                        case "大恒相机": brand = CameraBrand.DaHeng; break;
                        default: throw new NotSupportedException($"不支持的相机品牌：{item.Brand}");
                    }

                    ICamera camera = CamFactory.CreatCamera(brand);
                    bool initResult = camera.InitDevice(item.Sn);
                    MyLogger.Info($"相机[{item.Sn}]初始化结果：{initResult}");

                    if (initResult)
                    {
                        _cameraInstances.Add(new CameraInstance
                        {
                            CameraSN = item.Sn,
                            CameraBrand = item.Brand,
                            PlcAddress = item.PlcAddress,
                            SdkInstance = camera
                        });
                    }
                    else
                    {
                        camera.CloseDevice();
                        CamFactory.DestroyCamera(camera);
                        MyLogger.Error($"相机[{item.Sn}]初始化失败");
                    }
                }
                catch (Exception ex)
                {
                    MyLogger.Error($"相机[{item.Sn}]初始化异常：{ex.Message}");
                    MessageBox.Show($"相机[{item.Sn}]初始化失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            try
            {
                _plcService = PlcFactory.Create(_projectConfigs.PlcConfig.Brand);
                MainViewState.Instance.PlcStatus =_plcService.Connect(_projectConfigs.PlcConfig.Ip, int.Parse(_projectConfigs.PlcConfig.Port));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"PLC 初始化失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region 资源释放

        /// <summary>
        /// 释放所有相机资源
        /// </summary>
        public void DestroyAllCameras()
        {
            foreach (var instance in _cameraInstances)
            {
                try
                {
                    instance.SdkInstance?.CloseDevice();
                    CamFactory.DestroyCamera(instance.SdkInstance);
                }
                catch (Exception ex)
                {
                    MyLogger.Error($"释放相机[{instance.CameraSN}]异常：{ex.Message}");
                }
            }
            _cameraInstances.Clear();
        }

        #endregion

        #region 硬触发模式 生产者线程

        /// <summary>
        /// 启动所有相机硬触发
        /// 相机每次触发拍照，回调入队
        /// </summary>
        public void StartHardTriggerAll()
        {
            if (_hardTriggerCts != null)
                return; // 已经启动

            _hardTriggerCts = new CancellationTokenSource();

            foreach (var cameraInstance in _cameraInstances)
            {
                try
                {
                    cameraInstance.SdkInstance.StartWith_HardTriggerModel(TriggerSource.Line0, (bitmap) =>
                    {
                        if (_hardTriggerCts.IsCancellationRequested)
                            return;

                        if (bitmap == null)
                        {
                            MyLogger.Error($"硬触发相机[{cameraInstance.CameraSN}]采图失败");
                            return;
                        }

                        // 入队
                        _imageQueue.Enqueue(bitmap);

                        //写PLC完成信号
                        if (!string.IsNullOrWhiteSpace(cameraInstance.PlcAddress))
                        {
                            _plcService.Write(cameraInstance.PlcAddress, "0");
                            MyLogger.Info($"PLC完成信号写入：{cameraInstance.PlcAddress}");
                        }
                    });

                    MyLogger.Info($"相机[{cameraInstance.CameraSN}]已启动硬触发(Line0)");
                }
                catch (Exception ex)
                {
                    MyLogger.Error($"相机[{cameraInstance.CameraSN}]启动硬触发异常：{ex.Message}");
                }
            }
        }

        #endregion

        #region 消费者线程

        /// <summary>
        /// 启动后台消费者处理图片队列
        /// </summary>
        /// <param name="processImageAction">处理图片的委托方法</param>
        /// <param name="cancellationToken">取消令牌</param>
        public void StartConsumer(Action<Bitmap> processImageAction, CancellationToken cancellationToken = default)
        {
            Task.Run(() =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (_imageQueue.TryDequeue(out var item))
                    {
                        try
                        {
                            processImageAction?.Invoke(item);

                        }
                        catch (Exception ex)
                        {
                            MyLogger.Error($"图片处理异常：{ex.Message}");
                        }
                    }
                    else
                    {
                        try
                        {
                            Task.Delay(10, cancellationToken).Wait();
                        }
                        catch { } // 捕获取消异常
                    }
                }
            }, cancellationToken);
        }

        #endregion

        #region 内部类

        /// <summary>
        /// 相机实例封装类
        /// 包含相机SN、品牌、PLC地址和SDK实例
        /// </summary>
        private class CameraInstance
        {
            public string CameraSN { get; set; }
            public string CameraBrand { get; set; }
            public string PlcAddress { get; set; }
            public ICamera SdkInstance { get; set; }
        }

        #endregion
    }
}
