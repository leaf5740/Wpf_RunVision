using System;
using System.IO;
using System.Management;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;

namespace Wpf_RunVision.Utils
{
    /// <summary>
    /// NAS 连接检测工具（支持网络路径和映射盘）
    /// </summary>
    public sealed class NasConnectionChecker
    {
        private static readonly Lazy<NasConnectionChecker> _instance =
            new Lazy<NasConnectionChecker>(() => new NasConnectionChecker());

        public static NasConnectionChecker Instance => _instance.Value;

        private NasConnectionChecker() { }

        /// <summary>
        /// 判断NAS是否连接（支持映射盘或UNC路径）
        /// </summary>
        /// <param name="nasPath">如 @"\\192.168.1.10\share" 或 "S:\CloudMusic"</param>
        public bool IsConnected(string nasPath)
        {
            if (string.IsNullOrWhiteSpace(nasPath))
            {
                LogError("NAS路径不能为空");
                return false;
            }

            string targetPath = nasPath;

            // ✅ 如果是映射盘（如 S:\）
            if (Regex.IsMatch(nasPath, @"^[A-Z]:\\", RegexOptions.IgnoreCase))
            {
                string networkPath = GetNetworkPathFromDrive(nasPath.Substring(0, 2));
                if (!string.IsNullOrEmpty(networkPath))
                {
                    targetPath = networkPath;
                    Console.WriteLine($"映射盘 {nasPath.Substring(0, 2)} → 实际路径：{networkPath}");
                }
                else
                {
                    LogError($"无法获取映射盘 {nasPath.Substring(0, 2)} 的网络路径");
                    return CheckPathAccess(nasPath); // 本地路径检测
                }
            }

            // ✅ 提取 IP 并 Ping 测试
            string nasIp = ExtractIpFromPath(targetPath);
            if (string.IsNullOrWhiteSpace(nasIp))
            {
                LogError($"无法从路径解析IP：{targetPath}");
                return CheckPathAccess(targetPath);
            }

            if (!PingIp(nasIp))
            {
                LogError($"NAS网络不可达（IP：{nasIp}）");
                return false;
            }

            // ✅ 检查访问权限
            if (!CheckPathAccess(targetPath))
            {
                LogError($"NAS路径不可访问（路径：{targetPath}）");
                return false;
            }

            Console.WriteLine($"✅ NAS已连接成功（IP：{nasIp}，路径：{targetPath}）");
            return true;
        }

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
                LogError($"获取映射盘网络路径失败：{ex.Message}");
            }
            return null;
        }

        private string ExtractIpFromPath(string path)
        {
            var match = Regex.Match(path, @"^\\\\([\d.]+)\\", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : null;
        }

        private bool PingIp(string ip)
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = ping.Send(ip, 3000);
                    return reply.Status == IPStatus.Success;
                }
            }
            catch (Exception ex)
            {
                LogError($"Ping测试异常（IP：{ip}）：{ex.Message}");
                return false;
            }
        }

        private bool CheckPathAccess(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                    return false;

                using (var enumerator = Directory.EnumerateFileSystemEntries(path).GetEnumerator())
                {
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
            Console.WriteLine($"❌ {DateTime.Now:HH:mm:ss} - {msg}");
        }
    }
}
