using System;

namespace Wpf_RunVision.Services.Plc
{
    public class InovancePlcService : ModbusTcpBase
    {
        public override bool Write(int address, int value)
        {
            // TODO: 汇川报文格式
            Console.WriteLine($"[汇川PLC] 写入 地址={address}, 值={value}");
            return true;
        }

        public override int Read(int address)
        {
            // TODO: 汇川报文格式
            Console.WriteLine($"[汇川PLC] 读取 地址={address}");
            return 1234;
        }
    }
}