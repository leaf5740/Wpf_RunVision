using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Windows;
using Wpf_RunVision.Models;
using Wpf_RunVision.Services.Plc;
using Wpf_RunVision.Utils;

namespace Wpf_RunVision.ViewModels.TabViewModels
{
    public class PlcTabViewModel : ObservableObject
    {
        private readonly ObservableCollection<string> _availableProtocols = new ObservableCollection<string> { "ModbusTCP" };
        private readonly ObservableCollection<string> _plcBrands = new ObservableCollection<string> { "汇川", "三菱" };
        private string _selectedProtocol;
        private string _selectedBrand;
        private  PlcModels _plcConfig = new PlcModels(); // 显式指定类型，避免目标类型推断
        private IPlcService _plcService;

        public ObservableCollection<PLCAddressModels> PLCAddressModels1 { get; private set; } = new ObservableCollection<PLCAddressModels>();
        public ObservableCollection<PLCAddressModels> PLCAddressModels2 { get; private set; } = new ObservableCollection<PLCAddressModels>();

        // 公共属性
        public ObservableCollection<string> AvailableProtocols => _availableProtocols;
        public ObservableCollection<string> PlcBrands => _plcBrands;

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

        private RelayCommand _connectCommand;
        public RelayCommand ConnectCommand
        {
            get
            {
                return _connectCommand ?? (_connectCommand = new RelayCommand(ConnectPlc));
            }
        }

        private RelayCommand _saveConfigCommand;
        public RelayCommand SaveConfigCommand
        {
            get
            {
                return _saveConfigCommand ?? (_saveConfigCommand = new RelayCommand(SavePlcConfig));
            }
        }

        public PlcTabViewModel()
        {
            InitializeAddressModels();
            LoadPlcConfig();
            InitializeSelectedItems();
        }

        /// <summary>
        /// 初始化地址模型集合的默认值（适配C# 7.3，显式创建对象）
        /// </summary>
        private void InitializeAddressModels()
        {
            var sendSignals = new List<string> { "进板信号", "心跳信号", "复位完成信号" };
            foreach (var name in sendSignals)
            {
                PLCAddressModels1.Add(new PLCAddressModels // 显式指定类型，不依赖目标类型推断
                {
                    Name = name,
                    Address = "null", // 保持与模型类一致的属性名
                    Value = "null",
                    Remark = "null"
                });
            }

            var receiveSignals = new List<string>
            {
                "相机1Ready信号", "相机2Ready信号", "相机3Ready信号",
                "相机4Ready信号", "扫码枪Ready信号", "扫码枪失败信号",
                "检测完成信号", "复位完成信号"
            };
            foreach (var name in receiveSignals)
            {
                PLCAddressModels2.Add(new PLCAddressModels // 显式指定类型
                {
                    Name = name,
                    Address = "null", // 保持与模型类一致的属性名
                    Value = "null",
                    Remark = "null"
                });
            }
        }

        /// <summary>
        /// 从配置文件加载PLC配置
        /// </summary>
        private void LoadPlcConfig()
        {
            try
            {
                var configHelper = ProjectConfigHelper.Instance;
                var currentConfigs = configHelper.CurrentConfigs;
                if (currentConfigs == null) return;

                var config = currentConfigs.PlcConfig;
                if (config == null) return;

                // 同步基础配置
                PlcConfig.Ip = config.Ip;
                PlcConfig.Port = config.Port;
                PlcConfig.Protocol = config.Protocol;
                PlcConfig.Brand = config.Brand;

                // 加载发送信号地址（替换默认值）
                if (config.PLCAddressModels1 != null && config.PLCAddressModels1.Any())
                {
                    PLCAddressModels1.Clear();
                    foreach (var item in config.PLCAddressModels1)
                    {
                        PLCAddressModels1.Add(item);
                    }
                }

                // 加载接收信号地址（替换默认值）
                if (config.PLCAddressModels2 != null && config.PLCAddressModels2.Any())
                {
                    PLCAddressModels2.Clear();
                    foreach (var item in config.PLCAddressModels2)
                    {
                        PLCAddressModels2.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载PLC配置失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 初始化选中项（协议和品牌）
        /// </summary>
        private void InitializeSelectedItems()
        {
            if (!string.IsNullOrEmpty(PlcConfig.Protocol) && AvailableProtocols.Contains(PlcConfig.Protocol))
            {
                SelectedProtocol = PlcConfig.Protocol;
            }
            else
            {
                SelectedProtocol = AvailableProtocols.FirstOrDefault();
            }

            // 初始化品牌选中项
            if (!string.IsNullOrEmpty(PlcConfig.Brand) && PlcBrands.Contains(PlcConfig.Brand))
            {
                SelectedBrand = PlcConfig.Brand;
            }
            else
            {
                SelectedBrand = PlcBrands.FirstOrDefault();
            }
        }

        /// <summary>
        /// 连接PLC
        /// </summary>
        private void ConnectPlc()
        {
            if (string.IsNullOrEmpty(PlcConfig.Ip) || !IPAddress.TryParse(PlcConfig.Ip, out _))
            {
                MessageBox.Show("请输入有效的IP地址！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!int.TryParse(PlcConfig.Port, out int port) || port < 1 || port > 65535) // 替换 is < 1 or > 65535
            {
                MessageBox.Show("请输入有效的端口号（1-65535）！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                _plcService = PlcFactory.Create(SelectedBrand);
                bool isSuccess = _plcService != null && _plcService.Connect(PlcConfig.Ip, port); // 替换 ?. 语法的等效判断

                if (isSuccess)
                {
                    MessageBox.Show("PLC连接成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("PLC连接失败，无法建立连接！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"PLC连接失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 保存PLC配置
        /// </summary>
        private void SavePlcConfig()
        {
            try
            {
                var configHelper = ProjectConfigHelper.Instance;
                var currentConfig = configHelper.CurrentConfigs;
                if (currentConfig == null) return;

                currentConfig.PlcConfig = new PlcModels
                {
                    Ip = PlcConfig.Ip,
                    Port = PlcConfig.Port,
                    Protocol = SelectedProtocol,
                    Brand = SelectedBrand,
                    PLCAddressModels1 = PLCAddressModels1.ToList(),
                    PLCAddressModels2 = PLCAddressModels2.ToList()
                };

                configHelper.SaveConfig();
                MessageBox.Show("PLC配置已保存！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"PLC配置保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}