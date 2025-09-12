using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using VM.Core;
using VMControls.WPF.Release.Front;
using Wpf_RunVision.Tools;
using Wpf_RunVision.Views;
using YourApp.Tools;

namespace Wpf_RunVision.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly string projectRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Projects");

        // 方案名 → 文件夹路径
        public Dictionary<string, string> SchemeFolders { get; } = new Dictionary<string, string>();

        // 方案集合（用于菜单绑定）
        public ObservableCollection<string> Schemes { get; } = new ObservableCollection<string>();

        private string _currentPermission = "当前权限：员工";
        public string CurrentPermission
        {
            get => _currentPermission;
            set => SetProperty(ref _currentPermission, value); 
        }

        private string _currentScheme = "当前方案：null";
        public string CurrentScheme
        {
            get => _currentScheme;
            set => SetProperty(ref _currentScheme, value); 
        }

        public MainViewModel()
        {
            // 初始化方案列表
            LoadSchemes();

        }

        /// <summary>
        /// 加载 Projects 文件夹下有效方案
        /// </summary>
        private void LoadSchemes()
        {
            if (!Directory.Exists(projectRoot))
                Directory.CreateDirectory(projectRoot);

            Schemes.Clear();
            SchemeFolders.Clear();

            var folders = Directory.GetDirectories(projectRoot);
            foreach (var folder in folders)
            {
                // 只算包含 .sol 文件的文件夹为有效方案
                var solFile = Directory.GetFiles(folder, "*.sol").FirstOrDefault();
                if (solFile != null)
                {
                    string folderName = Path.GetFileName(folder);
                    Schemes.Add(folderName);
                    SchemeFolders[folderName] = folder; // 保存路径

                    // 检查配置文件是否存在
                    string configFile = Path.Combine(folder, "config.json");
                    if (!File.Exists(configFile))
                    {
                        ProjectConfigHelper.Instance.CurrentConfig.Name = folderName;
                        ProjectConfigHelper.Instance.CurrentConfig.Exposure = 50;
                        ProjectConfigHelper.Instance.SaveConfig();

                    }
                }
            }

            //// 设置默认选择第一个方案
            //if (Schemes.Count > 0)
            //    CurrentScheme = $"当前方案：{Schemes[0]}";
        }

        /// <summary>
        /// 方案配置事件
        /// </summary>
        public ICommand OpenSchemeCommand => new RelayCommand(() =>
        {
            // 获取全局主窗口实例
            var mainWindow = Application.Current.MainWindow;
            // 创建子窗口并设置 Owner
            SchemeConfigWindow schemeConfigWindow = new SchemeConfigWindow();
            // 绑定到主窗口
            schemeConfigWindow.Owner = mainWindow; 
            // 居中显示
            schemeConfigWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner; 
            // 显示模态窗口
            bool? result = schemeConfigWindow.ShowDialog();
            if (result == true)
            {
                // 处理确认逻辑
            }
        });

        /// <summary>
        /// 登录事件
        /// </summary>
        public ICommand TogglePermissionCommand => new RelayCommand(() =>
        {
            
            if (CurrentPermission == "当前权限：工程师")
            {
                CurrentPermission = "当前权限：员工";
                return;
            }
            var mainWindow = Application.Current.MainWindow;
            PermissionWindow permissionWindow = new PermissionWindow();
            permissionWindow.Owner = mainWindow;
            permissionWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            bool? dia = permissionWindow.ShowDialog();
            if (dia == true)
            {
                CurrentPermission = "当前权限：工程师";
            }
            else
            {
                KeyboardHelper.CloseKeyboard();
            }
        });

        /// <summary>
        /// 加载切换方案事件
        /// </summary>
        public ICommand SwitchSchemeCommand => new RelayCommand<string>(async schemeName =>
        {
            if (string.IsNullOrEmpty(schemeName))
                return;

            if (CurrentScheme == $"当前方案：{schemeName}")
            {
                MyLogger.Warn($"方案 [{schemeName}] 已经是当前方案，跳过加载。");
                return;
            }

            try
            {
                // 可选：这里创建 Progress<int> 来更新绑定属性或日志
                var progress = new Progress<int>(p =>
                {
                    // TODO: 如果有进度条绑定，可以在这里更新
                    MyLogger.Info($"加载进度: {p}%");
                });

                // 异步加载方案
                await Task.Run(() =>
                {
                    if (SchemeFolders.TryGetValue(schemeName, out string folder))
                    {
                        ProjectConfigHelper.Instance.LoadConfig(folder);
                        (progress as IProgress<int>)?.Report(30);

                        string solFile = Directory.GetFiles(folder, "*.sol").FirstOrDefault();
                        (progress as IProgress<int>)?.Report(60);

                        if (!string.IsNullOrEmpty(solFile))
                        {
                            VmSolution.Load(solFile);
                            (progress as IProgress<int>)?.Report(100);
                        }
                        else
                        {
                            MyLogger.Error("未找到 .sol 文件");
                        }
                    }
                });
                CurrentScheme = $"当前方案：{schemeName}";
                MyLogger.Info($"方案 [{schemeName}] 加载完成。");
            }
            catch (Exception ex)
            {
                MyLogger.Error($"加载方案失败: {ex.Message}");
            }
        });

        /// <summary>
        /// 监听关闭窗口事件
        /// </summary>
        public ICommand WindowClosedCommand => new RelayCommand(() =>
        {
            MyLogger.Info("程序正常关闭！");
        });

    }
}
