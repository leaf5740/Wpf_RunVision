using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows;
using Wpf_RunVision.Models;
using Wpf_RunVision.Utils;

namespace Wpf_RunVision.ViewModels.TabViewModels
{
    public partial class SaveImageTabViewModel : ObservableObject
    {
        #region 属性绑定
        [ObservableProperty]
        private ImageSaveModel _imageSaveModel = new ImageSaveModel();  // 图像保存配置
        #endregion

        #region 命令
        /// <summary>
        /// 保存存图配置命令
        /// </summary>
        [RelayCommand]
        private void SaveConfig()
        {
            try
            {
                // 获取配置助手实例
                var configHelper = ProjectConfigHelper.Instance;
                var currentConfig = configHelper.CurrentConfigs;
                if (currentConfig == null) return;

                // 校验存图配置数据
                if (!ValidateImageSaveConfig(out string errorMsg))
                {
                    MessageBox.Show(errorMsg, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 组装存图配置数据
                currentConfig.ImageSaveModel = ImageSaveModel;

                // 保存到配置文件
                configHelper.SaveConfig();

                // 提示保存成功
                MessageBox.Show("存图配置已成功保存！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);

            }
            catch (Exception ex)
            {
                // 异常处理，显示加载失败的消息
                MessageBox.Show($"存图配置保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region 构造函数
        public SaveImageTabViewModel()
        {
            LoadSavedConfig();
        }
        #endregion

        #region 加载配置
        private void LoadSavedConfig()
        {
            try
            {
                // 获取配置助手实例
                var configHelper = ProjectConfigHelper.Instance;
                var currentConfigs = configHelper.CurrentConfigs;

                // 如果没有读取到配置，则直接返回
                if (currentConfigs?.ImageSaveModel == null) return;

                var savedConfig = currentConfigs.ImageSaveModel;  // 获取存图配置
                ImageSaveModel = savedConfig;
            }
            catch (Exception ex)
            {
                // 异常处理，显示加载失败的消息
                MessageBox.Show($"加载存图配置失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region 校验逻辑
        private bool ValidateImageSaveConfig(out string errorMsg)
        {
            // 校验存储路径和压缩等级
            if (string.IsNullOrEmpty(ImageSaveModel.OkImageSavePath))
            {
                errorMsg = "存储OK图路径不能为空！";
                return false;
            }

            if (string.IsNullOrEmpty(ImageSaveModel.NgImageSavePath))
            {
                errorMsg = "存储NG图路径不能为空！";
                return false;
            }

            if (ImageSaveModel.CompressionLevel < 0 || ImageSaveModel.CompressionLevel > 100)
            {
                errorMsg = "压缩等级必须在 0 到 100 之间！";
                return false;
            }

            errorMsg = string.Empty;
            return true;
        }
        #endregion
    }
}
