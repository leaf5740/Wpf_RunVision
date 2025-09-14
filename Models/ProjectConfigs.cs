using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Wpf_RunVision.Models
{
    public class ProjectConfigs : ObservableObject
    {
        // 相机模型列表
        private ObservableCollection<CameraModel> _dataList;
        public ObservableCollection<CameraModel> DataList
        {
            get
            {
                if (_dataList == null)
                    _dataList = new ObservableCollection<CameraModel>();
                return _dataList;
            }
            set => SetProperty(ref _dataList, value);
        }

    }
}
