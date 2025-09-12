using HandyControl.Controls;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Wpf_RunVision.Tools;
using Wpf_RunVision.ViewModels;

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
            var vm = this.DataContext as PermissionWindowViewModel;
            if (vm == null) return;

            // 延迟同步，确保拿到最新输入的密码
            Dispatcher.BeginInvoke(new Action(() =>
            {
                vm.Password = HCPasswordBox.Password;
            }), DispatcherPriority.Background);
        }

    }

}
