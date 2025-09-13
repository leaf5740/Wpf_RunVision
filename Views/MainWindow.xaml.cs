using HandyControl.Controls;
using System.Linq;
using VMControls.WPF.Release;
using Wpf_RunVision.Tools;

namespace Wpf_RunVision.Views
{
    public partial class MainWindow : System.Windows.Window
    {

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new ViewModels.MainViewModel();
            MyLogger.Instance.Initialize(LogRichTextBox, "logs", 500, 300);
            MyLogger.Info("程序启动！");
          


        }
    }
}
