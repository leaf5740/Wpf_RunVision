using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace Wpf_RunVision.Models
{
    public class PlcModel
    {
        public string? Brand { set; get;}
        public string? Protocol { set; get; }
        public string? Ip { set; get; }
        public string? Port { set; get; }

        public List<PLCAddressModels> ReadPLCAddress { get; set; } = new List<PLCAddressModels>();

        public List<PLCAddressModels> WritePLCAddress { get; set; } = new List<PLCAddressModels>();
    }

    public class PLCAddressModels
    {
        public string? Name { get; set; }
        public string? Address { get; set; }
        public string? Value { get; set; }
        public string? Remark { get; set; }

    }

}
