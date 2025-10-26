using System;

namespace Wpf_RunVision.Services.Plc
{
    public class InovanceModbusTcp : ModbusTcpBase
    {
        private byte _slaveId = 1;

        public override int Read(string address)
        {
            ushort addr;
            var type = PlcAddressHelper.ParseType(address, out addr);

            switch (type)
            {
                case PlcAddressType.M:
                    return _master.ReadCoils(_slaveId, addr, 1)[0] ? 1 : 0;
                case PlcAddressType.D:
                    return _master.ReadHoldingRegisters(_slaveId, addr, 1)[0];
                default:
                    throw new NotSupportedException($"不支持的地址类型: {type}");
            }
        }

        public override bool Write(string address, string value)
        {
            ushort addr;
            var type = PlcAddressHelper.ParseType(address, out addr);

            switch (type)
            {
                case PlcAddressType.M:
                    bool coilValue = value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase);
                    _master.WriteSingleCoil(_slaveId, addr, coilValue);
                    break;
                case PlcAddressType.D:
                    ushort regValue = Convert.ToUInt16(value);
                    _master.WriteSingleRegister(_slaveId, addr, regValue);
                    break;
                default:
                    throw new NotSupportedException($"不支持的地址类型: {type}");
            }
            return true;
        }
    }
}
