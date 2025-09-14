using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Wpf_RunVision.Models
{
    public class ProjectConfigs : ObservableObject
    {
        // 相机模型列表
        private ObservableCollection<CameraModel> cameras;
        public ObservableCollection<CameraModel> Cameras
        {
            get
            {
                if (cameras == null)
                    cameras = new ObservableCollection<CameraModel>();
                return cameras;
            }
            set => SetProperty(ref cameras, value);
        }

    }
}
