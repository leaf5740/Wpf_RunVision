using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Windows;
using Wpf_RunVision.Models;
using Wpf_RunVision.Services.Mysql;
using Wpf_RunVision.Utils;

namespace Wpf_RunVision.ViewModels.TabViewModels
{
    public partial class DatabaseTabViewModel : ObservableObject
    {
        #region 只读数据源
        /// <summary>
        /// 可用数据库类型
        /// </summary>
        public ObservableCollection<string> AvailableDatabases { get; } = new() { "MySQL" };
        #endregion

        #region 可观察属性（自动生成 getter/setter + 通知）
        /// <summary>
        /// 选中的数据库类型
        /// </summary>
        [ObservableProperty]
        private string? _selectedDatabase;

        /// <summary>
        /// 数据库配置模型
        /// </summary>
        [ObservableProperty]
        private DatabaseModel _databaseModel = new();
        #endregion

        #region 命令定义（保持原命令名，适配XAML绑定）
        /// <summary>
        /// 数据库连接测试命令（保持原命令名 ConnectCommand）
        /// </summary>
        [RelayCommand]
        private void Connect()
        {
            // 1. 校验IP地址有效性
            if (string.IsNullOrWhiteSpace(DatabaseModel.Ip) || !IPAddress.TryParse(DatabaseModel.Ip, out _))
            {
                MessageBox.Show("请输入有效的IP地址！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 2. 校验端口号有效性
            if (!int.TryParse(DatabaseModel.Port, out int port) || port is < 1 or > 65535)
            {
                MessageBox.Show("请输入有效的端口号（1-65535）！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 3. 校验必填项
            if (string.IsNullOrWhiteSpace(DatabaseModel.Password))
            {
                MessageBox.Show("密码不能为空！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(DatabaseModel.LibraryName))
            {
                MessageBox.Show("库名不能为空！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(DatabaseModel.CodeTableName))
            {
                MessageBox.Show("Code表名不能为空！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(DatabaseModel.DataTableName))
            {
                MessageBox.Show("Data表名不能为空！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            //测试mysql连接
            bool initSuccess = MySqlDataService.Instance.Initialize(
                ip: DatabaseModel.Ip,
                port: int.Parse(DatabaseModel.Port),
                database: DatabaseModel.LibraryName,
                user: "root",
                password: DatabaseModel.Password
            );
            if (initSuccess)
            {
                MessageBox.Show(initSuccess ? "连接成功" : "连接失败", initSuccess ? "提示" : "错误", MessageBoxButton.OK, initSuccess ? MessageBoxImage.Information : MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 保存配置命令（保持原命令名 SaveConfigCommand）
        /// </summary>
        [RelayCommand]
        private void SaveConfig()
        {
            try
            {
                var configHelper = ProjectConfigHelper.Instance;
                var currentConfig = configHelper.CurrentConfigs;
                if (currentConfig == null) return;

                // 组装配置数据（去除首尾空格，优化数据完整性）
                currentConfig.DatabaseConfig = new DatabaseModel
                {
                    Ip = DatabaseModel.Ip?.Trim(),
                    Port = DatabaseModel.Port?.Trim(),
                    Password = DatabaseModel.Password?.Trim(),
                    LibraryName = DatabaseModel.LibraryName?.Trim(),
                    CodeTableName = DatabaseModel.CodeTableName?.Trim(),
                    DataTableName = DatabaseModel.DataTableName?.Trim()
                };

                configHelper.SaveConfig();
                MessageBox.Show("数据库配置已保存！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"数据库配置保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region 构造函数 + 初始化逻辑
        public DatabaseTabViewModel()
        {
            // 初始化选中数据库类型
            SelectedDatabase = AvailableDatabases.FirstOrDefault();
            // 加载已保存的配置
            LoadDatabasesConfig();
        }

        /// <summary>
        /// 加载数据库配置（修复原错误提示：加载数据库却提示PLC）
        /// </summary>
        private void LoadDatabasesConfig()
        {
            try
            {
                var configHelper = ProjectConfigHelper.Instance;
                var currentConfigs = configHelper.CurrentConfigs;
                if (currentConfigs == null) return;

                var databaseConfig = currentConfigs.DatabaseConfig;
                if (databaseConfig == null) return;

                // 同步配置（避免直接引用赋值，防止原配置被意外修改）
                DatabaseModel = new DatabaseModel
                {
                    Ip = databaseConfig.Ip,
                    Port = databaseConfig.Port,
                    Password = databaseConfig.Password,
                    LibraryName = databaseConfig.LibraryName,
                    CodeTableName = databaseConfig.CodeTableName,
                    DataTableName = databaseConfig.DataTableName
                };
            }
            catch (Exception ex)
            {
                // 修复原错误提示文本
                MessageBox.Show($"加载数据库配置失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion
    }
}