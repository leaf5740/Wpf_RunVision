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
using VMControls.WPF.Release;
using Wpf_RunVision.Tools;
using Wpf_RunVision.Views;

namespace Wpf_RunVision.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly string projectRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Projects");

        // 存储方案名和对应文件夹路径
        public Dictionary<string, string> SchemeFolders { get; } = new Dictionary<string, string>();

        // 菜单绑定使用的方案集合
        public ObservableCollection<string> Schemes { get; } = new ObservableCollection<string>();

        // 当前用户权限（显示在界面）
        private string _currentPermission = "当前权限：员工";
        public string CurrentPermission
        {
            get => _currentPermission;
            set => SetProperty(ref _currentPermission, value);
        }

        // 当前选择的方案（显示在界面）
        private string _currentScheme = "当前方案：null";
        public string CurrentScheme
        {
            get => _currentScheme;
            set => SetProperty(ref _currentScheme, value);
        }

        // 用于绑定 UI 的前端控件
        private VmFrontendControl _frontendControl;
        public VmFrontendControl FrontendControl
        {
            get => _frontendControl;
            set => SetProperty(ref _frontendControl, value);
        }

        public MainViewModel()
        {
            // 初始化加载方案列表
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

                    // 如果配置文件不存在，则生成默认配置
                    string configFile = Path.Combine(folder, "config.json");
                    if (!File.Exists(configFile))
                    {
                        ProjectConfigHelper.Instance.CurrentConfig.Name = folderName;
                        ProjectConfigHelper.Instance.CurrentConfig.Exposure = 50;
                        ProjectConfigHelper.Instance.SaveConfig();
                    }
                }
            }

            // 如果没有方案，则显示占位
            if (Schemes.Count == 0)
            {
                Schemes.Add("无方案");
                CurrentScheme = "当前方案：无方案";
            }
        }

        /// <summary>
        /// 打开方案配置窗口
        /// </summary>
        public ICommand OpenSchemeCommand => new RelayCommand(() =>
        {
            var mainWindow = Application.Current.MainWindow;
            SchemeConfigWindow schemeConfigWindow = new SchemeConfigWindow();
            schemeConfigWindow.Owner = mainWindow;
            schemeConfigWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            schemeConfigWindow.ShowDialog();
        });

        /// <summary>
        /// 切换用户权限（员工/工程师）
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
                CurrentPermission = "当前权限：工程师";
            else
                KeyboardHelper.CloseKeyboard();
        });

        /// <summary>
        /// 切换方案命令
        /// </summary>
        public ICommand SwitchSchemeCommand => new RelayCommand<string>(async schemeName =>
        {
            if (string.IsNullOrWhiteSpace(schemeName) || schemeName == "无方案")
            {
                MyLogger.Warn("当前没有可用方案，操作无效。");
                return;
            }

            if (CurrentScheme == $"当前方案：{schemeName}")
            {
                MyLogger.Warn($"方案 [{schemeName}] 已经是当前方案，跳过加载。");
                return;
            }

            try
            {
                string solFile = null;
                string folder = null;

                // 后台线程：加载配置 + 查找 .sol 文件 + 关闭旧方案
                await Task.Run(() =>
                {
                    if (!SchemeFolders.TryGetValue(schemeName, out folder))
                        return;

                    // 加载配置
                    ProjectConfigHelper.Instance.LoadConfig(folder);
                    MyLogger.Info($"方案 [{schemeName}] 加载进度: 30%");

                    // 查找 .sol 文件
                    solFile = Directory.GetFiles(folder, "*.sol").FirstOrDefault();
                    MyLogger.Info($"方案 [{schemeName}] 加载进度: 60%");

                    // 关闭旧方案 + 垃圾回收
                    try
                    {
                        VmSolution.Instance.CloseSolution();
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                    catch (Exception ex)
                    {
                        MyLogger.Error($"关闭旧方案失败: {ex.Message}");
                    }
                });

                if (string.IsNullOrEmpty(solFile))
                {
                    MyLogger.Error($"方案 [{schemeName}] 未找到 .sol 文件");
                    return;
                }

                // UI线程：创建新控件并加载方案
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        FrontendControl = new VmFrontendControl(); // 必须在UI线程创建
                        VmSolution.Load(solFile);                   // 如果 Load 涉及控件，可在UI线程执行
                        CurrentScheme = $"当前方案：{schemeName}";
                        MyLogger.Info($"方案 [{schemeName}] 加载进度: 100%");
                        MyLogger.Info($"方案 [{schemeName}] 加载完成");
                    }
                    catch (Exception ex)
                    {
                        MyLogger.Error($"方案 [{schemeName}] 加载失败: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                MyLogger.Error($"加载方案 [{schemeName}] 过程中发生异常: {ex}");
            }
        });




        /// <summary>
        /// 关闭窗口事件
        /// </summary>
        public ICommand WindowClosedCommand => new RelayCommand(() =>
        {
            MyLogger.Info("程序正常关闭！");
        });
    }
}
