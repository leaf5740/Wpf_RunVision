// Wpf_RunVision.Models/CameraModel.cs
using CommunityToolkit.Mvvm.ComponentModel;

namespace Wpf_RunVision.Models{
    /// <summary>
    /// 相机配置模型
    /// </summary>
    public class CameraModels : ObservableObject
    {
        private string _brand;
        private string _sn;
        private string _remark;

        /// <summary>
        /// 相机品牌
        /// </summary>
        public string Brand
        {
            get => _brand;
            set => SetProperty(ref _brand, value);
        }

        /// <summary>
        /// 相机序列号（唯一标识）
        /// </summary>
        public string Sn
        {
            get => _sn;
            set => SetProperty(ref _sn, value);
        }

        /// <summary>
        /// 相机备注信息
        /// </summary>
        public string Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
        }
    }
}

