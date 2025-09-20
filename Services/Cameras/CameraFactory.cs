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
                    return new HikvisionCameraService();  // 这里传 sn，如果构造函数需要
                case "大恒":
                    return new DahengCameraService();     // 同理
                default:
                    throw new NotSupportedException($"未支持的相机品牌: {brand}");
            }
        }
    }


}
