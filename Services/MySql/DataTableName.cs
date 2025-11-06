using System;
using System.ComponentModel.DataAnnotations;

namespace Wpf_RunVision.Services.MySql
{
    /// <summary>
    /// 检测结果表
    /// 对应数据库 data1 表
    /// </summary>
    public class DataTableName
    {
        /// <summary>
        /// EF 默认名称为 ID 的变量为键值
        /// </summary>
        [Key]
        public int indenx { get; set; }

        /// <summary>
        /// 检测时间
        /// </summary>
        public DateTime DetaTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 照片名字
        /// </summary>
        public string PhotoName { get; set; }

        /// <summary>
        /// 检测 PCS 号
        /// </summary>
        public string PCS号 { get; set; }

        /// <summary>
        /// 纸质 Code 码
        /// </summary>
        public string PaperCode { get; set; }

        /// <summary>
        /// 镭射 Code 码
        /// </summary>
        public string LaserCode { get; set; }

        /// <summary>
        /// lot 号
        /// </summary>
        public string Lot { get; set; }

        /// <summary>
        /// 工号
        /// </summary>
        public string UserID { get; set; }

        /// <summary>
        /// 屏幕
        /// </summary>
        public string Item { get; set; }

        /// <summary>
        /// 机种
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// NG 点位集合
        /// </summary>
        public string PointSet { get; set; }

        /// <summary>
        /// 检测结果（OK / NG）
        /// </summary>
        public string Result { get; set; }
    }
}

