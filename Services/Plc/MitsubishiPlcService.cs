using System;

namespace Wpf_RunVision.Services.Plc
{
    public class MitsubishiPlcService : ModbusTcpBase
    {
        public override bool Write(int address, int value)
        {
            // TODO: 三菱报文格式
            Console.WriteLine($"[三菱PLC] 写入 地址={address}, 值={value}");
            return true;
        }

        public override int Read(int address)
        {
            // TODO: 三菱报文格式
            Console.WriteLine($"[三菱PLC] 读取 地址={address}");
            return 5678;
        }
    }
}