using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using Wpf_RunVision.Models;
using Wpf_RunVision.Services.Cameras;
using Wpf_RunVision.Utils;

namespace Wpf_RunVision.ViewModels.TabViewModels
{
    /// <summary>
    /// 相机配置 Tab 页的 ViewModel
    /// 管理相机列表、品牌选择、序列号选择，并支持添加/删除/保存操作
    /// </summary>
    public class CameraTabViewModel : ObservableObject
    {
        #region 字段

        // 自动注册的相机服务列表（通过反射扫描所有ICameraService实现）
        private readonly List<ICameraService> _cameraServices;

        // 当前选中的相机（DataGrid 绑定 SelectedItem）
        private CameraModels _selectedCamera;

        // 当前选中的相机品牌（ComboBox 绑定）
        private string _selectedBrand;

        // 当前选中的相机序列号（ComboBox 绑定）
        private string _selectedSN;

        // 当前输入的相机备注（TextBox 绑定）
        private string _selectedRemark;

        // 当前输入的相机完成信号（TextBox 绑定）
        private string _plcCompleteAddress;

        // 命令字段
        private RelayCommand _deleteSelectedCommand;
        private RelayCommand _addCameraCommand;
        private RelayCommand _saveConfigCommand;

        #endregion

        #region 属性

        /// <summary>
        /// 已配置相机列表，绑定 DataGrid
        /// </summary>
        public ObservableCollection<CameraModels> Cameras { get; private set; }

        /// <summary>
        /// 相机品牌列表，绑定品牌 ComboBox
        /// </summary>
        public ObservableCollection<string> CameraBrands { get; private set; }

        /// <summary>
        /// 当前选中品牌下的可用序列号列表，绑定序列号 ComboBox
        /// </summary>
        public ObservableCollection<string> AvailableSNs { get; private set; }

