namespace Wpf_RunVision.Services.Plc
{
    public interface IPlcService
    {
        string Brand { get; }
        PlcProtocol[] SupportedProtocols { get; }  // 支持的协议
        bool Connect(string ip, int port, PlcProtocol protocol);
        void Disconnect();
    }
}
