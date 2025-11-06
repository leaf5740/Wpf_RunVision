using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyControl.Controls;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Wpf_RunVision.Utils;

namespace Wpf_RunVision.ViewModels
{
    public class PermissionViewModel : ObservableObject
    {
        private string _password;
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public PermissionViewModel()
        {

        }

        /// <summary>
        /// 登录事件
        /// </summary>
        public ICommand ConfirmCommand => new RelayCommand<System.Windows.Window>(Confirm =>
        {
            if (string.IsNullOrWhiteSpace(Password))
            {
                Growl.ErrorGlobal("密码不能为空");
                return;
            }

            if (Password == "123")
            {
                if (Confirm != null)
                    Confirm.DialogResult = true;
                Growl.ClearGlobal();
            }
            else
            {
                Growl.ErrorGlobal("密码错误，请重新输入!");
            }
        });

        /// <summary>
        /// 监听关闭窗口事件
        /// </summary>
        public ICommand WindowClosedCommand => new RelayCommand(() =>
        {
            Growl.ClearGlobal();
        });

    }

}