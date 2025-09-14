using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wpf_RunVision.Services.Cameras
{
    public class HikvisionCameraService : ICameraService
    {
        public string Brand => "海康";
        public List<string> GetAvailableSNs()
        {
            // TODO: 替换为真实 SDK 获取逻辑
            return new List<string> { "HK_SN_001", "HK_SN_002", "HK_SN_003" };
        }
    }
}
