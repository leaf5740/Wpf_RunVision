using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using VM.Core;
using Wpf_RunVision.Models;
using Wpf_RunVision.Utils;

namespace Wpf_RunVision.ViewModels.TabViewModels
{
    public partial class SolutionTabViewModel : ObservableObject
    {
        #region 绑定属性
        [ObservableProperty] private ObservableCollection<FlowStepModel> _flowSteps = new();
        [ObservableProperty] private FlowStepModel? _selectedFlowStep;
        [ObservableProperty] private string? _totalPcs;
        [ObservableProperty] private string? _totalImages;
        [ObservableProperty] private string? _imagePcs;
        [ObservableProperty] private string? _imageIndexRule;
        [ObservableProperty] private string? _pcsSorting;
        [ObservableProperty] private ObservableCollection<string> _flowTypes = new();
        [ObservableProperty] private string? _selectedFlowType;
        #endregion

        #region 命令定义
        // 添加流程步骤命令
        [RelayCommand]
        private void AddFlowStep()
        {
            // 输入校验
            if (!ValidateAddInput(out string errorMsg))
            {
                MessageBox.Show(errorMsg, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 创建新流程步骤
            var newStep = new FlowStepModel
            {
                StepName = SelectedFlowType!,
                Pcs = int.Parse(ImagePcs!),
                ImageIndex = ImageIndexRule!,
                Remark = "备注空"
            };

            FlowSteps.Add(newStep);

            // 清空当前输入项
            ImageIndexRule = string.Empty;
        }

        // 删除选中流程步骤命令
        [RelayCommand(CanExecute = nameof(CanDeleteFlowStep))]
        private void DeleteSelectedFlowStep()
        {
            if (SelectedFlowStep == null) return;

            var result = MessageBox.Show(
                $"确定删除「{SelectedFlowStep.StepName}」?",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // 删除选中的流程步骤
                FlowSteps.Remove(SelectedFlowStep);
                SelectedFlowStep = null;
            }
        }

        // 保存配置命令
        [RelayCommand]
        private void SaveConfig()
        {
            try
            {

                var configHelper = ProjectConfigHelper.Instance;
                var currentConfig = configHelper.CurrentConfigs;
                if (currentConfig == null) return;

                // 保存配置
                currentConfig.SolutionConfig = new SolutionModel
                {
                    TotalPcs = TotalPcs,
                    TotalImages = TotalImages,
                    FlowSteps = FlowSteps.ToList(),
                    PcsSorting = PcsSorting
                };
                configHelper.SaveConfig();

                // 提示保存成功
                MessageBox.Show("配置已成功保存！", "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存异常：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region 命令执行条件

        private bool CanDeleteFlowStep()
        {
            // 确保有选中项才能执行删除操作
            return SelectedFlowStep != null;
        }

        #endregion

        #region 数据校验逻辑
        private bool ValidateAddInput(out string errorMsg)
        {
            if (string.IsNullOrEmpty(TotalPcs))
            {
                errorMsg = "总PCS不可为空！";
                return false;
            }
            if (string.IsNullOrEmpty(TotalImages))
            {
                errorMsg = "总图片数量不可为空！";
                return false;
            }
            if (string.IsNullOrEmpty(SelectedFlowType))
            {
                errorMsg = "请选择流程类型！";
                return false;
            }
            if (string.IsNullOrEmpty(ImagePcs))
            {
                errorMsg = "单张图片PCS不可为空！";
                return false;
            }
            if (string.IsNullOrEmpty(ImageIndexRule))
            {
                errorMsg = "图片索引规则不可为空！";
                return false;
            }
            if (!int.TryParse(ImagePcs, out int pcs) || pcs <= 0)
            {
                errorMsg = "单张图片PCS必须为大于0的整数！";
                return false;
            }
            if (FlowSteps.Any(s => s.StepName.Equals(SelectedFlowType!, StringComparison.OrdinalIgnoreCase)))
            {
                errorMsg = $"流程「{SelectedFlowType}」已存在，请勿重复添加！";
                return false;
            }
            if (FlowSteps.Any(s => s.ImageIndex.Equals(ImageIndexRule!, StringComparison.OrdinalIgnoreCase)))
            {
                errorMsg = $"流程索引「{ImageIndexRule}」已存在，请勿重复添加！";
                return false;
            }
            errorMsg = string.Empty;
            return true;
        }

        #endregion

        #region 初始化逻辑
        public SolutionTabViewModel()
        {
            LoadFlowTypes();
            LoadSavedConfig();

            // 属性变化时动态控制命令的可执行性
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SelectedFlowStep))
                {
                    DeleteSelectedFlowStepCommand.NotifyCanExecuteChanged(); // 当选中项改变时，刷新删除按钮的可执行状态
                }

                if (e.PropertyName is nameof(TotalPcs) || e.PropertyName is nameof(TotalImages) ||
                    e.PropertyName is nameof(ImagePcs) || e.PropertyName is nameof(SelectedFlowType))
                {
                    AddFlowStepCommand.NotifyCanExecuteChanged();
                    SaveConfigCommand.NotifyCanExecuteChanged();
                }
            };
        }

        private void LoadFlowTypes()
        {
            try
            {
                var processNames = VmSolution.Instance.GetAllProcedureList()
                    .astProcessInfo.Where(p => !string.IsNullOrEmpty(p.strProcessName))
                    .Select(p => p.strProcessName.Trim())
                    .Distinct()
                    .OrderBy(name => name)
                    .ToList();

                FlowTypes.Clear();
                foreach (var name in processNames)
                {
                    FlowTypes.Add(name);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载流程类型失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSavedConfig()
        {
            try
            {
                var configHelper = ProjectConfigHelper.Instance;
                var currentConfigs = configHelper.CurrentConfigs;
                if (currentConfigs?.SolutionConfig == null) return;

                var savedConfig = currentConfigs.SolutionConfig;
                TotalPcs = savedConfig.TotalPcs ?? string.Empty;
                TotalImages = savedConfig.TotalImages ?? string.Empty;
                PcsSorting = savedConfig.PcsSorting ?? string.Empty;
                FlowSteps.Clear();
                if (savedConfig.FlowSteps != null)
                {
                    foreach (var step in savedConfig.FlowSteps)
                    {
                        FlowSteps.Add(new FlowStepModel
                        {
                            StepName = step.StepName ?? "未命名流程",
                            Pcs = step.Pcs ?? 0,
                            ImageIndex = step.ImageIndex ?? "无",
                            Remark = step.Remark ?? string.Empty
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载配置失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion
    }
}
