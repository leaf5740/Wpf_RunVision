using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wpf_RunVision.Models
{
    public class ImageSaveModel
    {
        public string? OkImageSavePath { get; set; }  // 存储OK图路径
        public string? NgImageSavePath { get; set; }  // 存储NG图路径
        public int CompressionLevel { get; set; } = 80; // 图像压缩等级
    }
}
