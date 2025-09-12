using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using Wpf_RunVision.Tools;

namespace Wpf_RunVision.ViewModels
{
    public class PermissionWindowViewModel : ObservableObject
    {
        private string _password;
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public PermissionWindowViewModel()
        {

        }

        /// <summary>
        /// 登录事件
        /// </summary>
        public ICommand ConfirmCommand => new RelayCommand<System.Windows.Window>(Confirm =>
        {
            if (string.IsNullOrWhiteSpace(Password))
            {
                HandyControl.Controls.Growl.ErrorGlobal("密码不能为空");
                return;
            }

            if (Password == "123")
            {
                if (Confirm != null)
                    Confirm.DialogResult = true;

                KeyboardHelper.CloseKeyboard();
            }
            else
            {
                HandyControl.Controls.Growl.ErrorGlobal("密码错误，请重新输入!");
            }
        });

        /// <summary>
        /// 屏幕键盘按钮事件
        /// </summary>
        public ICommand ShowKeyboardCommand => new RelayCommand(() =>
        {
            KeyboardHelper.ShowKeyboard();
        });

        /// <summary>
        /// 监听关闭窗口事件
        /// </summary>
        public ICommand WindowClosedCommand => new RelayCommand(() =>
        {
            HandyControl.Controls.Growl.ClearGlobal();
        });

    }

}