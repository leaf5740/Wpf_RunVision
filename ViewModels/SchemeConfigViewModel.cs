using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Wpf_RunVision.Models;
using Wpf_RunVision.Tools;


namespace Wpf_RunVision.ViewModels
{
    public class SchemeConfigViewModel : ObservableObject
    {

        // 类型集合
        private ObservableCollection<string> _demoTypes;
        public ObservableCollection<string> DemoTypes
        {
            get => _demoTypes;
            set => SetProperty(ref _demoTypes, value);
        }

        private ObservableCollection<CameraModel> _dataList;
        public ObservableCollection<CameraModel> DataList
        {
            get => _dataList;
            set => SetProperty(ref _dataList, value);
        }

        public SchemeConfigViewModel()
        {
            DemoTypes = new ObservableCollection<string>();
            DataList = ProjectConfigHelper.Instance.CurrentConfigs.DataList;
            // 示例相机序列号
            DemoTypes.Add("SN001");
            DemoTypes.Add("SN002");
            DemoTypes.Add("SN003");

            //示例数据
            // 绑定 DataList 到配置对象

            //if (config.DataList == null)
            //{
            //    config.DataList = new ObservableCollection<CameraModel>();
            //}

            //DataList.Add(new CameraModel { Name = "Camera1", Type = "null", Remark = "备注1" });
            //DataList.Add(new CameraModel { Name = "Camera2", Type = "null", Remark = "备注2" });

        }

        /// <summary>
        /// 监听关闭窗口事件
        /// </summary>
        public ICommand WindowClosedCommand => new RelayCommand(() =>
        {
            ProjectConfigHelper.Instance.SaveConfig();
        });


    }
}
