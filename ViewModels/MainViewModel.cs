using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyControl.Tools;
using MySqlX.XDevAPI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using VM.Core;
using VMControls.WPF.Release;
using Wpf_RunVision.Models;
using Wpf_RunVision.Services;
using Wpf_RunVision.Utils;
using Wpf_RunVision.Views;

namespace Wpf_RunVision.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        #region 常量定义（规范命名，集中管理）
        /// <summary>
        /// 项目根目录（程序目录下的Projects文件夹）
        /// </summary>
        private readonly string _projectRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Projects");

        /// <summary>
        /// 方案核心文件后缀
        /// </summary>
        private const string SchemeCoreFileSuffix = "*.sol";

        /// <summary>
        /// 方案配置文件名
        /// </summary>
        private const string SchemeConfigFileName = "config.json";

        /// <summary>
        /// 无方案占位符
        /// </summary>
        private const string NoSchemePlaceholder = "无方案";

        /// <summary>
        /// 权限标识 - 员工
        /// </summary>
        private const string PermissionStaff = "员工";

        /// <summary>
        /// 权限标识 - 工程师
        /// </summary>
        private const string PermissionEngineer = "工程师";
        #endregion

        #region 绑定属性（用特性简化，自动生成通知）
        /// <summary>
        /// 存储方案名与对应文件夹路径的映射（只读，避免外部修改）
        /// </summary>
        public IReadOnlyDictionary<string, string> SchemeFolders { get; } = new Dictionary<string, string>();

        /// <summary>
        /// 菜单绑定的方案集合（含"无方案"占位）
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<string> _schemes = new();

        /// <summary>
        /// 当前用户权限（显示在界面）
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsStaffPermission))]
        [NotifyPropertyChangedFor(nameof(IsEngineerPermission))]
        private string _currentPermission = $"当前权限：{PermissionStaff}";

        /// <summary>
        /// 当前选择的方案（显示在界面）
        /// </summary>
        [ObservableProperty]
        private string _currentScheme = $"当前方案：{NoSchemePlaceholder}";

        /// <summary>
        /// 前端控件（绑定UI）
        /// </summary>
        [ObservableProperty]
        private VmFrontendControl? _frontendControl;

        /// <summary>
        /// 是否允许编辑方案（工程师权限可编辑）
        /// </summary>
        [ObservableProperty]
        private bool _canEditScheme;

        /// <summary>
        /// 是否显示加载遮罩（UI线程安全）
        /// </summary>
        [ObservableProperty]
        private Visibility _isAnimation = Visibility.Hidden;

        /// <summary>
        /// 总运行时间
        /// </summary>
        [ObservableProperty]
        private string _totalRunTime = "00:00:00";

        [ObservableProperty]
        public string _etchingCode = "未检测";
        [ObservableProperty]
        public string _paperCode = "未检测";
        [ObservableProperty]
        public bool _plcStatus = false;
        [ObservableProperty]
        public bool _dbStatus =false;
        [ObservableProperty]
        public bool _nasStatus = false;
        [ObservableProperty]
        public double _progressValue= 0;
        [ObservableProperty]
        public string _ctTime = "00:00.00";
        [ObservableProperty]
        public string _singleFlowTime = "00:00.00";
       
        /// <summary>
        /// 是否为员工权限（简化UI绑定逻辑）
        /// </summary>
        public bool IsStaffPermission => CurrentPermission == $"当前权限：{PermissionStaff}";

        /// <summary>
        /// 是否为工程师权限（简化UI绑定逻辑）
        /// </summary>
        public bool IsEngineerPermission => CurrentPermission == $"当前权限：{PermissionEngineer}";
        #endregion

        #region 私有字段（线程安全相关）
        /// <summary>
        /// 程序启动时间（用于计算总运行时长）
        /// </summary>
        private readonly DateTime _startTime = DateTime.Now;

        /// <summary>
        /// 实时时长更新定时器（UI线程安全）
        /// </summary>
        private readonly DispatcherTimer _runTimeTimer;

        /// <summary>
        /// 方案文件夹映射（内部可修改，外部暴露只读）
        /// </summary>
        private readonly Dictionary<string, string> _internalSchemeFolders = new();

        /// <summary>
        /// 核心类：视觉服务管理器（方案切换时初始化）
        /// </summary>
        private VisionServiceManager visionServiceManager;

        #endregion

        #region 构造函数（初始化逻辑集中管理）
        public MainViewModel()
        {
            // 初始化方案文件夹映射（外部只读封装）
            SchemeFolders = _internalSchemeFolders;

            // 初始化运行时长定时器
            _runTimeTimer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Normal, RunTimeTimer_Tick, Application.Current.Dispatcher);

            // 异步加载方案列表（不阻塞UI线程）
            _ = LoadSchemesAsync().ConfigureAwait(false);

            // 初始化权限对应的编辑状态
            UpdateEditSchemePermission();
        }
        #endregion

        #region 方案加载逻辑（优化异步、线程安全）
        /// <summary>
        /// 异步加载Projects文件夹下的有效方案
        /// 有效方案定义：包含*.sol文件的文件夹
        /// </summary>
        private async Task LoadSchemesAsync()
        {
            try
            {
                // 确保Projects文件夹存在（后台线程执行）
                await EnsureProjectRootExistsAsync();

                // 后台线程扫描文件夹（避免阻塞UI）
                var validSchemes = await ScanValidSchemesAsync();

                // 主线程更新UI绑定集合（必须UI线程）
                await UpdateSchemesCollectionAsync(validSchemes);

                MyLogger.Info($"方案加载完成：共找到{validSchemes.Count}个有效方案");
            }
            catch (Exception ex)
            {
                MyLogger.Error("方案列表加载失败", ex);
            }
        }

        /// <summary>
        /// 确保项目根目录存在
        /// </summary>
        private async Task EnsureProjectRootExistsAsync()
        {
            await Task.Run(() =>
            {
                if (!Directory.Exists(_projectRoot))
                {
                    Directory.CreateDirectory(_projectRoot);
                    MyLogger.Info($"Projects文件夹不存在，已自动创建：{_projectRoot}");
                }
            });
        }

        /// <summary>
        /// 后台线程扫描有效方案（含配置文件检查）
        /// </summary>
        private async Task<List<(string SchemeName, string SchemePath)>> ScanValidSchemesAsync()
        {
            var validSchemes = new List<(string, string)>();

            // 后台线程扫描文件夹
            var folders = await Task.Run(() => Directory.GetDirectories(_projectRoot));

            foreach (var folder in folders)
            {
                // 检查是否包含核心.sol文件
                var hasCoreFile = await Task.Run(() =>
                    Directory.GetFiles(folder, SchemeCoreFileSuffix, SearchOption.TopDirectoryOnly).Any()
                );

                if (hasCoreFile)
                {
                    string schemeName = Path.GetFileName(folder);
                    validSchemes.Add((schemeName, folder));

                    // 确保配置文件存在（后台线程生成）
                    await EnsureSchemeConfigExistsAsync(folder, schemeName);
                }
            }

            return validSchemes;
        }

        /// <summary>
        /// 确保方案文件夹下存在配置文件（不存在则生成默认配置）
        /// </summary>
        private async Task EnsureSchemeConfigExistsAsync(string schemeFolder, string schemeName)
        {
            string configPath = Path.Combine(schemeFolder, SchemeConfigFileName);

            if (!await Task.Run(() => File.Exists(configPath)))
            {
                try
                {
                    await Task.Run(() =>
                    {
                        // 修正：传入方案路径，确保配置保存在对应文件夹
                        ProjectConfigHelper.Instance.SaveConfig();
                        MyLogger.Warn($"方案[{schemeName}]缺少配置文件，已生成默认配置：{configPath}");
                    });
                }
                catch (Exception ex)
                {
                    MyLogger.Error($"方案[{schemeName}]生成默认配置失败", ex);
                }
            }
        }

        /// <summary>
        /// 主线程更新方案集合（UI绑定安全）
        /// </summary>
        private async Task UpdateSchemesCollectionAsync(List<(string SchemeName, string SchemePath)> validSchemes)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // 清空原有数据
                Schemes.Clear();
                _internalSchemeFolders.Clear();

                // 添加有效方案
                foreach (var (schemeName, schemePath) in validSchemes)
                {
                    Schemes.Add(schemeName);
                    _internalSchemeFolders[schemeName] = schemePath;
                }

                // 处理无方案场景
                if (Schemes.Count == 0)
                {
                    Schemes.Add(NoSchemePlaceholder);
                    CurrentScheme = $"当前方案：{NoSchemePlaceholder}";
                    MyLogger.Warn("未检测到任何有效方案（需包含*.sol核心文件）");
                }
            });
        }
        #endregion

        #region 命令定义（用特性简化，增强CanExecute判断）
        /// <summary>
        /// 打开方案配置窗口（仅工程师权限可执行）
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanOpenSchemeConfig))]
        private void OpenSchemeConfig()
        {
            try
            {
                var mainWindow = Application.Current.MainWindow;
                if (mainWindow == null)
                {
                    MyLogger.Warn("打开方案配置窗口失败：主窗口不存在");
                    return;
                }

                var schemeConfigWindow = new SchemeConfigWindow
                {
                    Owner = mainWindow,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                schemeConfigWindow.ShowDialog();

            }
            catch (Exception ex)
            {
                MyLogger.Error("打开方案配置窗口失败", ex);
            }
        }

        /// <summary>
        /// 能否打开方案配置窗口（工程师权限）
        /// </summary>
        private bool CanOpenSchemeConfig() => IsEngineerPermission;

        /// <summary>
        /// 切换用户权限
        /// </summary>
        [RelayCommand]
        private void TogglePermission()
        {
            try
            {
                if (IsEngineerPermission)
                {
                    // 切换为员工权限
                    CurrentPermission = $"当前权限：{PermissionStaff}";
                }
                else
                {
                    // 验证密码切换为工程师权限
                    var mainWindow = Application.Current.MainWindow;
                    if (mainWindow == null)
                    {
                        return;
                    }

                    var permissionWindow = new PermissionWindow
                    {
                        Owner = mainWindow,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };

                    if (permissionWindow.ShowDialog() == true)
                    {
                        CurrentPermission = $"当前权限：{PermissionEngineer}";
                    }
                }

                // 更新编辑权限状态
                UpdateEditSchemePermission();
            }
            catch (Exception ex)
            {
                MyLogger.Error("切换用户权限失败", ex);
            }
        }

        /// <summary>
        /// 切换方案命令（步骤：校验→释放旧资源→加载新配置→初始化服务→更新UI）
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanSwitchScheme))]
        private async Task SwitchSchemeAsync(string schemeName)
        {

            try
            {
                // 1. 校验方案有效性
                if (!ValidateScheme(schemeName, out string errorMsg))
                {
                    MyLogger.Warn($"方案切换失败：{errorMsg}");
                    return;
                }

                // 2. 后台线程：释放旧资源+加载配置
                var (schemeFolder, solFile) = await PrepareSchemeSwitchAsync(schemeName);
                // 显示加载遮罩（主线程）
                IsAnimation = Visibility.Visible;

                visionServiceManager = new VisionServiceManager();
                 await visionServiceManager.InitializeAsync();
                PlcStatus = visionServiceManager.PlcStatus;
                DbStatus = visionServiceManager.DatabaseStatus;
                NasStatus = visionServiceManager.NasStatus;
                visionServiceManager.StartAsync();

                // 3. 主线程：初始化UI控件+加载方案
                await InitializeNewSchemeAsync(schemeName, solFile);



                MyLogger.Info($"方案切换成功：[{schemeName}]，路径：{schemeFolder}");
            }
            catch (Exception ex)
            {
                CurrentScheme = $"当前方案：{NoSchemePlaceholder}";
                MyLogger.Error($"方案[{schemeName}]加载失败", ex);
            }
            finally
            {
                // 隐藏加载遮罩（主线程）
                IsAnimation = Visibility.Hidden;
            }
        }

        /// <summary>
        /// 能否切换方案（方案有效+非当前方案）
        /// </summary>
        private bool CanSwitchScheme(string schemeName)
        {
            return !string.IsNullOrWhiteSpace(schemeName)
                   && schemeName != NoSchemePlaceholder
                   && CurrentScheme != $"当前方案：{schemeName}";
        }

        /// <summary>
        /// 窗口关闭命令（Closing 事件触发，支持取消关闭）
        /// </summary>
        /// <param name="e">关闭事件参数（用于取消关闭流程）</param>
        [RelayCommand]
        private void WindowClosing(CancelEventArgs e)
        {
            try
            {
                var confirmResult = MessageBox.Show(
                    "是否确定退出程序？",
                    "系统提示",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question,
                    MessageBoxResult.No);

                // 用户取消退出：阻止窗口关闭
                if (confirmResult != MessageBoxResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }

                if (visionServiceManager != null)
                {
                    visionServiceManager.Stop();
                }
                // 用户确认退出：释放资源
                _runTimeTimer.Stop();
                _runTimeTimer.Tick -= RunTimeTimer_Tick;

                FrontendControl?.Dispose();
                FrontendControl = null;

                VmSolution.Instance.CloseSolution();

                GC.Collect();
                GC.WaitForPendingFinalizers();
                MyLogger.Info("程序正常关闭，所有资源已释放");
                Process.GetCurrentProcess().Kill();
            }
            catch (Exception ex)
            {
                e.Cancel = true;
                MyLogger.Error("退出失败", ex);
                MessageBox.Show($"退出错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region 方案切换辅助方法（拆分逻辑，便于维护）
        /// <summary>
        /// 校验方案有效性
        /// </summary>
        private bool ValidateScheme(string schemeName, out string errorMsg)
        {
            if (string.IsNullOrWhiteSpace(schemeName) || schemeName == NoSchemePlaceholder)
            {
                errorMsg = "请先创建或导入有效方案";
                return false;
            }

            if (!_internalSchemeFolders.ContainsKey(schemeName))
            {
                errorMsg = $"方案[{schemeName}]路径不存在";
                return false;
            }

            errorMsg = string.Empty;
            return true;
        }

        /// <summary>
        /// 后台线程：准备方案切换（释放旧资源+加载配置）
        /// </summary>
        private async Task<(string SchemeFolder, string SolFile)> PrepareSchemeSwitchAsync(string schemeName)
        {
            return await Task.Run(() =>
            {
                // 获取方案文件夹路径
                string schemeFolder = _internalSchemeFolders[schemeName];

                // 加载方案配置
                ProjectConfigHelper.Instance.LoadConfig(schemeFolder);

                // 查找.sol核心文件
                string solFile = Directory.GetFiles(schemeFolder, SchemeCoreFileSuffix, SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (solFile == null)
                {
                    throw new FileNotFoundException($"方案核心文件（{SchemeCoreFileSuffix}）不存在", schemeFolder);
                }

                // 释放旧方案资源
                ReleaseOldSchemeResources();

                return (schemeFolder, solFile);
            });
        }

        /// <summary>
        /// 释放旧方案资源
        /// </summary>
        private void ReleaseOldSchemeResources()
        {
            try
            {
                // 关闭旧方案
                VmSolution.Instance.CloseSolution();

                FrontendControl = null;

                //MyLogger.Info("旧方案资源已释放");
            }
            catch (Exception ex)
            {
                MyLogger.Warn($"旧方案资源释放警告：{ex.Message}");
            }
        }

        /// <summary>
        /// 主线程：初始化新方案（UI控件+方案加载）
        /// </summary>
        private async Task InitializeNewSchemeAsync(string schemeName, string solFile)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // 创建前端控件（必须UI线程）
                FrontendControl = new VmFrontendControl();

                // 加载.sol方案
                VmSolution.Load(solFile);

                // 更新UI状态
                CurrentScheme = $"当前方案：{schemeName}";
            });
        }
        #endregion

        #region 辅助方法（权限、UI提示、时长格式化）
        /// <summary>
        /// 根据当前权限更新编辑方案权限
        /// </summary>
        private void UpdateEditSchemePermission()
        {
            CanEditScheme = IsEngineerPermission;
        }

        /// <summary>
        /// 定时器事件：每秒更新程序总运行时长（优化格式化）
        /// </summary>
        private void RunTimeTimer_Tick(object? sender, EventArgs e)
        {
            TimeSpan runDuration = DateTime.Now - _startTime;

            // 优化格式化：根据时长动态显示（天→时→分→秒）
            TotalRunTime = runDuration.Days > 0
                ? $"{runDuration.Days:D2}天 {runDuration.Hours:D2}:{runDuration.Minutes:D2}:{runDuration.Seconds:D2}"
                : $"{runDuration.Hours:D2}:{runDuration.Minutes:D2}:{runDuration.Seconds:D2}";
        }
        #endregion

        #region 资源释放（析构函数，防止遗漏）
        ~MainViewModel()
        {
            // 停止定时器（非托管资源释放）
            _runTimeTimer?.Stop();
        }
        #endregion
    }
}