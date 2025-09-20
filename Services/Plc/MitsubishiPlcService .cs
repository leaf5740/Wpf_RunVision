using System;
using System.Linq;

namespace Wpf_RunVision.Services.Plc
{
    public class MitsubishiPlcService : IPlcService
    {
        public string Brand => "三菱";

        public PlcProtocol[] SupportedProtocols => new[] { PlcProtocol.Mitsubishi };

        public bool Connect(string ip, int port, PlcProtocol protocol)
        {
            if (!SupportedProtocols.Contains(protocol))
                throw new InvalidOperationException("三菱 PLC 只支持 Mitsubishi 协议");

            // TODO: 连接逻辑
            Console.WriteLine($"Connecting Mitsubishi PLC at {ip}:{port} with {protocol}");
            return true;
        }

        public void Disconnect() { Console.WriteLine("Disconnect Mitsubishi PLC"); }
    }
}
