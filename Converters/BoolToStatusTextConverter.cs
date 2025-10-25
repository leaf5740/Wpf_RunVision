using System;
using System.Globalization;
using System.Windows.Data;

namespace Wpf_RunVision.Converters
{
    /// <summary>
    /// 将 bool 值转换为连接状态文本
    /// </summary>
    public class BoolToStatusTextConverter : IValueConverter
    {
        public string TrueText { get; set; }
        public string FalseText { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? TrueText : FalseText; // 根据 bool 值返回相应的文本
            return FalseText; // 如果输入不为 bool，则默认返回 FalseText
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException(); // 不需要支持双向转换
        }
    }
}
