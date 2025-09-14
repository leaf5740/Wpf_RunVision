using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wpf_RunVision.Services.Cameras
{
    public class DahengCameraService : ICameraService
    {
        public string Brand => "大恒";

        public List<string> GetAvailableSNs()
        {
            // TODO: 替换为真实 SDK 获取逻辑
            return new List<string> { "DH_SN_001", "DH_SN_002", "DH_SN_003" };
        }
    }
}
