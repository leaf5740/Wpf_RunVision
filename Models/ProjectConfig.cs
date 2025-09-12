using CommunityToolkit.Mvvm.ComponentModel;

namespace Wpf_RunVision.Models
{
    /// <summary>
    /// 方案配置模型
    /// </summary>
    public class ProjectConfig : ObservableObject
    {
        private string _name;
        private double _exposure;
        private string _savePath;

        /// <summary>
        /// 方案名称
        /// </summary>
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>
        /// 曝光时间
        /// </summary>
        public double Exposure
        {
            get => _exposure;
            set => SetProperty(ref _exposure, value);
        }

        /// <summary>
        /// 保存路径
        /// </summary>
        public string SavePath
        {
            get => _savePath;
            set => SetProperty(ref _savePath, value);
        }

    }
}
