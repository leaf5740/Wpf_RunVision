using System.Windows;
using YourApp.Tools;

namespace Wpf_RunVision.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // 初始化（在 UI 线程）
            MyLogger.Instance.Initialize(LogRichTextBox, "logs", 500, 300);
            MyLogger.Info("程序启动！");
        }
    }
}
