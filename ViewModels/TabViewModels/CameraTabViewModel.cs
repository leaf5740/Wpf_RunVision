using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Wpf_RunVision.Models;
using Wpf_RunVision.Services.Cameras;
using Wpf_RunVision.Tools;

namespace Wpf_RunVision.ViewModels.TabViewModels
{
    public class CameraTabViewModel : ObservableObject
    {
        #region 字段

        // 所有相机服务（支持多品牌相机）
        private readonly List<ICameraService> _cameraServices;

        // 当前选中的相机对象
        private CameraModel _selectedCamera;

        // 当前选中的相机品牌
        private string _selectedBrand;

        // 当前选中的相机序列号
        private string _selectedSN;

        // 数据源：已配置相机列表
        private ObservableCollection<CameraModel> _cameras;

        // 删除命令实例
        private RelayCommand _deleteSelectedCommand;

        #endregion

        #region 属性

        /// <summary>
        /// 已配置相机列表，绑定 DataGrid
        /// ObservableCollection 自动更新 UI
        /// </summary>
        public ObservableCollection<CameraModel> Cameras
        {
            get { return _cameras; }
            set
            {
                _cameras = value;
                OnPropertyChanged();
                // 数据变化时刷新删除按钮状态
                _deleteSelectedCommand?.NotifyCanExecuteChanged();
            }
        }

        /// <summary>
        /// 相机品牌列表，绑定品牌 ComboBox
        /// </summary>
        public ObservableCollection<string> CameraBrands { get; set; }

        /// <summary>
        /// 当前选中品牌下可用序列号列表，绑定 SN ComboBox
        /// </summary>
        public ObservableCollection<string> AvailableSNs { get; set; }

        /// <summary>
        /// 当前选中的相机对象，绑定 DataGrid 的 SelectedItem
        /// </summary>
        public CameraModel SelectedCamera
        {
            get { return _selectedCamera; }
            set
            {
                if (SetProperty(ref _selectedCamera, value))
                {
                    // 选中相机变化时刷新删除按钮状态
                    _deleteSelectedCommand?.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// 当前选中的相机品牌，绑定 ComboBox
        /// </summary>
        public string SelectedBrand
        {
            get { return _selectedBrand; }
            set
            {
                if (SetProperty(ref _selectedBrand, value))
                {
                    // 品牌改变时刷新对应序列号列表
                    UpdateAvailableSNByBrand(value);
                }
            }
        }

        /// <summary>
        /// 当前选中的相机序列号，绑定 ComboBox
        /// </summary>
        public string SelectedSN
        {
            get { return _selectedSN; }
            set { SetProperty(ref _selectedSN, value); }
        }

        /// <summary>
        /// 当前输入的相机备注，绑定 TextBox
        /// </summary>
        public string SelectedRemark { get; set; }

        #endregion

        #region 构造函数

        public CameraTabViewModel()
        {
            // 从配置文件加载已保存相机配置
            Cameras = ProjectConfigHelper.Instance.CurrentConfigs.Cameras;

            // 初始化可用序列号列表
            AvailableSNs = new ObservableCollection<string>();

            // 初始化相机服务列表（支持多品牌）
            _cameraServices = new List<ICameraService>
            {
                new DahengCameraService(),
                new HikvisionCameraService(),
            };

            // 填充品牌列表
            CameraBrands = new ObservableCollection<string>(_cameraServices.Select(s => s.Brand));
        }

        #endregion

        #region 方法

        /// <summary>
        /// 根据品牌刷新可用相机序列号列表
        /// </summary>
        /// <param name="brand">相机品牌</param>
        private void UpdateAvailableSNByBrand(string brand)
        {
            // 清空原有序列号
            AvailableSNs.Clear();

            // 获取对应品牌的服务
            var service = _cameraServices.FirstOrDefault(s => s.Brand == brand);
            if (service != null)
            {
                foreach (var sn in service.GetAvailableSNs())
                    AvailableSNs.Add(sn);
            }

            // 默认选中第一个序列号
            if (AvailableSNs.Count > 0)
                SelectedSN = AvailableSNs[0];
            else
                SelectedSN = null;
        }

        #endregion

        #region 命令

        /// <summary>
        /// 添加相机命令
        /// </summary>
        private RelayCommand _addCameraCommand;
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

                        // 校验重复 SN
                        if (Cameras.Any(c => c.Type == SelectedSN))
                        {
                            MessageBox.Show("该序列号已存在！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        // 创建新相机对象
                        var newCamera = new CameraModel
                        {
                            Name = $"{SelectedBrand}相机{Cameras.Count + 1}",
                            Type = SelectedSN,
                            Remark = string.IsNullOrWhiteSpace(SelectedRemark) ? $"{SelectedBrand}品牌相机" : SelectedRemark
                        };

                        // 添加到列表
                        Cameras.Add(newCamera);

                        // 刷新删除按钮状态
                        _deleteSelectedCommand?.NotifyCanExecuteChanged();
                    });
                }
                return _addCameraCommand;
            }
        }

        /// <summary>
        /// 删除选中相机命令（无数据时按钮禁用）
        /// </summary>
        public ICommand DeleteSelectedCommand
        {
            get
            {
                if (_deleteSelectedCommand == null)
                {
                    _deleteSelectedCommand = new RelayCommand(
                        () =>
                        {
                            // 判断是否选中相机
                            if (SelectedCamera == null)
                            {
                                MessageBox.Show("请先选择要删除的相机！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                                return;
                            }

                            // 从列表中移除
                            if (Cameras.Contains(SelectedCamera))
                                Cameras.Remove(SelectedCamera);

                            // 刷新按钮状态
                            _deleteSelectedCommand.NotifyCanExecuteChanged();
                        },
                        // CanExecute：当列表有数据时按钮可用
                        () => Cameras != null && Cameras.Count > 0
                    );
                }
                return _deleteSelectedCommand;
            }
        }

        /// <summary>
        /// 保存配置按钮命令
        /// </summary>
        private RelayCommand _saveConfigCommand;
        public ICommand SaveConfigCommand
        {
            get
            {
                if (_saveConfigCommand == null)
                {
                    _saveConfigCommand = new RelayCommand(() =>
                    {
                        if (Cameras != null && Cameras.Count > 0)
                        {
                            // 有数据则保存
                            ProjectConfigHelper.Instance.SaveConfig();
                            MessageBox.Show("配置已保存", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            // 无数据弹出提示
                            MessageBox.Show("请先添加相机信息再保存", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    });
                }
                return _saveConfigCommand;
            }
        }
        #endregion
    }
}
