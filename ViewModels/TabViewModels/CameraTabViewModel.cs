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
    public class CameraTabViewModel : ObservableObject
    {
        #region 私有字段
        // 相机品牌列表（绑定第一个ComboBox）
        private readonly ObservableCollection<string> _cameraBrands;
        // 可用序列号列表（绑定第二个ComboBox）
        private readonly ObservableCollection<string> _availableSNs = new ObservableCollection<string>();
        // 已配置相机列表（绑定DataGrid）
        private readonly ObservableCollection<CameraModels> _cameras = new ObservableCollection<CameraModels>();

        // 绑定的选中项（初始化为null，实现默认不选中）
        private CameraModels _selectedCamera;
        private string _selectedBrand; // 初始为null，默认不选中品牌
        private string _selectedSN;
        private string _selectedPlcAddress;
        private string _selectedRemark;

        // 命令字段
        private RelayCommand _deleteSelectedCommand;
        private RelayCommand _addCameraCommand;
        private RelayCommand _saveConfigCommand;
        #endregion

        #region 公共属性（绑定视图）
        /// <summary>
        /// 已配置相机列表（DataGrid的ItemsSource）
        /// </summary>
        public ObservableCollection<CameraModels> Cameras => _cameras;

        /// <summary>
        /// 选中的相机（DataGrid的SelectedItem）
        /// </summary>
        public CameraModels SelectedCamera
        {
            get => _selectedCamera;
            set
            {
                if (SetProperty(ref _selectedCamera, value))
                {
                    SyncFormWithSelectedCamera();
                    _deleteSelectedCommand?.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// 相机品牌列表（第一个ComboBox的ItemsSource）
        /// </summary>
        public ObservableCollection<string> CameraBrands => _cameraBrands;

        /// <summary>
        /// 选中的品牌（第一个ComboBox的SelectedItem）
        /// 手动选中时才触发SN枚举
        /// </summary>
        public string SelectedBrand
        {
            get => _selectedBrand;
            set
            {
                if (SetProperty(ref _selectedBrand, value))
                {
                    // 只有选中有效品牌时才更新SN列表（空值时清空）
                    UpdateAvailableSNs();
                }
            }
        }

        /// <summary>
        /// 可用序列号列表（第二个ComboBox的ItemsSource）
        /// </summary>
        public ObservableCollection<string> AvailableSNs => _availableSNs;

        /// <summary>
        /// 选中的序列号（第二个ComboBox的SelectedItem）
        /// </summary>
        public string SelectedSN
        {
            get => _selectedSN;
            set
            {
                if (SetProperty(ref _selectedSN, value))
                {
                    // 刷新添加相机命令的可执行状态
                    AddCameraCommand?.NotifyCanExecuteChanged();
                }
            }
        }

        public string SelectedPlcAddress
        {
            get => _selectedPlcAddress;
            set => SetProperty(ref _selectedPlcAddress, value);
        }

        /// <summary>
        /// 选中的备注（TextBox的Text）
        /// </summary>
        public string SelectedRemark
        {
            get => _selectedRemark;
            set => SetProperty(ref _selectedRemark, value);
        }
        #endregion

        #region 命令（绑定按钮）
        /// <summary>
        /// 删除选中相机命令
        /// </summary>
        public RelayCommand DeleteSelectedCommand
        {
            get
            {
                return _deleteSelectedCommand ?? (_deleteSelectedCommand = new RelayCommand(
                    execute: DeleteSelectedCamera,
                    canExecute: () => SelectedCamera != null
                ));
            }
        }

        /// <summary>
        /// 添加相机命令（品牌和SN都选中时才可执行）
        /// </summary>
        public RelayCommand AddCameraCommand
        {
            get
            {
                return _addCameraCommand ?? (_addCameraCommand = new RelayCommand(
                    execute: AddCamera,
                    canExecute: () => !string.IsNullOrEmpty(SelectedBrand) && !string.IsNullOrEmpty(SelectedSN)
                ));
            }
        }

        /// <summary>
        /// 保存配置命令
        /// </summary>
        public RelayCommand SaveConfigCommand
        {
            get
            {
                return _saveConfigCommand ?? (_saveConfigCommand = new RelayCommand(
                    execute: SaveConfig,
                    canExecute: () => true
                ));
            }
        }
        #endregion

        #region 构造函数（核心修改：移除默认选中品牌的逻辑）
        public CameraTabViewModel()
        {
            // 初始化相机品牌列表
            _cameraBrands = new ObservableCollection<string> { "海康相机", "大恒相机" };

            // 加载已保存的相机配置
            LoadSavedCameras();

            // 【关键修改】删除默认选中第一个品牌的代码，保持SelectedBrand初始为null
            // 原代码：if (_cameraBrands.Any()) { SelectedBrand = _cameraBrands.First(); }
        }
        #endregion

        #region 业务逻辑
        /// <summary>
        /// 同步选中相机的信息到表单
        /// </summary>
        private void SyncFormWithSelectedCamera()
        {
            if (SelectedCamera != null)
            {
                // 选中相机时自动填充品牌（触发SN枚举）
                SelectedBrand = SelectedCamera.Brand;
                SelectedSN = SelectedCamera.Sn;
                SelectedPlcAddress = SelectedCamera.PlcAddress;
                SelectedRemark = SelectedCamera.Remark;
            }
            else
            {
                // 未选中相机时清空表单（品牌保持当前选中状态，不强制清空）
                SelectedSN = string.Empty;
                SelectedPlcAddress = string.Empty;
                SelectedRemark = string.Empty;
            }
        }
  
        /// <summary>
        /// 根据选中的品牌更新可用SN列表（仅在品牌不为空时枚举）
        /// </summary>
        private void UpdateAvailableSNs()
        {
            // 清空现有SN列表
            _availableSNs.Clear();
            SelectedSN = string.Empty;

            // 【核心逻辑】品牌未选中（为空）时不枚举，直接返回
            if (string.IsNullOrEmpty(SelectedBrand))
                return;

            try
            {
                // 将界面品牌映射为枚举
                CameraBrand brand;
                switch (SelectedBrand)
                {
                    case "海康相机":
                        brand = CameraBrand.HIK;
                        break;
                    case "大恒相机":
                        brand = CameraBrand.DaHeng;
                        break;
                    default:
                        MessageBox.Show($"不支持的相机品牌：{SelectedBrand}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                }

                // 调用工厂类枚举SN（仅在品牌有效时执行）
                var deviceList = CamFactory.GetDeviceEnum(brand);

                // 绑定SN列表到界面
                foreach (var sn in deviceList)
                {
                    _availableSNs.Add(sn);
                }

                // 默认选中第一个SN（如有）
                if (_availableSNs.Any())
                {
                    SelectedSN = _availableSNs.First();
                }
                else
                {
                    MessageBox.Show($"未查询到{SelectedBrand}的设备SN！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // 关键：刷新添加命令的可执行状态
                AddCameraCommand?.NotifyCanExecuteChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取{SelectedBrand}的SN列表失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 添加相机到列表
        /// </summary>
        private void AddCamera()
        {
            if (string.IsNullOrEmpty(SelectedPlcAddress))
            {
                MessageBox.Show($"相机完成信号不能为空！", "提示");
                return;
            }
            if (_cameras.Any(c => c.Sn == SelectedSN))
            {
                MessageBox.Show($"SN为{SelectedSN}的相机已存在！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_cameras.Any(c => c.PlcAddress == SelectedPlcAddress))
            {
                MessageBox.Show($"相机完成信号为{SelectedPlcAddress}的地址已存在！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newCamera = new CameraModels
            {
                Brand = SelectedBrand,
                Sn = SelectedSN,
                PlcAddress = SelectedPlcAddress,
                Remark = string.IsNullOrWhiteSpace(SelectedRemark) ? $"{SelectedBrand}相机" : SelectedRemark
            };
            _cameras.Add(newCamera);

            // 清空备注，保留品牌和SN方便连续添加同品牌设备
            SelectedRemark = string.Empty;
        }

        /// <summary>
        /// 删除选中的相机
        /// </summary>
        private void DeleteSelectedCamera()
        {
            if (SelectedCamera != null)
            {
                _cameras.Remove(SelectedCamera);
                SelectedCamera = null;
            }
        }

        /// <summary>
        /// 保存相机配置
        /// </summary>
        private void SaveConfig()
        {
            try
            {
                var configHelper = ProjectConfigHelper.Instance;
                configHelper.CurrentConfigs.Cameras = _cameras.ToList();
                configHelper.SaveConfig();
                MessageBox.Show("相机配置保存成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存配置失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 加载已保存的相机配置
        /// </summary>
        private void LoadSavedCameras()
        {
            try
            {
                var configHelper = ProjectConfigHelper.Instance;
                var savedCameras = configHelper.CurrentConfigs?.Cameras;

                if (savedCameras != null && savedCameras.Any())
                {
                    foreach (var camera in savedCameras)
                    {
                        _cameras.Add(camera);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载历史配置失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion
    }
}