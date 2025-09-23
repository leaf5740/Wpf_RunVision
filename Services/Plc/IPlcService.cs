using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wpf_RunVision.Services.Plc
{
    public interface IPlcService
    {
        bool Connect(string ip, int port);
        void Disconnect();
        bool Write(int address, int value);
        int Read(int address);
    }
}
