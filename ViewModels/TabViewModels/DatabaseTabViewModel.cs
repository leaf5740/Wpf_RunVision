using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Windows;
using Wpf_RunVision.Models;
using Wpf_RunVision.Utils;

namespace Wpf_RunVision.ViewModels.TabViewModels
{
    public class DatabaseTabViewModel : ObservableObject
    {
        public ObservableCollection<string> AvailableDatabases { get; set; } = new ObservableCollection<string> { "MySQL" };

        private string _selectedDatabase;
        public string SelectedDatabase
        {
            get => _selectedDatabase;
            set => SetProperty(ref _selectedDatabase, value);
        }

        private DatabaseModel _databaseModel = new DatabaseModel();
        public DatabaseModel DatabaseModel
        {
            get => _databaseModel;
            set => SetProperty(ref _databaseModel, value);
        }

        public RelayCommand ConnectCommand { get; set; }
        public RelayCommand SaveConfigCommand { get; set; }

        public DatabaseTabViewModel()
        {
            SelectedDatabase = AvailableDatabases[0];
            LoadDatabasesConfig();
            ConnectCommand = new RelayCommand(Connect);
            SaveConfigCommand = new RelayCommand(SaveConfig);
        }
        //加载数据库配置
        private void LoadDatabasesConfig()
        {
            try
            {
                var configHelper = ProjectConfigHelper.Instance;
                var currentConfigs = configHelper.CurrentConfigs;
                if (currentConfigs == null) return;

                var databaseConfig = currentConfigs.DatabaseConfig;
                if (databaseConfig == null) return;

                // 同步基础配置
                _databaseModel.Ip = databaseConfig.Ip;
                _databaseModel.Port = databaseConfig.Port;
                _databaseModel.Password = databaseConfig.Password;
                _databaseModel.LibraryName = databaseConfig.LibraryName;
                _databaseModel.CodeTableName = databaseConfig.CodeTableName;
                _databaseModel.DataTableName = databaseConfig.DataTableName;

            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载PLC配置失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void Connect()
        {
            if (string.IsNullOrEmpty(DatabaseModel.Ip) || !IPAddress.TryParse(DatabaseModel.Ip, out _))
            {
                MessageBox.Show("请输入有效的IP地址！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!int.TryParse(DatabaseModel.Port, out int port) || port < 1 || port > 65535)
            {
                MessageBox.Show("请输入有效的端口号（1-65535）！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrEmpty(DatabaseModel.Password))
            {
                MessageBox.Show("密码不能为空！","提示");
                return;
            }

            if (string.IsNullOrEmpty(DatabaseModel.LibraryName))
            {
                MessageBox.Show("库名不能为空！", "提示");
                return;
            }

            if (string.IsNullOrEmpty(DatabaseModel.CodeTableName))
            {
                MessageBox.Show("Code表名不能为空！", "提示");
                return;
            }

            if (string.IsNullOrEmpty(DatabaseModel.DataTableName))
            {
                MessageBox.Show("Data表名不能为空！", "提示");
                return;
            }

            // 这里模拟连接测试
            bool success = !string.IsNullOrEmpty(DatabaseModel.Ip) && !string.IsNullOrEmpty(DatabaseModel.Port);
            MessageBox.Show(success ? "连接成功" : "连接失败");
        }

        private void SaveConfig()
        {
            try
            {
                var configHelper = ProjectConfigHelper.Instance;
                var currentConfig = configHelper.CurrentConfigs;
                if (currentConfig == null) return;

                currentConfig.DatabaseConfig = new DatabaseModel
                {
                    Ip = DatabaseModel.Ip,
                    Port = DatabaseModel.Port,
                    Password = DatabaseModel.Password,
                    LibraryName = DatabaseModel.LibraryName,
                    CodeTableName = DatabaseModel.CodeTableName,
                    DataTableName = DatabaseModel.DataTableName
                };

                configHelper.SaveConfig();
                MessageBox.Show("数据库配置已保存！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"数据库配置保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
