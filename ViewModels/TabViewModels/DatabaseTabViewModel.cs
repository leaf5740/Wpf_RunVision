using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
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
            LoadPlcConfig();
            ConnectCommand = new RelayCommand(Connect);
            SaveConfigCommand = new RelayCommand(SaveConfig);
        }
        //加载数据库配置
        private void LoadPlcConfig()
        {
            try
            {
                var configHelper = ProjectConfigHelper.Instance;
                var currentConfigs = configHelper.CurrentConfigs;
                if (currentConfigs == null) return;

                var databaseConfig = currentConfigs.DatabaseConfig;
                if (databaseConfig == null) return;

                // 同步基础配置
                SelectedDatabase = databaseConfig.Brand;
                _databaseModel.Ip = databaseConfig.Ip;
                _databaseModel.Port = databaseConfig.Port;
                _databaseModel.Password = databaseConfig.Password;

            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载PLC配置失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void Connect()
        {
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
                    Brand = SelectedDatabase,
                    Ip = DatabaseModel.Ip,
                    Port = DatabaseModel.Port,
                    Password = DatabaseModel.Password
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
