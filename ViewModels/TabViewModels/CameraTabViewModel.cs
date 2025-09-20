using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
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

        // 删除选中相机命令
        private RelayCommand _deleteSelectedCommand;

        // 添加相机命令
        private RelayCommand _addCameraCommand;

        // 保存配置命令
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
                    _deleteSelectedCommand?.NotifyCanExecuteChanged(); // 更新删除命令状态
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
                    UpdateAvailableSNByBrand(_selectedBrand); // 刷新序列号列表
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
        /// 当前输入的相机备注（TextBox 绑定）
        /// </summary>
        public string SelectedRemark
        {
            get => _selectedRemark;
            set => SetProperty(ref _selectedRemark, value);
        }

        #endregion

        #region 构造函数

        public CameraTabViewModel()
        {
            // 初始化相机列表，从项目配置读取
            var configs = ProjectConfigHelper.Instance.CurrentConfigs;
            Cameras = new ObservableCollection<CameraModels>(ProjectConfigHelper.Instance.CurrentConfigs.Cameras ?? new List<CameraModels>());

            // 初始化序列号列表
            AvailableSNs = new ObservableCollection<string>();

            // 自动注册所有实现了ICameraService的服务（核心修改）
            _cameraServices = GetAllCameraServices();

            // 初始化品牌列表（去重处理）
            CameraBrands = new ObservableCollection<string>(
                _cameraServices.Select(s => s.Brand).Distinct()
            );

            // 可选：默认选中第一个品牌
            // if (CameraBrands.Count > 0)
            //     SelectedBrand = CameraBrands[0];
        }

        #endregion

        #region 核心方法：自动扫描并注册所有相机服务

        /// <summary>
        /// 通过反射扫描程序集中所有实现了ICameraService的类，并创建实例
        /// </summary>
        /// <returns>所有相机服务实例列表</returns>
        private List<ICameraService> GetAllCameraServices()
        {
            var cameraServices = new List<ICameraService>();

            try
            {
                // 获取当前程序集（如果服务在其他程序集，可改为Assembly.Load("其他程序集名称")）
                var assembly = Assembly.GetExecutingAssembly();

                // 查找所有实现了ICameraService接口的非抽象类
                var serviceTypes = assembly.GetTypes()
                    .Where(type =>
                        typeof(ICameraService).IsAssignableFrom(type)  // 实现了接口
                        && !type.IsAbstract                          // 不是抽象类
                        && !type.IsInterface                         // 不是接口本身
                        && type.GetConstructor(Type.EmptyTypes) != null); // 有无参数构造函数

                // 为每个找到的类型创建实例
                foreach (var type in serviceTypes)
                {
                    var service = (ICameraService)Activator.CreateInstance(type);
                    cameraServices.Add(service);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"自动注册相机服务失败：{ex.Message}", "初始化错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return cameraServices;
        }

        #endregion

        #region 其他方法

        /// <summary>
        /// 根据选中的品牌更新可用序列号列表
        /// </summary>
        /// <param name="brand">选中的品牌</param>
        private void UpdateAvailableSNByBrand(string brand)
        {
            AvailableSNs.Clear();
            if (string.IsNullOrEmpty(brand))
            {
                SelectedSN = null;
                return;
            }

            try
            {
                var service = _cameraServices.FirstOrDefault(s => s.Brand == brand);
                var sns = service?.GetAvailableSNs() ?? new List<string>();
                foreach (var sn in sns)
                    AvailableSNs.Add(sn);
                SelectedSN = AvailableSNs.FirstOrDefault();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取{brand}相机序列号失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                SelectedSN = null;
            }
        }

        /// <summary>
        /// 判断序列号是否已存在（避免重复添加）
        /// </summary>
        private bool IsSnDuplicated(string sn)
        {
            return Cameras.Any(camera => camera.Sn == sn);
        }

        #endregion

        #region 命令

        /// <summary>
        /// 添加相机命令
        /// </summary>
        public ICommand AddCameraCommand
        {
            get
            {
                if (_addCameraCommand == null)
                {
                    _addCameraCommand = new RelayCommand(() =>
                    {
                        // 校验品牌和序列号
                        if (string.IsNullOrEmpty(SelectedBrand) || string.IsNullOrEmpty(SelectedSN))
                        {
                            MessageBox.Show("相机品牌和序列号不能为空！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        // 防止重复序列号
                        if (IsSnDuplicated(SelectedSN))
                        {
                            MessageBox.Show("该序列号已存在！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        // 新建相机配置
                        var newCamera = new CameraModels
                        {
                            Brand = SelectedBrand,
                            Sn = SelectedSN,
                            Remark = string.IsNullOrWhiteSpace(SelectedRemark) ? $"{SelectedBrand}品牌相机" : SelectedRemark
                        };

                        // 添加到列表并更新删除命令状态
                        Cameras.Add(newCamera);
                        _deleteSelectedCommand?.NotifyCanExecuteChanged();
                    });
                }
                return _addCameraCommand;
            }
        }

        /// <summary>
        /// 删除选中相机命令
        /// </summary>
        public ICommand DeleteSelectedCommand
        {
            get
            {
                if (_deleteSelectedCommand == null)
                {
                    _deleteSelectedCommand = new RelayCommand(() =>
                    {
                        if (SelectedCamera == null)
                        {
                            MessageBox.Show("请先选择要删除的相机！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }

                        Cameras.Remove(SelectedCamera);
                        _deleteSelectedCommand.NotifyCanExecuteChanged();
                    },
                    // 优化CanExecute逻辑：列表非空且有选中项
                    () => Cameras != null && Cameras.Count > 0 && SelectedCamera != null);
                }
                return _deleteSelectedCommand;
            }
        }

        /// <summary>
        /// 保存相机配置命令
        /// </summary>
        public ICommand SaveConfigCommand
        {
            get
            {
                if (_saveConfigCommand == null)
                {
                    _saveConfigCommand = new RelayCommand(() =>
                    {
                        // 保存到项目配置
                        ProjectConfigHelper.Instance.CurrentConfigs.Cameras = Cameras.ToList();
                        ProjectConfigHelper.Instance.SaveConfig();

                        MessageBox.Show("配置已保存", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    },
                    // 优化CanExecute逻辑：允许保存空列表（仅判断列表已初始化）
                    () => Cameras != null);
                }
                return _saveConfigCommand;
            }
        }

        #endregion
    }
}
