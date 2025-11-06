using System;
using System.Linq;
using System.Threading;
using System.Windows;
using Wpf_RunVision.Views;

namespace Wpf_RunVision
{
    public partial class App : Application
    {
        // 用于保证单实例运行
        private static Mutex _mutex;

        /// <summary>
        /// 应用程序启动事件
        /// </summary>
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            const string mutexName = "Wpf_RunVision_OnlyOne"; // Mutex 唯一名称
            bool isNewInstance;

            // 尝试创建全局 Mutex，标识当前实例
            _mutex = new Mutex(true, mutexName, out isNewInstance);

            // 如果不是新实例，则已有程序在运行
            if (!isNewInstance)
            {
                // 弹窗提示用户
                MessageBox.Show("软件已经在运行中！请勿重复开启", "提示", MessageBoxButton.OK, MessageBoxImage.Information);

                // 激活已存在实例的主窗口
                ActivateExistingInstance();

                // 关闭当前启动的实例
                Shutdown();
                return;
            }

            // 注册全局异常捕获，保证未处理异常被记录日志
            //MyLogger.Instance.RegisterGlobalExceptionHandlers();

            // 创建并显示主窗口
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }

        /// <summary>
        /// 激活已经运行的实例（恢复窗口、置顶）
        /// </summary>
        private void ActivateExistingInstance()
        {
            var currentProcess = System.Diagnostics.Process.GetCurrentProcess();

            // 找到除自己以外的同名进程（已经运行的实例）
            var otherProcess = System.Diagnostics.Process.GetProcessesByName(currentProcess.ProcessName)
                                     .FirstOrDefault(p => p.Id != currentProcess.Id && p.MainWindowHandle != IntPtr.Zero);

            if (otherProcess != null)
            {
                IntPtr handle = otherProcess.MainWindowHandle;

                // 如果窗口最小化，恢复显示
                Win32.ShowWindow(handle, Win32.SW_RESTORE);

                // 将窗口置于前台
                Win32.SetForegroundWindow(handle);
            }
        }

        /// <summary>
        /// 程序退出时，释放 Mutex
        /// </summary>
        protected override void OnExit(ExitEventArgs e)
        {
            _mutex?.ReleaseMutex(); // 释放锁
            _mutex = null;
            base.OnExit(e);
        }
    }

    /// <summary>
    /// 调用 Win32 API 操作窗口
    /// </summary>
    internal static class Win32
    {
        public const int SW_RESTORE = 9; // 恢复最小化窗口

        // 设置窗口为前台
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        // 显示或恢复窗口
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }
}