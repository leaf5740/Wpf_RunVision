using System;
using System.Collections.Generic;
using System.Linq;

namespace Wpf_RunVision.Services.Plc
{
    public static class PlcFactory
    {
        private static readonly List<IPlcService> _plcServices = new List<IPlcService>
        {
            new MitsubishiPlcService(),
            new InovancePlcService()
        };

        public static IPlcService Create(string brand)
        {
            var service = _plcServices.FirstOrDefault(p => p.Brand == brand);
            if (service == null)
                throw new NotSupportedException($"未支持的PLC品牌: {brand}");
            return service;
        }
    }
}
