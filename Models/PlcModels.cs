using CommunityToolkit.Mvvm.ComponentModel;
using Wpf_RunVision.Services.Plc;

namespace Wpf_RunVision.Models
{
    /// <summary>
    /// PLC 配置模型，用于界面绑定
    /// </summary>
    public class PlcModels : ObservableObject
    {
        // 私有字段
        private string _brand;
        private PlcProtocol _protocol;
        private string _ip;
        private string _port;

        // 属性
        public string Brand
        {
            get => _brand;
            set => SetProperty(ref _brand, value);
        }

        public PlcProtocol Protocol
        {
            get => _protocol;
            set => SetProperty(ref _protocol, value);
        }

        public string Ip
        {
            get => _ip;
            set => SetProperty(ref _ip, value);
        }

        public string Port
        {
            get => _port;
            set => SetProperty(ref _port, value);
        }
    }
}
