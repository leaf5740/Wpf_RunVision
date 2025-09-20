using System;
using System.Linq;

namespace Wpf_RunVision.Services.Plc
{
    public class InovancePlcService : IPlcService
    {
        public string Brand => "汇川";

        public PlcProtocol[] SupportedProtocols => new[] { PlcProtocol.ModbusTCP, PlcProtocol.ModbusRTU };

        public bool Connect(string ip, int port, PlcProtocol protocol)
        {
            if (!SupportedProtocols.Contains(protocol))
                throw new InvalidOperationException("汇川 PLC 只支持 ModbusTCP/RTU 协议");

            Console.WriteLine($"Connecting Inovance PLC at {ip}:{port} with {protocol}");
            return true;
        }

        public void Disconnect() { Console.WriteLine("Disconnect Inovance PLC"); }
    }
}
