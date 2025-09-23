using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wpf_RunVision.Services.Cameras
{
    public static class CameraFactory
    {
        public static ICameraService Create(string brand, string sn)
        {
            switch (brand)
            {
                case "海康":
                    return new HikvisionCameraService(sn);
                case "大恒":
                    return new DahengCameraService(sn);   
                default:
                    throw new NotSupportedException($"未支持的相机品牌: {brand}");
            }
        }
    }


}
