using System;

namespace Wpf_RunVision.Services.Plc
{
    public static class PlcFactory
    {
        public static IPlcService Create(string brand)
        {
            switch (brand.Trim())
            {
                case "汇川":
                    return new InovanceModbusTcp();
                case "三菱":
                    return new MitsubishiModbusTcp();
                default:
                    throw new NotSupportedException($"不支持的PLC品牌: {brand}");
            }
        }
    }
}
