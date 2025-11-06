using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MG.CamCtrl;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Wpf_RunVision.Models;
using Wpf_RunVision.Utils;

namespace Wpf_RunVision.ViewModels.TabViewModels
{
    public partial class CameraTabViewModel : ObservableObject
    {
        /// <summary>
        /// 相机品牌列表（绑定第一个 ComboBox）
        /// </summary>
        public ObservableCollection<string> CameraBrands { get; } = new()
        {
            "海康相机", "大恒相机"
        };

        /// <summary>
        /// 可用序列号列表（绑定第二个 ComboBox）
        /// </summary>
        public ObservableCollection<string> AvailableSNs { get; } = new();

        /// <summary>
        /// 已配置相机列表（绑定 DataGrid）
        /// </summary>
        public ObservableCollection<CameraModel> ConfiguredCameras { get; } = new();

        /// <summary>
        /// 选中的相机（DataGrid 选中项）
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(DeleteSelectedCameraCommand))] // 选中状态变化时刷新删除命令
        private CameraModel? _selectedCamera;

        /// <summary>
        /// 选中的相机品牌（第一个 ComboBox 选中项）
        /// </summary>
        [ObservableProperty]
        private string? _selectedBrand;

        /// <summary>
        /// 选中的相机 SN（第二个 ComboBox 选中项）
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddCameraCommand))] // SN 变化时刷新添加命令
        private string? _selectedSN;

        /// <summary>
        /// PLC Ready 信号地址（文本框输入）
        /// </summary>
        [ObservableProperty]
        private string? _plcReadyAddress;

        /// <summary>
        /// 相机备注（文本框输入）
        /// </summary>
        [ObservableProperty]
        private string? _cameraRemark;

        /// <summary>
        /// 添加相机命令（品牌、SN、PLC地址都有效时可执行）
        /// </summary>
        [RelayCommand]
        private void AddCamera()
        {
            // 1. 校验是否选中相机品牌
            if (string.IsNullOrWhiteSpace(SelectedBrand))
            {
                MessageBox.Show("请先选中相机品牌！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. 校验是否选中相机 SN
            if (string.IsNullOrWhiteSpace(SelectedSN))
            {
                MessageBox.Show($"请先选中{SelectedBrand}的设备SN！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 3. 校验是否填写 PLC Ready 信号地址
            if (string.IsNullOrWhiteSpace(PlcReadyAddress))
            {
                MessageBox.Show("请输入Ready信号（PLC地址）！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 4. 校验 SN 唯一性（避免重复添加）
            if (ConfiguredCameras.Any(c => c.Sn == SelectedSN))
            {
                MessageBox.Show($"SN为{SelectedSN}的相机已存在！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 5. 校验 PLC 地址唯一性
            if (ConfiguredCameras.Any(c => c.PlcAddress == PlcReadyAddress))
            {
                MessageBox.Show($"Ready信号地址{PlcReadyAddress}已被占用！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 所有校验通过，创建新相机模型并添加到列表
            var newCamera = new CameraModel
            {
                Brand = SelectedBrand!,
                Sn = SelectedSN!,
                PlcAddress = PlcReadyAddress!,
                Remark = string.IsNullOrWhiteSpace(CameraRemark) ? $"{SelectedBrand}相机" : CameraRemark
            };
            ConfiguredCameras.Add(newCamera);

            // 清空备注（保留品牌和SN，方便连续添加同品牌设备）
            CameraRemark = string.Empty;

            // 可选：添加成功后提示（按需开启）
            // MessageBox.Show("相机添加成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 删除选中相机命令（选中相机时可执行）
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanDeleteSelectedCamera))]
        private void DeleteSelectedCamera()
        {
            if (SelectedCamera != null)
            {
                ConfiguredCameras.Remove(SelectedCamera);
                SelectedCamera = null; // 清空选中状态，触发表单重置
            }
        }

        /// <summary>
        /// 保存配置命令（始终可执行）
        /// </summary>
        [RelayCommand]
        private void SaveConfig()
        {
            try
            {
                var configHelper = ProjectConfigHelper.Instance;
                configHelper.CurrentConfigs.CamerasConfigs = ConfiguredCameras.ToList();
                configHelper.SaveConfig();
                MessageBox.Show("相机配置保存成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存配置失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        /// <summary>
        /// 删除相机命令的可执行条件
        /// </summary>
        private bool CanDeleteSelectedCamera()
        {
            return SelectedCamera != null;
        }

        public CameraTabViewModel()
        {
            // 订阅属性变化事件（替代手动在 setter 中处理逻辑）
            SubscribeToPropertyChanges();

            // 加载已保存的配置
            LoadSavedCameraConfigs();
        }

        /// <summary>
        /// 订阅属性变化（解耦 setter 和业务逻辑）
        /// </summary>
        private void SubscribeToPropertyChanges()
        {
            // 选中品牌变化时，更新可用 SN 列表
            this.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SelectedBrand))
                {
                    UpdateAvailableSNs();
                }
                // PLC地址变化时，刷新添加命令状态
                else if (e.PropertyName == nameof(PlcReadyAddress))
                {
                    AddCameraCommand.NotifyCanExecuteChanged();
                }
            };
        }

        /// <summary>
        /// 加载已保存的相机配置
        /// </summary>
        private void LoadSavedCameraConfigs()
        {
            try
            {
                var configHelper = ProjectConfigHelper.Instance;
                var savedCameras = configHelper.CurrentConfigs?.CamerasConfigs;

                if (savedCameras?.Any() == true)
                {
                    foreach (var camera in savedCameras)
                    {
                        ConfiguredCameras.Add(camera);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载历史配置失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 根据选中的品牌更新可用 SN 列表
        /// </summary>
        private void UpdateAvailableSNs()
        {
            // 清空现有 SN 列表和选中状态
            AvailableSNs.Clear();
            SelectedSN = null;

            // 品牌未选中时，直接返回
            if (string.IsNullOrWhiteSpace(SelectedBrand))
                return;

            try
            {
                // 品牌映射为枚举
                var cameraBrand = SelectedBrand switch
                {
                    "海康相机" => CameraBrand.HIK,
                    "大恒相机" => CameraBrand.DaHeng,
                    _ => throw new ArgumentException($"不支持的相机品牌：{SelectedBrand}")
                };

                // 枚举设备 SN
                var deviceSNs = CamFactory.GetDeviceEnum(cameraBrand);

                // 绑定到界面
                foreach (var sn in deviceSNs)
                {
                    AvailableSNs.Add(sn);
                }

                // 默认选中第一个 SN（如有）
                if (AvailableSNs.Any())
                {
                    SelectedSN = AvailableSNs.First();
                }
                else
                {
                    MessageBox.Show($"未查询到{SelectedBrand}的设备！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取{SelectedBrand}SN列表失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}