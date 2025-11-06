using System;
using System.Windows.Threading;

namespace Wpf_RunVision.Views
{
    /// <summary>
    /// PermissionWindow.xaml 的交互逻辑
    /// </summary>
    public partial class PermissionWindow : System.Windows.Window
    {
        public PermissionWindow()
        {
            InitializeComponent();
        }

        private void HCPasswordBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var vm = this.DataContext as ViewModels.PermissionViewModel;
            if (vm == null) return;
            // 延迟同步，确保拿到最新输入的密码
            Dispatcher.BeginInvoke(new Action(() =>
            {
                vm.Password = HCPasswordBox.Password;
            }), DispatcherPriority.Background);
        }

    }

}
