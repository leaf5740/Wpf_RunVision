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
    public partial class PlcTabViewModel : ObservableObject
    {
        #region 只读数据源（绑定下拉框）
        /// <summary>
        /// 可用通讯协议列表
        /// </summary>
        public ObservableCollection<string> AvailableProtocols { get; } = new() { "ModbusTCP" };

        /// <summary>
        /// PLC品牌列表
        /// </summary>
        public ObservableCollection<string> PlcBrands { get; } = new() { "汇川", "三菱" };
        #endregion

        #region 可观察属性（自动生成通知）
        /// <summary>
        /// 选中的通讯协议
        /// </summary>
        [ObservableProperty]
        private string? _selectedProtocol;

        /// <summary>
        /// 选中的PLC品牌
        /// </summary>
        [ObservableProperty]
        private string? _selectedBrand;

        /// <summary>
        /// PLC配置模型（IP、端口等）
        /// </summary>
        [ObservableProperty]
        private PlcModel _plcConfig = new();

        /// <summary>
        /// 读取地址列表（绑定左侧DataGrid）
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<PLCAddressModels> _readPLCAddress = new();

        /// <summary>
        /// 写入地址列表（绑定右侧DataGrid）
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<PLCAddressModels> _writePLCAddress = new();
        #endregion

        #region 依赖服务
        /// <summary>
        /// PLC通讯服务
        /// </summary>
        private IPlcService? _plcService;
        #endregion

        #region 命令定义（特性自动生成）
        /// <summary>
        /// PLC连接测试命令
        /// </summary>
        [RelayCommand]
        private void Connect()
        {
            // 校验是否选中PLC品牌
            if (string.IsNullOrWhiteSpace(SelectedBrand))
            {
                MessageBox.Show($"请先选中PLC品牌！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 校验IP地址有效性
            if (string.IsNullOrEmpty(PlcConfig.Ip) || !IPAddress.TryParse(PlcConfig.Ip, out _))
            {
                MessageBox.Show("请输入有效的IP地址！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 校验端口号有效性
            if (!int.TryParse(PlcConfig.Port, out int port) || port is < 1 or > 65535)
            {
                MessageBox.Show("请输入有效的端口号（1-65535）", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 校验品牌选中
            if (string.IsNullOrEmpty(SelectedBrand))
            {
                MessageBox.Show("请先选择PLC品牌！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 创建对应品牌的PLC服务
                _plcService = PlcFactory.Create(SelectedBrand);
                if (_plcService == null)
                {
                    MessageBox.Show($"不支持的PLC品牌：{SelectedBrand}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 尝试连接
                bool isSuccess = _plcService.Connect(PlcConfig.Ip, port);

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
        /// 保存PLC配置命令
        /// </summary>
        [RelayCommand]
        private void SaveConfig()
        {
            try
            {
                var configHelper = ProjectConfigHelper.Instance;
                var currentConfig = configHelper.CurrentConfigs;
                if (currentConfig == null) return;

                // 组装配置数据
                currentConfig.PlcConfig = new PlcModel
                {
                    Ip = PlcConfig.Ip,
                    Port = PlcConfig.Port,
                    Protocol = SelectedProtocol,
                    Brand = SelectedBrand,
                    ReadPLCAddress = ReadPLCAddress.ToList(),
                    WritePLCAddress = WritePLCAddress.ToList()
                };

                // 保存到配置文件
                configHelper.SaveConfig();
                MessageBox.Show("PLC配置已保存！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"PLC配置保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region 构造函数 + 初始化逻辑
        public PlcTabViewModel()
        {
            // 初始化默认地址数据
            InitializeAddressModels();
            // 加载已保存的配置
            LoadPlcConfig();
            // 初始化下拉框选中项
            InitializeSelectedItems();
        }

        /// <summary>
        /// 初始化默认的读写地址模型
        /// </summary>
        private void InitializeAddressModels()
        {
            // 初始化读取地址
            var readSignals = new List<string> { "进板信号", "心跳信号", "复位完成信号" };
            foreach (var name in readSignals)
            {
                ReadPLCAddress.Add(new PLCAddressModels
                {
                    Name = name,
                    Address = "null",
                    Value = "null",
                    Remark = "null"
                });
            }

            // 初始化写入地址
            var writeSignals = new List<string> { "扫码枪失败信号", "检测完成信号", "复位完成信号" };
            foreach (var name in writeSignals)
            {
                WritePLCAddress.Add(new PLCAddressModels
                {
                    Name = name,
                    Address = "null",
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
                if (currentConfigs?.PlcConfig == null) return;

                var savedConfig = currentConfigs.PlcConfig;

                // 同步基础配置
                PlcConfig.Ip = savedConfig.Ip;
                PlcConfig.Port = savedConfig.Port;
                PlcConfig.Protocol = savedConfig.Protocol;
                PlcConfig.Brand = savedConfig.Brand;
                
                // 同步读取地址（覆盖默认值）
                if (savedConfig.ReadPLCAddress?.Any() == true)
                {
                    ReadPLCAddress.Clear();
                    savedConfig.ReadPLCAddress.ToList().ForEach(ReadPLCAddress.Add);
                }

                // 同步写入地址（覆盖默认值）
                if (savedConfig.WritePLCAddress?.Any() == true)
                {
                    WritePLCAddress.Clear();
                    savedConfig.WritePLCAddress.ToList().ForEach(WritePLCAddress.Add);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载PLC配置失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 初始化下拉框选中项（优先用保存的配置，无则选第一个）
        /// </summary>
        private void InitializeSelectedItems()
        {
            // 初始化通讯协议选中项
            SelectedProtocol = !string.IsNullOrEmpty(PlcConfig.Protocol) && AvailableProtocols.Contains(PlcConfig.Protocol)
                ? PlcConfig.Protocol
                : AvailableProtocols.FirstOrDefault();

            // 初始化PLC品牌选中项
            SelectedBrand = !string.IsNullOrEmpty(PlcConfig.Brand) && PlcBrands.Contains(PlcConfig.Brand)
                ? PlcConfig.Brand
                : null;
        }
        #endregion
    }
}