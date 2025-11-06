using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wpf_RunVision.Services.MySql
{
    /// <summary>
    /// Code 对照表
    /// 对应数据库 data2 表
    /// </summary>
    public class CodeTableName
    {
        /// <summary>
        /// 主键索引
        /// </summary>
        [Key]
        public int indenx { get; set; }

        /// <summary>
        /// 工件 / 产品 Code
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// 数据时间
        /// </summary>
        public DateTime DetaTime { get; set; } = DateTime.Now;
    }
}
