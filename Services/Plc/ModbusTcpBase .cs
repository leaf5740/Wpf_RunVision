using Modbus.Device;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Wpf_RunVision.Utils;

namespace Wpf_RunVision.Services.Plc
{
    /// <summary>
    /// Modbus TCP 基类（同步连接/断开，异步读写）
    /// </summary>
    public abstract class ModbusTcpBase : IPlcService
    {
        protected TcpClient _client;
        protected IModbusMaster _master;

        /// <summary>
        /// PLC连接状态（即时查询）
        /// </summary>
        public bool IsConnected => _client != null && _client.Connected && _master != null;

        #region 同步连接/断开（核心不变）
        /// <summary>
        /// 同步连接PLC（原生TCP连接，带超时配置）
        /// </summary>
        public virtual bool Connect(string ip, int port = 502)
        {
            try
            {
                // 先释放原有连接
                if (_client != null)
                {
                    Disconnect();
                }

                // 同步TCP连接（符合需求：连接无需异步）
                _client = new TcpClient();
                _client.Connect(ip, port);
                _master = ModbusIpMaster.CreateIp(_client);

                // Modbus超时配置（3秒，可调整）
                _master.Transport.ReadTimeout = 3000;
                _master.Transport.WriteTimeout = 3000;

                MyLogger.Info($"PLC [ {ip}:{port}] 连接成功！");
                return true;
            }
            catch (SocketException ex)
            {
                MyLogger.Error($"PLC连接失败（网络异常）: {ip}:{port}，错误：{ex.Message}");
                CleanupResources();
                return false;
            }
            catch (Exception ex)
            {
                MyLogger.Error($"PLC连接异常: {ip}:{port}，错误：{ex.Message}", ex);
                CleanupResources();
                return false;
            }
        }

        /// <summary>
        /// 同步断开PLC连接（释放资源）
        /// </summary>
        public virtual void Disconnect()
        {
            try
            {
                if (_client != null)
                {
                    if (_client.Connected)
                        _client.Close();
                    _client.Dispose();
                }
                _master = null;
                MyLogger.Warn("PLC已断开连接");
            }
            catch (Exception ex)
            {
                MyLogger.Error($"断开PLC异常: {ex.Message}", ex);
            }
            finally
            {
                _client = null;
            }
        }
        #endregion

        #region 异步读写（抽象方法，子类实现）
        /// <summary>
        /// 异步读取PLC数据（子类实现具体逻辑）
        /// </summary>
        public abstract Task<int> ReadAsync(string address);

        /// <summary>
        /// 异步写入PLC数据（子类实现具体逻辑）
        /// </summary>
        public abstract Task<bool> WriteAsync(string address, string value);
        #endregion

        #region 兼容原有同步读写（逐步淘汰）
        [Obsolete("已过时，请使用异步方法 ReadAsync，避免阻塞线程")]
        public virtual int Read(string address)
        {
            // 同步调用异步方法，确保旧代码兼容
            return ReadAsync(address).GetAwaiter().GetResult();
        }

        [Obsolete("已过时，请使用异步方法 WriteAsync，避免阻塞线程")]
        public virtual bool Write(string address, string value)
        {
            // 同步调用异步方法，确保旧代码兼容
            return WriteAsync(address, value).GetAwaiter().GetResult();
        }
        #endregion

        #region 资源清理辅助方法
        private void CleanupResources()
        {
            _client?.Dispose();
            _client = null;
            _master = null;
        }
        #endregion
    }
}