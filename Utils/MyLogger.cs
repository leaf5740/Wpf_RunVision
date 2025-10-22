using Microsoft.Win32;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace Wpf_RunVision.Utils
{
    /// <summary>
    /// WPF 日志管理器（Singleton 单例模式）
    /// 功能：
    /// 1. 在 RichTextBox 中显示实时日志
    /// 2. 写入 NLog 文件日志
    /// 3. 支持日志等级过滤（Info, Debug, Warn, Error）
    /// 4. 支持右键菜单：清空、导出、复制、日志等级显示过滤
    /// 5. 支持全局异常捕获
    /// </summary>
    public sealed class MyLogger
    {
        // 单例实例
        private static readonly Lazy<MyLogger> _instance = new Lazy<MyLogger>(() => new MyLogger());
        public static MyLogger Instance => _instance.Value;

        private MyLogger() { }

        // NLog 日志对象
        private static readonly Logger _nlogger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 日志等级
        /// </summary>
        public enum Level { Info, Debug, Warn, Error }

        /// <summary>
        /// 内存中存储的日志条目
        /// </summary>
        private class LogEntry
        {
            public long Seq { get; set; }        // 序号
            public DateTime Time { get; set; }   // 时间
            public Level Lv { get; set; }        // 日志等级
            public string Message { get; set; }  // 日志内容

            // 转换成文本行
            public string ToText()
            {
                return $"[{Seq}] [{Time:yyyy-MM-dd HH:mm:ss}] [{Lv}] {Message}";
            }
        }

        // RichTextBox 用于 UI 显示
        private RichTextBox _rtb;
        private Dispatcher _dispatcher; // UI 线程调度器

        // 内存日志集合和同步锁
        private readonly List<LogEntry> _allLogs = new List<LogEntry>();
        private readonly object _sync = new object();

        // 序号
        private long _seq = 0;

        // 日志数量限制
        private int _maxLogCount = 500; // 最大日志条数
        private int _trimCount = 200;   // 超过最大条数时清理的条数

        // 日志等级显示开关
        private bool _showInfo = true;
        private bool _showDebug = true;
        private bool _showWarn = true;
        private bool _showError = true;

        private bool _initialized = false;

        #region 初始化

        /// <summary>
        /// 初始化 UILogger，绑定 RichTextBox 并创建日志文件夹
        /// </summary>
        /// <param name="rtb">用于显示日志的 RichTextBox</param>
        /// <param name="str">日志文件夹名称，默认 logs</param>
        /// <param name="maxLogCount">最大日志条数</param>
        /// <param name="trimCount">超过最大条数时清理的条数</param>
        public void Initialize(RichTextBox rtb, string str = "logs", int maxLogCount = 500, int trimCount = 200)
        {
            if (rtb == null) throw new ArgumentNullException(nameof(rtb));
            if (_initialized && _rtb == rtb) return;

            _rtb = rtb;
            _dispatcher = _rtb.Dispatcher;
            _maxLogCount = maxLogCount;
            _trimCount = Math.Min(trimCount, maxLogCount);

            // 初始化 RichTextBox 样式
            _rtb.Document = new FlowDocument
            {
                PagePadding = new Thickness(0),
                LineHeight = 1,
                TextAlignment = TextAlignment.Left
            };
            _rtb.IsReadOnly = true;
            _rtb.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

            // 自动创建日志文件夹
            string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, str);
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            // 添加右键菜单
            AttachContextMenu();

            _initialized = true;
        }

        #endregion

        #region 全局异常捕获

        /// <summary>
        /// 注册全局异常捕获事件
        /// </summary>
        /// <param name="markHandled">是否标记 UI 异常已处理，避免程序崩溃</param>
        public void RegisterGlobalExceptionHandlers(bool markHandled = true)
        {
            // 捕获 UI 线程未处理异常
            if (Application.Current != null)
            {
                Application.Current.DispatcherUnhandledException += (s, e) =>
                {
                    string errorMsg = $"UI线程未处理异常：{e.Exception.Message}\n\n详细信息：{e.Exception.StackTrace}";
                    Error(errorMsg, e.Exception);

                    // 弹出错误对话框
                    System.Windows.MessageBox.Show(
                        errorMsg,
                        "程序错误",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );

                    e.Handled = markHandled;
                };
            }

            // 捕获非 UI 线程未处理异常
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                string errorMsg = $"非UI线程未处理异常：{ex?.Message}\n\n详细信息：{ex?.StackTrace}";
                Error(errorMsg, ex);

                // 非UI线程中需要通过Dispatcher切换到UI线程显示弹窗
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show(
                        errorMsg,
                        "程序错误",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                });
            };

            // 捕获 Task 未观察到的异常
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                string errorMsg = $"Task未观察到的异常：{e.Exception?.Message}\n\n详细信息：{e.Exception?.StackTrace}";
                Error(errorMsg, e.Exception);

                // Task异常可能在非UI线程，需切换到UI线程
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show(
                        errorMsg,
                        "程序错误",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                });

                e.SetObserved();
            };

        }

        #endregion

        #region 日志接口

        // 对外静态方法，方便调用
        public static void Info(string message) => Instance.LogInternal(Level.Info, message, null);
        public static void Debug(string message) => Instance.LogInternal(Level.Debug, message, null);
        public static void Warn(string message) => Instance.LogInternal(Level.Warn, message, null);
        public static void Error(string message) => Instance.LogInternal(Level.Error, message, null);
        public static void Error(string message, Exception ex) => Instance.LogInternal(Level.Error, message, ex);
        
        // 新增：带格式参数的日志方法
        public static void InfoFormat(string format, params object[] args) => Instance.LogInternal(Level.Info, string.Format(format, args), null);
        public static void DebugFormat(string format, params object[] args) => Instance.LogInternal(Level.Debug, string.Format(format, args), null);
        public static void WarnFormat(string format, params object[] args) => Instance.LogInternal(Level.Warn, string.Format(format, args), null);
        public static void ErrorFormat(string format, params object[] args) => Instance.LogInternal(Level.Error, string.Format(format, args), null);
        public static void ErrorFormat(Exception ex, string format, params object[] args) => Instance.LogInternal(Level.Error, string.Format(format, args), ex);
        
        // 新增：性能日志方法
        public static void Performance(string operation, TimeSpan elapsed) => 
            Instance.LogInternal(Level.Debug, $"[性能] {operation} 耗时: {elapsed.TotalMilliseconds:F2} ms", null);
        
        // 新增：操作日志方法
        public static void Operation(string user, string action, string target = null, bool success = true)
        {
            string message = success 
                ? $"[操作] 用户 {user} 执行 {action}" + (string.IsNullOrEmpty(target) ? "" : $" 于 {target}")
                : $"[操作] 用户 {user} 执行 {action}" + (string.IsNullOrEmpty(target) ? "" : $" 于 {target}") + " 失败";
            
            Instance.LogInternal(success ? Level.Info : Level.Warn, message, null);
        }

        /// <summary>
        /// 内部记录日志
        /// </summary>
        private void LogInternal(Level level, string message, Exception ex)
        {
            try
            {
                // 写入 NLog 文件日志
                switch (level)
                {
                    case Level.Info: _nlogger.Info(message); break;
                    case Level.Debug: _nlogger.Debug(message); break;
                    case Level.Warn: _nlogger.Warn(message); break;
                    case Level.Error:
                        if (ex != null) _nlogger.Error(ex, message);
                        else _nlogger.Error(message);
                        break;
                }

                // 写入内存 + RichTextBox
                AppendMemoryAndUI(level, message);
            }
            catch { }
        }

        /// <summary>
        /// 写入内存列表，并更新 RichTextBox
        /// </summary>
        private void AppendMemoryAndUI(Level level, string message)
        {
            LogEntry entry = null;
            bool needFullRefresh = false;

            lock (_sync)
            {
                entry = new LogEntry
                {
                    Seq = ++_seq,
                    Time = DateTime.Now,
                    Lv = level,
                    Message = message
                };
                _allLogs.Add(entry);

                // 超过最大条数，清理部分日志
                if (_allLogs.Count > _maxLogCount)
                {
                    int remove = Math.Min(_trimCount, _allLogs.Count);
                    _allLogs.RemoveRange(0, remove);
                    needFullRefresh = true;
                }
            }

            if (needFullRefresh)
            {
                SafeInvoke(RefreshDisplay);
                return;
            }

            if (ShouldDisplay(level))
                SafeInvoke(() => AddToRichTextBox(entry));
        }

        #endregion

        #region UI 操作

        /// <summary>
        /// 安全地在 UI 线程执行操作
        /// </summary>
        private void SafeInvoke(Action action)
        {
            if (_dispatcher == null || action == null) return;
            if (_dispatcher.CheckAccess()) action();
            else _dispatcher.Invoke(action);
        }

        /// <summary>
        /// 添加单条日志到 RichTextBox
        /// </summary>
        private void AddToRichTextBox(LogEntry entry)
        {
            if (_rtb == null || entry == null) return;
            Brush color = GetBrush(entry.Lv);
            var p = new Paragraph(new Run(entry.ToText())) { Foreground = color, Margin = new Thickness(0) };
            _rtb.Document.Blocks.Add(p);

            // 控制 RichTextBox 最大显示行数
            while (_rtb.Document.Blocks.Count > _maxLogCount)
                _rtb.Document.Blocks.Remove(_rtb.Document.Blocks.FirstBlock);

            _rtb.ScrollToEnd();
        }

        /// <summary>
        /// 根据日志等级获取显示颜色
        /// </summary>
        private Brush GetBrush(Level lv)
        {
            switch (lv)
            {
                case Level.Debug: return Brushes.Blue;
                case Level.Warn: return Brushes.Orange;
                case Level.Error: return Brushes.Red;
                default: return Brushes.Black;
            }
        }

        /// <summary>
        /// 判断该日志等级是否需要显示
        /// </summary>
        private bool ShouldDisplay(Level lv)
        {
            switch (lv)
            {
                case Level.Info: return _showInfo;
                case Level.Debug: return _showDebug;
                case Level.Warn: return _showWarn;
                case Level.Error: return _showError;
                default: return true;
            }
        }

        /// <summary>
        /// 刷新 RichTextBox 显示所有符合等级的日志
        /// </summary>
        private void RefreshDisplay()
        {
            if (_rtb == null) return;

            _rtb.Document.Blocks.Clear();
            List<LogEntry> snapshot;
            lock (_sync) snapshot = _allLogs.ToList();

            foreach (var entry in snapshot)
            {
                if (ShouldDisplay(entry.Lv))
                {
                    var p = new Paragraph(new Run(entry.ToText()))
                    {
                        Foreground = GetBrush(entry.Lv),
                        Margin = new Thickness(0)
                    };
                    _rtb.Document.Blocks.Add(p);
                }
            }
            _rtb.ScrollToEnd();
        }

        #endregion

        #region 导出 / 清空 / 复制

        /// <summary>
        /// 导出日志到指定文件
        /// </summary>
        public void ExportToFile(string filePath)
        {
            try
            {
                List<string> lines;
                lock (_sync)
                {
                    lines = _allLogs.Where(e => ShouldDisplay(e.Lv)).Select(e => e.ToText()).ToList();
                }
                File.WriteAllLines(filePath, lines, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _nlogger.Warn(ex, "ExportToFile 失败");
            }
        }

        /// <summary>
        /// 清空所有日志
        /// </summary>
        public void ClearAll()
        {
            lock (_sync) _allLogs.Clear();
            _seq = 0;
            SafeInvoke(() => _rtb?.Document.Blocks.Clear());
        }

        /// <summary>
        /// 复制选中内容或全部日志到剪贴板
        /// </summary>
        public void CopySelectedOrAll()
        {
            SafeInvoke(() =>
            {
                if (_rtb == null) return;
                string text = !_rtb.Selection.IsEmpty ? _rtb.Selection.Text :
                    string.Join(Environment.NewLine, _allLogs.Where(e => ShouldDisplay(e.Lv)).Select(e => e.ToText()));
                if (!string.IsNullOrEmpty(text)) Clipboard.SetText(text);
            });
        }

        #endregion

        #region 右键菜单

        /// <summary>
        /// 为 RichTextBox 添加右键菜单
        /// </summary>
        private void AttachContextMenu()
        {
            if (_rtb == null) return;

            var menu = new ContextMenu();

            // 清空日志
            var miClear = new MenuItem { Header = "清空日志" };
            miClear.Click += (s, e) => ClearAll();
            menu.Items.Add(miClear);

            // 导出日志
            var miExport = new MenuItem { Header = "导出日志" };
            miExport.Click += (s, e) => ShowExportDialog();
            menu.Items.Add(miExport);

            menu.Items.Add(new Separator());

            // 复制日志
            var miCopy = new MenuItem { Header = "复制选中/全部" };
            miCopy.Click += (s, e) => CopySelectedOrAll();
            menu.Items.Add(miCopy);

            menu.Items.Add(new Separator());

            // 日志等级过滤（合并到子菜单）
            var miLevel = new MenuItem { Header = "日志等级过滤" };

            // Info
            var miInfo = new MenuItem { Header = "显示 Info", IsCheckable = true, IsChecked = _showInfo };
            miInfo.Click += (s, e) => { _showInfo = miInfo.IsChecked; SafeInvoke(RefreshDisplay); };
            miLevel.Items.Add(miInfo);

            // Debug
            var miDebug = new MenuItem { Header = "显示 Debug", IsCheckable = true, IsChecked = _showDebug };
            miDebug.Click += (s, e) => { _showDebug = miDebug.IsChecked; SafeInvoke(RefreshDisplay); };
            miLevel.Items.Add(miDebug);

            // Warn
            var miWarn = new MenuItem { Header = "显示 Warn", IsCheckable = true, IsChecked = _showWarn };
            miWarn.Click += (s, e) => { _showWarn = miWarn.IsChecked; SafeInvoke(RefreshDisplay); };
            miLevel.Items.Add(miWarn);

            // Error
            var miError = new MenuItem { Header = "显示 Error", IsCheckable = true, IsChecked = _showError };
            miError.Click += (s, e) => { _showError = miError.IsChecked; SafeInvoke(RefreshDisplay); };
            miLevel.Items.Add(miError);

            // 添加子菜单到主菜单
            menu.Items.Add(miLevel);

            _rtb.ContextMenu = menu;

        }

        /// <summary>
        /// 弹出导出日志对话框
        /// </summary>
        private void ShowExportDialog()
        {
            SafeInvoke(() =>
            {
                var dlg = new SaveFileDialog
                {
                    FileName = $"Logs_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                    Filter = "文本文件 (*.txt)|*.txt"
                };
                if (dlg.ShowDialog() == true)
                {
                    ExportToFile(dlg.FileName);
                }
            });
        }

        #endregion
    }
}
