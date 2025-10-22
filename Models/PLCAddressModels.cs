using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wpf_RunVision.Models
{
    public class PLCAddressModels:ObservableObject
    {
        private string _name;
        private string _address;
        private string _value;
        private string _remark;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Address 
        {
            get => _address;
            set => SetProperty(ref _address, value);
        }

        public string Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        public string Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
        }

    }
}
