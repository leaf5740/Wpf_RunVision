using System;
using System.Threading.Tasks;

namespace Wpf_RunVision.Services.Plc
{
    public class MitsubishiModbusTcp : ModbusTcpBase
    {
        private byte _slaveId = 1;

        #region 异步读写实现（核心适配）
        /// <summary>
        /// 异步读取三菱PLC（M区线圈/D区保持寄存器）
        /// </summary>
        public override async Task<int> ReadAsync(string address)
        {
            // Task.Run包装同步Modbus操作，避免阻塞调用线程
            return await Task.Run(() =>
            {
                ushort addr;
                var type = PlcAddressHelper.ParseType(address, out addr);

                switch (type)
                {
                    case PlcAddressType.M:
                        // M区线圈：true→1，false→0（校验返回数组长度，避免越界）
                        var coils = _master.ReadCoils(_slaveId, addr, 1);
                        return coils.Length > 0 && coils[0] ? 1 : 0;
                    case PlcAddressType.D:
                        // D区保持寄存器：返回16位整数
                        var regs = _master.ReadHoldingRegisters(_slaveId, addr, 1);
                        return regs.Length > 0 ? regs[0] : 0;
                    default:
                        throw new NotSupportedException($"三菱PLC不支持的地址类型: {type}，仅支持 M/D 区");
                }
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步写入三菱PLC（M区线圈/D区保持寄存器）
        /// </summary>
        public override async Task<bool> WriteAsync(string address, string value)
        {
            return await Task.Run(() =>
            {
                ushort addr;
                var type = PlcAddressHelper.ParseType(address, out addr);

                switch (type)
                {
                    case PlcAddressType.M:
                        // M区线圈："1"/"true"→通，其他→断（忽略大小写）
                        bool coilVal = value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase);
                        _master.WriteSingleCoil(_slaveId, addr, coilVal);
                        break;
                    case PlcAddressType.D:
                        // D区寄存器：校验16位整数（0-65535）
                        if (!ushort.TryParse(value, out ushort regVal))
                        {
                            throw new ArgumentException($"D区写入值无效：{value}，需传入0-65535的整数");
                        }
                        _master.WriteSingleRegister(_slaveId, addr, regVal);
                        break;
                    default:
                        throw new NotSupportedException($"三菱PLC不支持的地址类型: {type}，仅支持 M/D 区");
                }
                return true;
            }).ConfigureAwait(false);
        }
        #endregion

        #region 同步读写兼容（继承基类，无需重复实现）
        // 直接继承基类的 [Obsolete] 标记同步方法，无需额外代码
        #endregion
    }
}