        /// <summary>
        /// 当前选中的相机（DataGrid SelectedItem 绑定）
        /// </summary>
        public CameraModels SelectedCamera
        {
            get { return _selectedCamera; }
            set
            {
                if (_selectedCamera != value)
                {
                    _selectedCamera = value;
                    OnPropertyChanged(nameof(SelectedCamera));
                    _deleteSelectedCommand?.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// 当前选中的品牌（ComboBox 绑定）
        /// </summary>
        public string SelectedBrand
        {
            get { return _selectedBrand; }
            set
            {
                if (_selectedBrand != value)
                {
                    _selectedBrand = value;
                    OnPropertyChanged(nameof(SelectedBrand));
                    UpdateAvailableSNByBrand(_selectedBrand);
                }
            }
        }

        /// <summary>
        /// 当前选中的序列号（ComboBox 绑定）
        /// </summary>
        public string SelectedSN
        {
            get { return _selectedSN; }
            set
            {
                if (_selectedSN != value)
                {
                    _selectedSN = value;
                    OnPropertyChanged(nameof(SelectedSN));
                }
            }
        }

        /// <summary>
        /// 当前输入的相机完成信号（TextBox 绑定）
        /// </summary>
        public string PlcCompleteAddress
        {
            get { return _plcCompleteAddress; }
            set
            {
                if (_plcCompleteAddress != value)
                {
                    _plcCompleteAddress = value;
                    OnPropertyChanged(nameof(PlcCompleteAddress));
                }
            }
        }

        /// <summary>
        /// 当前输入的相机备注（TextBox 绑定）
        /// </summary>
        public string SelectedRemark
        {
            get { return _selectedRemark; }
            set
            {
                if (_selectedRemark != value)
                {
                    _selectedRemark = value;
                    OnPropertyChanged(nameof(SelectedRemark));
                }
            }
        }

        #endregion

        #region 构造函数

        public CameraTabViewModel()
        {
            // 初始化集合
            Cameras = new ObservableCollection<CameraModels>();
            CameraBrands = new ObservableCollection<string>();
            AvailableSNs = new ObservableCollection<string>();

            // 加载相机服务
            _cameraServices = GetAllCameraServices();

            // 初始化数据
            LoadCamerasFromConfig();
            InitializeCameraBrands();
        }

        #endregion

        #region 初始化方法

        /// <summary>
        /// 从配置加载相机列表
        /// </summary>
        private void LoadCamerasFromConfig()
        {
            try
            {
                var configs = ProjectConfigHelper.Instance.CurrentConfigs;
                if (configs.Cameras != null && configs.Cameras.Any())
                {
                    foreach (var camera in configs.Cameras)
                    {
                        Cameras.Add(camera);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载相机配置失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 初始化相机品牌列表
        /// </summary>
        private void InitializeCameraBrands()
        {
            try
            {
                var distinctBrands = _cameraServices
                    .Select(s => s.Brand)
                    .Where(brand => !string.IsNullOrEmpty(brand))
                    .Distinct();

                foreach (var brand in distinctBrands)
                {
                    CameraBrands.Add(brand);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化品牌列表失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 通过反射扫描程序集中所有实现了ICameraService的类，并创建实例
        /// </summary>
        private List<ICameraService> GetAllCameraServices()
        {
            var cameraServices = new List<ICameraService>();

            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var serviceTypes = assembly.GetTypes()
                    .Where(type => typeof(ICameraService).IsAssignableFrom(type)
                                   && !type.IsAbstract
                                   && !type.IsInterface
                                   && type.GetConstructor(Type.EmptyTypes) != null);

                foreach (var type in serviceTypes)
                {
                    var service = Activator.CreateInstance(type) as ICameraService;
                    if (service != null)
                    {
                        cameraServices.Add(service);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"自动注册相机服务失败：{ex.Message}", "初始化错误",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return cameraServices;
        }

        #endregion

        #region 业务逻辑

        /// <summary>
        /// 根据选中的品牌更新可用序列号列表
        /// </summary>
        private void UpdateAvailableSNByBrand(string brand)
        {
            AvailableSNs.Clear();
            SelectedSN = null;

            if (string.IsNullOrEmpty(brand))
                return;

            try
            {
                var service = _cameraServices.FirstOrDefault(s => s.Brand == brand);
                if (service == null)
                {
                    MessageBox.Show($"未找到{brand}品牌的相机服务", "警告",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var sns = service.GetAvailableSNs() ?? new List<string>();
                foreach (var sn in sns)
                {
                    AvailableSNs.Add(sn);
                }

                if (AvailableSNs.Any())
                    SelectedSN = AvailableSNs.First();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取{brand}序列号失败：{ex.Message}", "错误",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 判断序列号是否已存在
        /// </summary>
        private bool IsSnDuplicated(string sn)
        {
            return Cameras.Any(camera => camera.Sn == sn);
        }

        /// <summary>
        /// 判断PLC完成信号是否已存在
        /// </summary>
        private bool IsPlcAddressDuplicated(string address)
        {
            return Cameras.Any(camera => camera.PlcCompleteAddress == address);
        }

        #endregion

        #region 命令

        /// <summary>
        /// 添加相机命令
        /// </summary>
        public RelayCommand AddCameraCommand
        {
            get
            {
                if (_addCameraCommand == null)
                {
                    _addCameraCommand = new RelayCommand(AddCamera);
                }
                return _addCameraCommand;
            }
        }

        /// <summary>
        /// 添加相机执行逻辑
        /// </summary>
        private void AddCamera()
        {
            // 输入验证
            if (string.IsNullOrEmpty(SelectedBrand) ||
                string.IsNullOrEmpty(SelectedSN) ||
                string.IsNullOrEmpty(PlcCompleteAddress))
            {
                MessageBox.Show("相机品牌、序列号和PLC完成信号不能为空！", "错误",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 重复验证
            if (IsSnDuplicated(SelectedSN))
            {
                MessageBox.Show("该序列号已存在！", "错误",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (IsPlcAddressDuplicated(PlcCompleteAddress))
            {
                MessageBox.Show("该PLC完成信号已存在！", "错误",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 创建新相机
            var newCamera = new CameraModels
            {
                Brand = SelectedBrand,
                Sn = SelectedSN,
                PlcCompleteAddress = PlcCompleteAddress,
                Remark = string.IsNullOrWhiteSpace(SelectedRemark)
                    ? $"{SelectedBrand}相机"
                    : SelectedRemark
            };

            Cameras.Add(newCamera);
        }

        /// <summary>
        /// 删除选中相机命令
        /// </summary>
        public RelayCommand DeleteSelectedCommand
        {
            get
            {
                if (_deleteSelectedCommand == null)
                {
                    _deleteSelectedCommand = new RelayCommand(DeleteSelected, CanDeleteSelected);
                }
                return _deleteSelectedCommand;
            }
        }

        /// <summary>
        /// 删除相机执行逻辑
        /// </summary>
        private void DeleteSelected()
        {
            if (SelectedCamera != null)
            {
                Cameras.Remove(SelectedCamera);
                SelectedCamera = null; // 清除选中状态
            }
        }

        /// <summary>
        /// 删除命令可用性判断
        /// </summary>
        private bool CanDeleteSelected()
        {
            return SelectedCamera != null;
        }

        /// <summary>
        /// 保存配置命令
        /// </summary>
        public RelayCommand SaveConfigCommand
        {
            get
            {
                if (_saveConfigCommand == null)
                {
                    _saveConfigCommand = new RelayCommand(SaveConfig, CanSaveConfig);
                }
                return _saveConfigCommand;
            }
        }

        /// <summary>
        /// 保存配置执行逻辑
        /// </summary>
        private void SaveConfig()
        {
            try
            {
                ProjectConfigHelper.Instance.CurrentConfigs.Cameras = Cameras.ToList();
                ProjectConfigHelper.Instance.SaveConfig();
                MessageBox.Show("配置已保存", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存配置失败：{ex.Message}", "错误",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 保存命令可用性判断
        /// </summary>
        private bool CanSaveConfig()
        {
            return Cameras != null;
        }

        #endregion
    }
}
