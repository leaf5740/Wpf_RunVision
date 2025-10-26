using System.Collections.Generic;
using Wpf_RunVision.Models;

public class ProjectConfigs
{
    public List<CameraModel> CamerasConfigs { get; set; } = new List<CameraModel>();
    public PlcModel PlcConfig { get; set; } = new PlcModel();

    public DatabaseModel DatabaseConfig { get; set; } = new DatabaseModel();
}
