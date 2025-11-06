namespace Wpf_RunVision.Models{

    public class CameraModel 
    {
        //相机品牌
        public string? Brand { get; set; }
        //相机序列号
        public string? Sn { get; set; }
        //相机备注信息
        public string? Remark { get; set; }
        //相机完成信号plc
        public string? PlcAddress { get; set; }

    }
}

