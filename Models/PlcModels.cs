using CommunityToolkit.Mvvm.ComponentModel;

namespace Wpf_RunVision.Models
{
    public class PlcModels : ObservableObject
    {
        private string _brand;
        private string _protocol;
        private string _ip;
        private string _port;

        public string Protocol
        {
            get => _protocol;
            set => SetProperty(ref _protocol, value);
        }

        public string Brand
        {
            get => _brand;
            set => SetProperty(ref _brand, value);
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
