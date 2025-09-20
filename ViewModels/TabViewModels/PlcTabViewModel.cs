using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using Wpf_RunVision.Services.Plc;
using Wpf_RunVision.Utils;
using Wpf_RunVision.Models;

namespace Wpf_RunVision.ViewModels.TabViewModels
{
    /// <summary>
    /// PLC 配置 Tab 页的 ViewModel
    /// 用于绑定 PLC 的品牌、协议、IP、端口等信息，并提供连接与保存操作
    /// </summary>
    public class PlcTabViewModel : ObservableObject
    {
        #region 字段

        // PLC 连接命令
        private RelayCommand _connectCommand;

        // 保存 PLC 配置命令
        private RelayCommand _saveConfigCommand;

        #endregion

        #region 属性

        /// <summary>
        /// 当前 PLC 配置模型对象
        /// </summary>
        public PlcModels PlcConfig { get; set; }

        /// <summary>
        /// PLC 品牌列表（如三菱、汇川）
        /// 用于绑定 ComboBox
        /// </summary>
        public ObservableCollection<string> PlcBrands { get; }

        /// <summary>
        /// 当前选中品牌支持的通信协议列表
        /// </summary>
        public ObservableCollection<PlcProtocol> AvailableProtocols { get; }

        /// <summary>
        /// 当前选中的 PLC 品牌
        /// 当品牌变化时，刷新可用协议列表
        /// </summary>
        public string SelectedBrand
        {
            get { return PlcConfig.Brand; }
            set
            {
                if (PlcConfig.Brand != value)
                {
                    PlcConfig.Brand = value;
                    OnPropertyChanged(); // 通知 UI 更新
                    UpdateProtocolsByBrand(value);
                }
            }
        }

        /// <summary>
        /// 当前选中的 PLC 协议
        /// </summary>
        public PlcProtocol SelectedProtocol
        {
            get { return PlcConfig.Protocol; }
            set
            {
                if (PlcConfig.Protocol != value)
                {
                    PlcConfig.Protocol = value;
                    OnPropertyChanged(); // 通知 UI 更新
                }
            }
        }

        #endregion

        #region 构造函数

        public PlcTabViewModel()
        {
            // 从配置文件读取 PLC 配置，如果为空则创建新的默认对象
            PlcConfig = ProjectConfigHelper.Instance.CurrentConfigs.PlcConfig ?? new PlcModels();

            // 初始化品牌列表
            PlcBrands = new ObservableCollection<string>(new[] { "汇川", "三菱", });

            // 初始化可用协议列表
            AvailableProtocols = new ObservableCollection<PlcProtocol>();

            // 如果配置文件中已有品牌信息，则刷新可用协议
            if (!string.IsNullOrEmpty(PlcConfig.Brand))
                UpdateProtocolsByBrand(PlcConfig.Brand);
        }

        #endregion

        #region 方法

        /// <summary>
        /// 根据 PLC 品牌刷新可用协议列表
        /// </summary>
        /// <param name="brand">选中的 PLC 品牌</param>
        private void UpdateProtocolsByBrand(string brand)
        {
            AvailableProtocols.Clear();

            if (string.IsNullOrEmpty(brand)) return;

            // 创建对应品牌的 PLC 服务实例
            var plcService = PlcFactory.Create(brand);

            // 遍历支持的协议，添加到列表中
            foreach (var p in plcService.SupportedProtocols)
                AvailableProtocols.Add(p);

            // 默认选中第一个协议
            if (AvailableProtocols.Count > 0)
                SelectedProtocol = AvailableProtocols[0];
        }

        #endregion

        #region 命令

        /// <summary>
        /// 连接 PLC 命令
        /// 点击执行连接操作，连接成功或失败提示用户
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
                            // 创建对应品牌的 PLC 服务并连接
                            var plc = PlcFactory.Create(PlcConfig.Brand);
                            plc.Connect(PlcConfig.Ip, port, PlcConfig.Protocol);

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
                            ProjectConfigHelper.Instance.CurrentConfigs.PlcConfig = PlcConfig;
                            ProjectConfigHelper.Instance.SaveConfig();

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

        #endregion
    }
}
