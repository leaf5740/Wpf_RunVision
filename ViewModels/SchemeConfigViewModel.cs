using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Wpf_RunVision.Models;
using Wpf_RunVision.Utils;

namespace Wpf_RunVision.ViewModels
{

    public class SchemeConfigViewModel : ObservableObject
    {
        public SchemeConfigViewModel()
        {

        }

        /// <summary>
        /// 窗口关闭命令（可选操作）
        /// </summary>
        public ICommand WindowClosedCommand { get; } = new RelayCommand(() =>
        {

        });

    }
}
