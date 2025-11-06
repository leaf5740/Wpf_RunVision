using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Wpf_RunVision.Models
{
    /// <summary>
    /// 方案配置主模型（包含总配置信息和流程列表）
    /// </summary>
    public class SolutionModel
    {
        /// <summary>
        /// 总PCS数量
        /// </summary>
        public string? TotalPcs { get; set; }

        /// <summary>
        /// 总图片数量
        /// </summary>
        public string? TotalImages { get; set; }

        /// <summary>
        /// PCS排序方式
        /// </summary>
        public string? PcsSorting { get; set; }

        /// <summary>
        /// 流程步骤列表
        /// </summary>
        public List<FlowStepModel>? FlowSteps { get; set; } = new List<FlowStepModel>();
    }

    /// <summary>
    /// 流程步骤模型（仅包含单个流程的信息，无总配置属性）
    /// </summary>
    public class FlowStepModel
    {
        /// <summary>
        /// 流程名称
        /// </summary>
        public string? StepName { get; set; } = "未命名流程";

        /// <summary>
        /// 该流程的PCS数量
        /// </summary>
        public int? Pcs { get; set; } = 0;

        /// <summary>
        /// 该流程对应的图片索引
        /// </summary>
        public string? ImageIndex { get; set; } = "无";

        /// <summary>
        /// 该流程的备注
        /// </summary>
        public string? Remark { get; set; } = string.Empty;
    }
}