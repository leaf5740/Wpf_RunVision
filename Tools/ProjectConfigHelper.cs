using Newtonsoft.Json;
using System.IO;
using Wpf_RunVision.Models;

namespace Wpf_RunVision.Tools
{
    /// <summary>
    /// 配置管理 + 存储（单例）
    /// </summary>
    public sealed class ProjectConfigHelper
    {
        private const string ConfigFileName = "config.json";

        // 单例实例
        public static ProjectConfigHelper Instance { get; } = new ProjectConfigHelper();

        // 当前配置对象
        public ProjectConfig CurrentConfig { get; private set; } = new ProjectConfig();

        // 当前配置文件夹路径
        public string CurrentFolder { get; private set; }

        private ProjectConfigHelper() { }

        /// <summary>
        /// 加载配置（指定方案文件夹）
        /// </summary>
        public void LoadConfig(string folder)
        {
            CurrentFolder = folder;
            string filePath = Path.Combine(folder, ConfigFileName);

            if (!File.Exists(filePath))
            {
                CurrentConfig = new ProjectConfig();
                SaveConfig(); // 创建默认配置
                return;
            }

            var json = File.ReadAllText(filePath);
            CurrentConfig = JsonConvert.DeserializeObject<ProjectConfig>(json) ?? new ProjectConfig();
        }

        /// <summary>
        /// 保存配置到当前方案文件夹
        /// </summary>
        public void SaveConfig()
        {
            if (string.IsNullOrEmpty(CurrentFolder))
                return;

            string filePath = Path.Combine(CurrentFolder, ConfigFileName);
            var json = JsonConvert.SerializeObject(CurrentConfig, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

    }
}
