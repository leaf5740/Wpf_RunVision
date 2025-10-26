namespace Wpf_RunVision.Services.Plc
{
    public interface IPlcService
    {
        bool Connect(string ip, int port = 502);
        void Disconnect();
        bool Write(string address, string value);
        int Read(string address);
        bool IsConnected { get; }
    }
}
