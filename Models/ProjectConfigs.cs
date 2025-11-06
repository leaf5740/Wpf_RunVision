using Wpf_RunVision.Models;

public class ProjectConfigs
{
    public List<CameraModel> CamerasConfigs { get; set; } = new List<CameraModel>();
    public PlcModel PlcConfig { get; set; } = new PlcModel();

    public DatabaseModel DatabaseConfig { get; set; } = new DatabaseModel();

    public SolutionModel SolutionConfig { get; set; } = new SolutionModel();

    public ImageSaveModel ImageSaveModel { get; set; } = new ImageSaveModel();
}
