using System;
using System.Threading.Tasks;

namespace Wpf_RunVision.Services.Plc
{
    /// <summary>
    /// PLC服务接口（连接/断开同步，读写异步）
    /// </summary>
    public interface IPlcService
    {
        /// <summary>
        /// 同步连接PLC
        /// </summary>
        /// <param name="ip">PLC IP地址</param>
        /// <param name="port">端口号（默认502）</param>
        /// <returns>连接成功返回 true，失败返回 false</returns>
        bool Connect(string ip, int port = 502);

        /// <summary>
        /// 同步断开PLC连接
        /// </summary>
        void Disconnect();

        /// <summary>
        /// 异步写入PLC数据
        /// </summary>
        /// <param name="address">PLC寄存器地址</param>
        /// <param name="value">要写入的值</param>
        /// <returns>写入成功返回 true，失败返回 false</returns>
        Task<bool> WriteAsync(string address, string value);

        /// <summary>
        /// 异步读取PLC数据
        /// </summary>
        /// <param name="address">PLC寄存器地址</param>
        /// <returns>读取到的整数结果</returns>
        Task<int> ReadAsync(string address);

        /// <summary>
        /// PLC连接状态（同步查询）
        /// </summary>
        bool IsConnected { get; }
    }
}