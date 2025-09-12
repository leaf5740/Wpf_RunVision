using NLog;
using System;
using System.Threading;
using System.Windows;
using YourApp.Tools;

namespace Wpf_RunVision
{
    /// <summary>
    /// 应用程序入口类
    /// </summary>
    public partial class App : Application
    {
        // 定义一个 Mutex 用于保证应用程序单实例运行
        private static Mutex _mutex;

        /// <summary>
        /// 程序主入口（替代默认的 App.xaml StartupUri）
        /// </summary>
        [STAThread] // 指定为单线程单元模型（WPF 必须）
        public static void Main()
        {
            // Mutex 的唯一标识符，用于判断程序是否已有实例运行
            const string appName = "Wpf_RunVision_OnlyOne";
            bool createdNew;

            // 尝试创建一个全局唯一的 Mutex
            _mutex = new Mutex(true, appName, out createdNew);

            // 如果 Mutex 已经存在，说明已有程序实例运行
            if (!createdNew)
            {
                // 提示用户程序已经在运行，然后退出当前实例
                MessageBox.Show("程序已在运行！", "系统提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return; // 退出 Main
            }

            // 当前是唯一实例，正常启动 WPF 应用
            var app = new App();
            app.InitializeComponent();
            app.Run();
        }

        /// <summary>
        /// 应用程序启动事件
        /// </summary>
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // 注册全局异常捕获，保证未处理异常被记录日志
            MyLogger.Instance.RegisterGlobalExceptionHandlers();

            // 创建并显示主窗口
            var mainWindow = new Views.MainWindow();
            mainWindow.Show();
        }
    }
}
