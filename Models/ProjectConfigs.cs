using System.Collections.Generic;
using Wpf_RunVision.Models;

public class ProjectConfigs
{
    public List<CameraModels> Cameras { get; set; } = new List<CameraModels>();
    public PlcModels PlcConfig { get; set; } = new PlcModels();
}
