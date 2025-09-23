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
        private string _plcCompleteAddress;
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
        /// PLC完成信号地址（格式示例：DB1.0）
        /// </summary>
        public string PlcCompleteAddress
        {
            get => _plcCompleteAddress;
            set => SetProperty(ref _plcCompleteAddress, value);
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

