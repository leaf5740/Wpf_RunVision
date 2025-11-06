using Modbus.Device;
using System;
using System.Net.Sockets;
using Wpf_RunVision.Utils;

namespace Wpf_RunVision.Services.Plc
{
    public abstract class ModbusTcpBase : IPlcService
    {
        protected TcpClient _client;
        protected IModbusMaster _master;
        public bool IsConnected => _client != null && _client.Connected;

        public virtual bool Connect(string ip, int port = 502)
        {
            try
            {
                _client = new TcpClient(ip, port);
                _master = ModbusIpMaster.CreateIp(_client); // NModbus4 TCP Master
                MyLogger.Info($"PLC连接成功: {ip}:{port}");
                return true;
            }
            catch (Exception ex)
            {
                MyLogger.Error($"PLC连接失败: {ex.Message}");
                return false;
            }
        }

        public virtual void Disconnect()
        {
            try
            {
                _client?.Close();
                _client = null;
                _master = null;
                MyLogger.Warn("PLC已断开连接");
            }
            catch (Exception ex)
            {
                MyLogger.Error($"断开PLC异常: {ex.Message}");
            }
        }

        public abstract int Read(string address);
        public abstract bool Write(string address, string value);
    }
}
