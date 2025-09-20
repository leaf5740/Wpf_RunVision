using CommunityToolkit.Mvvm.ComponentModel;

namespace Wpf_RunVision.Models
{
    /// <summary>
    /// 单个相机配置
    /// </summary>
    public class CameraModels : ObservableObject
    {
        // 私有字段
        private string _brand;
        private string _sn;
        private string _remark;

        // 属性
        public string Brand
        {
            get => _brand;
            set => SetProperty(ref _brand, value);
        }

        public string Sn
        {
            get => _sn;
            set => SetProperty(ref _sn, value);
        }

        public string Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
        }
    }
}
