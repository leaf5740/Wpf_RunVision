using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wpf_RunVision.Services.Cameras;
using Wpf_RunVision.Services.Plc;

public class VisionCore
{
    private readonly List<ICameraService> _cameras;
    private readonly IPlcService _plc;

    public VisionCore(List<ICameraService> cameras, IPlcService plc)
    {
        _cameras = cameras ?? throw new ArgumentNullException(nameof(cameras));
        _plc = plc ?? throw new ArgumentNullException(nameof(plc));
    }

    /// <summary>
    /// 启动核心流程（生产者 + 消费者）
    /// </summary>
    public void Start()
    {
        Task.Run(ProducerLoop);
        Task.Run(ConsumerLoop);
    }

    /// <summary>
    /// 生产者：从 PLC 获取触发信号，采集相机图像
    /// </summary>
    private async Task ProducerLoop()
    {
        while (true)
        {
           
        }
    }

    /// <summary>
    /// 消费者：取出图像 → 跑算法 → 存数据库 → PLC 输出
    /// </summary>
    private void ConsumerLoop()
    {
      
    }

    /// 队列任务对象
    /// </summary>
    private class TaskItem
    {
        public string CameraSN { get; set; }
        public object Image { get; set; } // 可以是 Bitmap / Mat / CogImage
        public DateTime Timestamp { get; set; }
    }
}
