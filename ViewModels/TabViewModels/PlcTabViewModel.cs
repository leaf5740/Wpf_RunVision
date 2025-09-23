using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using Wpf_RunVision.Models;
using Wpf_RunVision.Services.Plc;
using Wpf_RunVision.Utils;

namespace Wpf_RunVision.ViewModels.TabViewModels
{
    public class PlcTabViewModel : ObservableObject
    {
        private ObservableCollection<string> _availableProtocols;
        private ObservableCollection<string> _plcBrands;
        private string _selectedProtocol;
        private string _selectedBrand;
        private PlcModels _plcConfig = new PlcModels();
        private IPlcService _plcService;

        public ObservableCollection<string> AvailableProtocols
        {
            get => _availableProtocols;
            set => SetProperty(ref _availableProtocols, value);
        }

        public ObservableCollection<string> PlcBrands
        {
            get => _plcBrands;
            set => SetProperty(ref _plcBrands, value);
        }

        public string SelectedProtocol
        {
            get => _selectedProtocol;
            set => SetProperty(ref _selectedProtocol, value);
        }

        public string SelectedBrand
        {
            get => _selectedBrand;
            set => SetProperty(ref _selectedBrand, value);
        }

        public PlcModels PlcConfig
        {
            get => _plcConfig;
            set => SetProperty(ref _plcConfig, value);
        }

        public PlcTabViewModel()
        {
            AvailableProtocols = new ObservableCollection<string> { "ModbusTCP" };
            PlcBrands = new ObservableCollection<string> { "汇川", "三菱" };

            // 延迟加载配置，确保线程安全
            var config = ProjectConfigHelper.Instance.CurrentConfigs?.PlcConfig;
            PlcConfig = config ?? new PlcModels();

            // 初始化选中项 - 优先使用配置文件中的数据
            //协议
            if (!string.IsNullOrEmpty(PlcConfig.Protocol) && AvailableProtocols.Contains(PlcConfig.Protocol))
            {
                SelectedProtocol = PlcConfig.Protocol;
            }
            else if (AvailableProtocols.Count > 0)
            {
                SelectedProtocol = AvailableProtocols[0];
            }
            //品牌
            if (!string.IsNullOrEmpty(PlcConfig.Brand) && PlcBrands.Contains(PlcConfig.Brand))
            {
                SelectedBrand = PlcConfig.Brand;
            }
            else if (PlcBrands.Count > 0)
            {
                SelectedBrand = PlcBrands[0];
            }
        }

        private RelayCommand _connectCommand;
        /// <summary>
        /// 连接 PLC 命令
        /// </summary>
        public RelayCommand ConnectCommand
        {
            get
            {
                if (_connectCommand == null)
                {
                    _connectCommand = new RelayCommand(() =>
                    {
                        // 检查必填信息
                        if (string.IsNullOrEmpty(PlcConfig.Ip) || string.IsNullOrEmpty(PlcConfig.Port))
                        {
                            MessageBox.Show("IP地址和端口号不能为空！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        if (!int.TryParse(PlcConfig.Port, out int port))
                        {
                            MessageBox.Show("端口号必须是数字！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        try
                        {
                            _plcService = PlcFactory.Create(SelectedBrand);
                            bool success = _plcService.Connect(_plcConfig.Ip, int.Parse(_plcConfig.Port));

                            MessageBox.Show("PLC连接成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"PLC连接失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        }

                    });
                }

                return _connectCommand;
            }
        }

        private RelayCommand _saveConfigCommand;
        /// <summary>
        /// 保存 PLC 配置命令
        /// 将当前设置保存到配置文件
        /// </summary>
        public RelayCommand SaveConfigCommand
        {
            get
            {
                if (_saveConfigCommand == null)
                {
                    _saveConfigCommand = new RelayCommand(() =>
                    {
                        try
                        {
                            // 同步到配置对象并保存
                            var helper = ProjectConfigHelper.Instance;
                            helper.CurrentConfigs.PlcConfig = PlcConfig;
                            helper.CurrentConfigs.PlcConfig.Protocol = SelectedProtocol;
                            helper.CurrentConfigs.PlcConfig.Brand = SelectedBrand;
                            helper.SaveConfig();

                            MessageBox.Show("PLC配置已保存！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"PLC配置保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    });
                }

                return _saveConfigCommand;
            }
        }

    }
}
