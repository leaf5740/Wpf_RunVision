using System;
using System.Threading.Tasks;
using Wpf_RunVision.Utils;

namespace Wpf_RunVision.Services.Plc
{
    /// <summary>
    /// 汇川PLC Modbus TCP 实现（同步连接/断开，异步读写）
    /// </summary>
    public class InovanceModbusTcp : ModbusTcpBase
    {
        /// <summary>
        /// 从站ID（汇川PLC默认1，可根据实际配置调整）
        /// </summary>
        private byte _slaveId = 1;

        #region 核心异步读写实现（适配接口）
        /// <summary>
        /// 异步读取汇川PLC数据（支持 M区线圈 / D区保持寄存器）
        /// </summary>
        /// <param name="address">PLC地址（如 "M0"、"D100"）</param>
        /// <returns>读取结果（M区：1=通/0=断；D区：寄存器数值）</returns>
        public override async Task<int> ReadAsync(string address)
        {
            // Task.Run 包装同步Modbus操作，避免阻塞业务线程
            return await Task.Run(() =>
            {
                ushort registerAddr;
                var addressType = PlcAddressHelper.ParseType(address, out registerAddr);

                switch (addressType)
                {
                    case PlcAddressType.M:
                        // 读取 M区线圈状态（汇川PLC M区为线圈类型）
                        var coilStates = _master.ReadCoils(_slaveId, registerAddr, 1);
                        // 校验返回结果，避免空数组索引越界
                        return coilStates.Length > 0 && coilStates[0] ? 1 : 0;
                    case PlcAddressType.D:
                        // 读取 D区保持寄存器（汇川PLC D区为16位无符号寄存器）
                        var registerValues = _master.ReadHoldingRegisters(_slaveId, registerAddr, 1);
                        return registerValues.Length > 0 ? registerValues[0] : 0;
                    default:
                        throw new NotSupportedException($"汇川PLC不支持的地址类型: {addressType}，仅支持 M区（线圈）和 D区（保持寄存器）");
                }
            }).ConfigureAwait(false); // 非UI场景，无需捕获上下文
        }

        /// <summary>
        /// 异步写入汇川PLC数据（支持 M区线圈 / D区保持寄存器）
        /// </summary>
        /// <param name="address">PLC地址（如 "M0"、"D100"）</param>
        /// <param name="value">写入值（M区："1"/"true"=通；D区：0-65535整数）</param>
        /// <returns>写入成功返回 true</returns>
        public override async Task<bool> WriteAsync(string address, string value)
        {
            return await Task.Run(() =>
            {
                ushort registerAddr;
                var addressType = PlcAddressHelper.ParseType(address, out registerAddr);

                switch (addressType)
                {
                    case PlcAddressType.M:
                        // M区线圈写入："1" 或 "true"（忽略大小写）视为导通，其他视为断开
                        bool coilValue = value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase);
                        _master.WriteSingleCoil(_slaveId, registerAddr, coilValue);
                        MyLogger.Debug($"汇川PLC M区写入：地址[{address}]，值[{coilValue}]");
                        break;
                    case PlcAddressType.D:
                        // D区保持寄存器写入：校验值为16位无符号整数（0-65535）
                        if (!ushort.TryParse(value, out ushort registerValue))
                        {
                            throw new ArgumentException($"汇川PLC D区写入值无效：{value}，需传入0-65535的整数（16位无符号）");
                        }
                        _master.WriteSingleRegister(_slaveId, registerAddr, registerValue);
                        MyLogger.Debug($"汇川PLC D区写入：地址[{address}]，值[{registerValue}]");
                        break;
                    default:
                        throw new NotSupportedException($"汇川PLC不支持的地址类型: {addressType}，仅支持 M区（线圈）和 D区（保持寄存器）");
                }
                return true;
            }).ConfigureAwait(false);
        }
        #endregion

        #region 同步读写兼容（继承基类，无需重复实现）
        // 直接继承 ModbusTcpBase 中带 [Obsolete] 标记的同步 Read/Write 方法
        // 旧代码可直接调用，新代码建议使用异步 ReadAsync/WriteAsync
        #endregion
    }
}