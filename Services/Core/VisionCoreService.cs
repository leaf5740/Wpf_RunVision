using MG.CamCtrl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Wpf_RunVision.Models;
using Wpf_RunVision.Utils;

namespace Wpf_RunVision.Services
{
    /// <summary>
    /// 视觉核心服务：管理多相机初始化、拍照与PLC交互（兼容.NET 4.7.1）
    /// </summary>
    public class VisionCoreService
    {
        private readonly List<CameraInstance> _cameraInstances = new List<CameraInstance>();

        /// <summary>
        /// 相机实例封装：关联配置与SDK实例
        /// </summary>
        private class CameraInstance
        {
            public CameraModels Config { get; set; }
            public ICamera SdkInstance { get; set; }
        }

        /// <summary>
        /// 构造函数：初始化所有配置的相机
        /// </summary>
        public VisionCoreService(List<CameraModels> cameraModels)
        {
            if (cameraModels == null || !cameraModels.Any())
            {
                MessageBox.Show("相机配置列表为空，无法初始化", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            foreach (var item in cameraModels)
            {
                try
                {
                    CameraBrand brand;
                    switch (item.Brand)
                    {
                        case "海康相机":
                            brand = CameraBrand.HIK;
                            break;
                        case "大恒相机":
                            brand = CameraBrand.DaHeng;
                            break;
                        default:
                            throw new NotSupportedException($"不支持的相机品牌：{item.Brand}");
                    }

                    // 创建并初始化相机
                    ICamera camera = CamFactory.CreatCamera(brand);
                    bool initResult = camera.InitDevice(item.Sn);
                    MyLogger.Info($"相机[{item.Sn}]初始化结果：{initResult}");

                    if (initResult)
                    {
                        _cameraInstances.Add(new CameraInstance
                        {
                            Config = item,
                            SdkInstance = camera
                        });
                    }
                    else
                    {
                        MyLogger.Error($"相机[{item.Sn}]初始化失败");
                        camera.CloseDevice();
                        CamFactory.DestroyCamera(camera);
                    }
                }
                catch (Exception ex)
                {
                    MyLogger.Error($"相机[{item.Sn}]初始化异常：{ex.Message}");
                    MessageBox.Show($"相机[{item.Sn}]初始化失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// 释放所有相机资源
        /// </summary>
        public void DestroyAllCameras()
        {
            foreach (var instance in _cameraInstances)
            {
                instance.SdkInstance?.CloseDevice();
                CamFactory.DestroyCamera(instance.SdkInstance);
            }
            _cameraInstances.Clear();
        }
    }
}