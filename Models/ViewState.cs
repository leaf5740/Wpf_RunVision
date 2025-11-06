using CommunityToolkit.Mvvm.ComponentModel;

namespace Wpf_RunVision.Models
{
    /// <summary>
    /// 界面状态模型（绑定UI显示数据）
    /// </summary>
    public class ViewState : ObservableObject
    {
        #region 码信息
        private string _etchingCode = "未检测";
        public string EtchingCode
        {
            get => _etchingCode;
            set => SetProperty(ref _etchingCode, value);
        }

        private string _paperCode = "未检测";
        public string PaperCode
        {
            get => _paperCode;
            set => SetProperty(ref _paperCode, value);
        }
        #endregion

        #region 服务状态（PLC/数据库/网盘）
        private bool _plcStatus;
        public bool PlcStatus
        {
            get => _plcStatus;
            set => SetProperty(ref _plcStatus, value);
        }

        private bool _dbStatus;
        public bool DbStatus
        {
            get => _dbStatus;
            set => SetProperty(ref _dbStatus, value);
        }

        private bool _nasStatus;
        public bool NasStatus
        {
            get => _nasStatus;
            set => SetProperty(ref _nasStatus, value);
        }
        #endregion

        #region 进度与时间统计
        private double _progressValue;
        public double ProgressValue
        {
            get => _progressValue;
            set => SetProperty(ref _progressValue, value);
        }

        private string _ctTime = "00:00.00";
        public string CtTime
        {
            get => _ctTime;
            set => SetProperty(ref _ctTime, value);
        }

        private string _singleFlowTime = "00:00.00";
        public string SingleFlowTime
        {
            get => _singleFlowTime;
            set => SetProperty(ref _singleFlowTime, value);
        }
        #endregion
    }
}