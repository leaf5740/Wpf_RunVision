using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Wpf_RunVision.Services.Plc
{
    public abstract class ModbusTcpBase : IPlcService
    {
        protected TcpClient _client;
        protected NetworkStream _stream;

        public virtual bool Connect(string ip, int port)
        {
            try
            {
                _client = new TcpClient();
                _client.Connect(ip, port);
                _stream = _client.GetStream();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PLC连接失败: {ex.Message}");
                return false;
            }
        }

        public virtual void Disconnect()
        {
            _stream?.Close();   
            _client?.Close();
        }

        public abstract bool Write(int address, int value);
        public abstract int Read(int address);
    }
}
