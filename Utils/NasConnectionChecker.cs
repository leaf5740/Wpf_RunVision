using System;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Wpf_RunVision.Utils
{
    /// <summary>
    /// NAS 连接检测工具（支持网络路径和映射盘），支持主机名解析与异步检测
    /// </summary>
    public sealed class NasConnectionChecker
    {
        private static readonly Lazy<NasConnectionChecker> _instance =
            new Lazy<NasConnectionChecker>(() => new NasConnectionChecker());

        public static NasConnectionChecker Instance => _instance.Value;

        private NasConnectionChecker() { }

        /// <summary>
        /// 异步判断 NAS 是否连接（支持映射盘或UNC路径）
        /// </summary>
        /// <param name="nasPath">如 @"\\192.168.1.10\share" 或 "S:\CloudMusic"</param>
        public bool IsConnectedAsync(string nasPath)
        {
            if (string.IsNullOrWhiteSpace(nasPath))
            {
                LogError("NAS路径不能为空");
                return false;
            }

            string targetPath = nasPath;

            // ✅ 判断是否为映射盘
            if (Regex.IsMatch(nasPath, @"^[A-Z]:\\", RegexOptions.IgnoreCase))
            {
                string driveLetter = nasPath.Substring(0, 2);
                string networkPath = GetNetworkPathFromDrive(driveLetter);
                if (!string.IsNullOrEmpty(networkPath))
                {
                    targetPath = networkPath;
                    MyLogger.Info($"检测到映射盘 {driveLetter}，转换为网络路径：{networkPath}");
                }
                else
                {
                    //LogError($"无法获取映射盘 {driveLetter} 的网络路径，尝试本地路径检测...");
                    return CheckPathAccess(nasPath); // 尝试本地路径
                }
            }

            // ✅ 解析 IP（支持主机名）
            string nasIp = ExtractIpFromPath(targetPath);
            if (string.IsNullOrWhiteSpace(nasIp))
            {
                LogError($"无法从路径解析出IP或主机名：{targetPath}");
                return CheckPathAccess(targetPath);
            }

            // ✅ 检查目录访问
            if (!CheckPathAccess(targetPath))
            {
                LogError($"NAS路径不可访问（路径：{targetPath}）");
                return false;
            }

            MyLogger.Info($"NAS连接成功（IP/主机：{nasIp}，路径：{targetPath}）！");
            return true;
        }

        #region 🔍 辅助函数

        /// <summary>
        /// 获取映射盘对应的网络路径（如 S: → \\192.168.1.100\share）
        /// </summary>
        private string GetNetworkPathFromDrive(string driveLetter)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    $"SELECT ProviderName FROM Win32_LogicalDisk WHERE DeviceID='{driveLetter}'"))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        return mo["ProviderName"]?.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                //LogError($"获取映射盘网络路径失败：{ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// 从路径中提取IP或主机名（支持 \\192.168.1.10\share 与 \\NAS-SERVER\share）
        /// </summary>
        private string ExtractIpFromPath(string path)
        {
            // 优先匹配 IP
            var ipMatch = Regex.Match(path, @"^\\\\([\d.]+)\\", RegexOptions.IgnoreCase);
            if (ipMatch.Success)
                return ipMatch.Groups[1].Value;

            // 尝试提取主机名并解析
            var hostMatch = Regex.Match(path, @"^\\\\([^\\]+)\\", RegexOptions.IgnoreCase);
            if (hostMatch.Success)
            {
                string hostName = hostMatch.Groups[1].Value;
                try
                {
                    var entry = Dns.GetHostEntry(hostName);
                    if (entry.AddressList.Length > 0)
                        return entry.AddressList[0].ToString();
                    return hostName;
                }
                catch
                {
                    return hostName; // 返回主机名以便 Ping 测试
                }
            }

            return null;
        }
        /// <summary>
        /// 检查路径是否可访问（存在+可读）
        /// </summary>
        private bool CheckPathAccess(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                    return false;

                using (var enumerator = Directory.EnumerateFileSystemEntries(path).GetEnumerator())
                {
                    // 成功枚举则认为可访问
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogError($"路径访问异常（路径：{path}）：{ex.Message}");
                return false;
            }
        }

        private void LogError(string msg)
        {
            MyLogger.Error(msg);
        }

        #endregion
    }
}
