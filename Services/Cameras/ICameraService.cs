using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wpf_RunVision.Services.Cameras
{
    public interface ICameraService
    {
        string Brand { get; }
        List<string> GetAvailableSNs();
         
    }

}
