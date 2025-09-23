using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wpf_RunVision.Services.Plc
{
    public static class PlcFactory
    {
        public static IPlcService Create(string brand)
        {
            switch (brand)
            {
                case "汇川":
                    return new InovancePlcService();
                case "三菱":
                    return new MitsubishiPlcService();
                default:
                    throw new NotSupportedException($"不支持的PLC品牌: {brand}");
            }
        }
    }
}
