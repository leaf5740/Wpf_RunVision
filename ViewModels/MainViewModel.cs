using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyControl.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using VM.Core;
using VMControls.WPF.Release;
using Wpf_RunVision.Models;
using Wpf_RunVision.Services;
using Wpf_RunVision.Utils;
using Wpf_RunVision.Views;

namespace Wpf_RunVision.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly string _projectRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Projects");

        /// <summary>
        /// 存储方案名与对应文件夹路径的映射
        /// </summary>
        public Dictionary<string, string> SchemeFolders { get; } = new Dictionary<string, string>();

        /// <summary>
        /// 菜单绑定的方案集合（含"无方案"占位）
        /// </summary>
        public ObservableCollection<string> Schemes { get; } = new ObservableCollection<string>();

        // 程序启动时间（用于计算总运行时长）
        private readonly DateTime _startTime;
        // 实时时长更新定时器（UI线程安全）
        private readonly DispatcherTimer _runTimeTimer;
        public MainViewState ViewState => MainViewState.Instance;

        private VisionCoreService visionCoreService;

        /// <summary>
        /// 当前用户权限（显示在界面）
        /// </summary>
        private string _currentPermission = "当前权限：员工";
        public string CurrentPermission
        {
            get => _currentPermission;
            set => SetProperty(ref _currentPermission, value);
        }

        /// <summary>
        /// 当前选择的方案（显示在界面）
        /// </summary>
        private string _currentScheme = "当前方案：null";
        public string CurrentScheme
        {
            get => _currentScheme;
            set => SetProperty(ref _currentScheme, value);
        }

        /// <summary>
        /// 前端控件（绑定UI）
        /// </summary>
        private VmFrontendControl _frontendControl;
        public VmFrontendControl FrontendControl
        {
            get => _frontendControl;
            set => SetProperty(ref _frontendControl, value);
        }

        /// <summary>
        /// 是否允许编辑方案（登录后禁用）
        /// </summary>
        private bool _canEditScheme = true;
        public bool CanEditScheme
        {
            get => _canEditScheme;
            set => SetProperty(ref _canEditScheme, value);
        }

        private string _totalRunTime = "0.00:00:00";
        public string TotalRunTime
        {
            get => _totalRunTime;
            set => SetProperty(ref _totalRunTime, value);
        }
        /// <summary>
        /// 特殊标记：无方案占位符
        /// </summary>
        private const string NoSchemePlaceholder = "无方案";

        public MainViewModel()
        {
            // 1. 初始化运行时长相关（启动时间+定时器）
            _startTime = DateTime.Now;
            _runTimeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1) // 每秒更新一次时长
            };
            _runTimeTimer.Tick += RunTimeTimer_Tick; // 绑定定时器事件
            _runTimeTimer.Start(); // 启动定时器

            // 测试绑定（实际运行时可删除）
            ViewState.EtchingCode = "00000000000000000000";
            ViewState.PaperCode = "FEGIWEUC";
            ViewState.DbStatus = false;
            ViewState.PlcStatus = true;
            ViewState.MesStatus = false;
            ViewState.NasStatus = false;
            ViewState.ProgressValue = 65;
            ViewState.CtTime = "16秒";
            ViewState.SingleFlowTime = "3秒";
            // 异步加载方案列表（避免阻塞UI线程）
            _ = LoadSchemesAsync();
        }

        /// <summary>
        /// 异步加载Projects文件夹下的有效方案
        /// 有效方案定义：包含*.sol文件的文件夹
        /// </summary>
        private async Task LoadSchemesAsync()
        {
            try
            {
                // 确保Projects文件夹存在
                if (!Directory.Exists(_projectRoot))
                {
                    Directory.CreateDirectory(_projectRoot);
                    MyLogger.Info($"Projects文件夹不存在，已自动创建：{_projectRoot}");
                }

                // 后台线程执行文件扫描（避免阻塞UI）
                var folders = await Task.Run(() => Directory.GetDirectories(_projectRoot));

                // 清空原有数据（UI操作需在主线程）
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Schemes.Clear();
                    SchemeFolders.Clear();
                });

                // 遍历文件夹，筛选有效方案
                foreach (var folder in folders)
                {
                    // 后台线程查找.sol文件（避免UI卡顿）
                    var solFile = await Task.Run(() =>
                        Directory.GetFiles(folder, "*.sol", SearchOption.TopDirectoryOnly).FirstOrDefault()
                    );

                    if (solFile != null)
                    {
                        string schemeName = Path.GetFileName(folder);
                        // 主线程更新集合（UI绑定需主线程）
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            Schemes.Add(schemeName);
                            SchemeFolders[schemeName] = folder;
                        });

                        // 生成默认配置（若不存在）
                        await EnsureDefaultConfigAsync(folder);
                    }
                }

                // 处理无方案场景（主线程更新UI）
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (Schemes.Count == 0)
                    {
                        Schemes.Add(NoSchemePlaceholder);
                        CurrentScheme = $"当前方案：{NoSchemePlaceholder}";
                        MyLogger.Warn("未检测到任何有效方案（需包含*.sol文件）");
                    }
                    else
                    {
                        MyLogger.Info($"成功加载{Schemes.Count}个有效方案");
                    }
                });
            }
            catch (Exception ex)
            {
                MyLogger.Error($"方案列表加载失败", ex);
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"加载方案失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        /// <summary>
        /// 确保方案文件夹下存在默认配置文件（config.json）
        /// 不存在则生成默认配置
        /// </summary>
        private async Task EnsureDefaultConfigAsync(string schemeFolder)
        {
            try
            {
                string configPath = Path.Combine(schemeFolder, "config.json");
                if (!File.Exists(configPath))
                {
                    // 后台线程生成并保存默认配置（避免阻塞UI）
                    await Task.Run(() =>
                    {
                        ProjectConfigHelper.Instance.SaveConfig();
                        MyLogger.Info($"方案[{Path.GetFileName(schemeFolder)}]缺少config.json，已生成默认配置");
                    });
                }
            }
            catch (Exception ex)
            {
                MyLogger.Error($"方案[{Path.GetFileName(schemeFolder)}]默认配置生成失败", ex);
            }
        }

        /// <summary>
        /// 打开方案配置窗口
        /// </summary>
        public ICommand OpenSchemeCommand => new RelayCommand(() =>
        {
            ViewState.ProgressValue = 11;
            try
            {
                var mainWindow = Application.Current.MainWindow;
                if (mainWindow == null) return;

                var schemeConfigWindow = new SchemeConfigWindow
                {
                    Owner = mainWindow,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                schemeConfigWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开配置窗口失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        });

        /// <summary>
        /// 切换用户权限
        /// </summary>
        public ICommand TogglePermissionCommand => new RelayCommand(() =>
        {
            try
            {
                if (CurrentPermission == "当前权限：工程师")
                {
                    CurrentPermission = "当前权限：员工";
                    CanEditScheme = false;
                    MyLogger.Info("用户权限切换为：员工");
                }
                else
                {
                    var mainWindow = Application.Current.MainWindow;
                    if (mainWindow == null) return;

                    var permissionWindow = new PermissionWindow
                    {
                        Owner = mainWindow,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };

                    bool? dialogResult = permissionWindow.ShowDialog();
                    if (dialogResult == true)
                    {
                        CurrentPermission = "当前权限：工程师";
                        CanEditScheme = true;
                        MyLogger.Info("用户权限切换为：工程师");
                    }
                }

                // 统一关闭软键盘（避免残留）
                KeyboardHelper.CloseKeyboard();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"切换权限失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        });

        /// <summary>
        /// 切换方案命令
        /// 步骤：1. 校验方案有效性 2. 释放旧资源 3. 加载新配置 4. 初始化核心服务 5. 更新UI
        /// </summary>
        public ICommand SwitchSchemeCommand => new RelayCommand<string>(async schemeName =>
        {
            // 校验方案有效性（排除占位符）
            if (string.IsNullOrWhiteSpace(schemeName) || schemeName == NoSchemePlaceholder)
            {
                MyLogger.Warn("方案切换失败：无可用方案（需先创建/导入有效方案）");
                MessageBox.Show("请先创建或导入有效方案", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 避免重复加载同一方案
            if (CurrentScheme == $"当前方案：{schemeName}")
            {
                MyLogger.Warn($"方案切换跳过：[{schemeName}]已是当前方案");
                return;
            }

            try
            {
                string solFile = null;
                string schemeFolder = null;


                // 阶段1：后台线程执行耗时操作（释放旧资源、加载配置）
                await Task.Run(() =>
                {
                    // 获取方案文件夹路径
                    if (!SchemeFolders.TryGetValue(schemeName, out schemeFolder))
                    {
                        throw new Exception($"方案路径不存在");
                    }

                    // 加载方案配置
                    ProjectConfigHelper.Instance.LoadConfig(schemeFolder);

                    // 查找.sol核心文件
                    solFile = Directory.GetFiles(schemeFolder, "*.sol", SearchOption.TopDirectoryOnly).FirstOrDefault();
                    if (solFile == null)
                    {
                        throw new Exception($"缺少核心文件*.sol");
                    }

                    // 关闭旧方案并回收内存
                    try
                    {
                        VmSolution.Instance.CloseSolution();
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                    catch (Exception ex)
                    {
                        MyLogger.Warn($"旧方案关闭警告：{ex.Message}");
                    }
                });

                // 阶段2：主线程执行UI相关操作（创建控件、初始化服务）
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // 创建前端控件（必须在UI线程）
                    FrontendControl = new VmFrontendControl();

                    // 加载.sol方案
                    VmSolution.Load(solFile);

                    // 更新UI状态
                    CurrentScheme = $"当前方案：{schemeName}";
                    MyLogger.Info($"方案[{schemeName}]加载完成");
                    visionCoreService = new VisionCoreService(ProjectConfigHelper.Instance.CurrentConfigs);


                });
            }
            catch (Exception ex)
            {
                // 加载失败时恢复状态
                CurrentScheme = "当前方案：null";
                MyLogger.Error($"方案[{schemeName}]加载失败", ex);
                MessageBox.Show($"方案加载失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            

        });

        /// <summary>
        /// 定时器事件：每秒更新程序总运行时长
        /// </summary>
        /// <summary>
        /// 定时器事件：每秒更新程序总运行时长（改为天时分秒格式）
        /// </summary>
        private void RunTimeTimer_Tick(object sender, EventArgs e)
        {
            TimeSpan runDuration = DateTime.Now - _startTime;
            // 中文格式化：天、时、分、秒
            TotalRunTime = $"{runDuration.Days}天{runDuration.Hours}小时{runDuration.Minutes}分{runDuration.Seconds}秒";
        }

        /// <summary>
        /// 窗口关闭命令（释放资源）
        /// </summary>
        public ICommand WindowClosedCommand => new RelayCommand(() =>
        {
            if (visionCoreService != null)
            {
                visionCoreService.DestroyAllCameras();
            }
            // 停止运行时长定时器（避免内存泄漏）
            _runTimeTimer.Stop();
            _runTimeTimer.Tick -= RunTimeTimer_Tick;
            MyLogger.Info("运行时长定时器已停止");
            MyLogger.Info("程序正常关闭，所有资源已释放");
        });
    }
}