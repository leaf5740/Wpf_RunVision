using CommunityToolkit.Mvvm.ComponentModel;

namespace Wpf_RunVision.Models
{
    /// <summary>
    /// 主界面绑定状态类，用于存放 UI 实时显示的数据
    /// </summary>
    public class MainViewState : ObservableObject
    {
        private string _etchingCode;
        private string _paperCode;
        private string _dbStatus;
        private string _plcStatus;
        private string _mesStatus;
        private string _nasStatus;
        private double _progressValue;
        private string _runTime;
        private string _ctTime;
        private string _singleFlowTime;

        public string EtchingCode
        {
            get => _etchingCode;
            set => SetProperty(ref _etchingCode, value);
        }

        public string PaperCode
        {
            get => _paperCode;
            set => SetProperty(ref _paperCode, value);
        }

        public string DbStatus
        {
            get => _dbStatus;
            set => SetProperty(ref _dbStatus, value);
        }

        public string PlcStatus
        {
            get => _plcStatus;
            set => SetProperty(ref _plcStatus, value);
        }

        public string MesStatus
        {
            get => _mesStatus;
            set => SetProperty(ref _mesStatus, value);
        }

        public string NasStatus
        {
            get => _nasStatus;
            set => SetProperty(ref _nasStatus, value);
        }

        public double ProgressValue
        {
            get => _progressValue;
            set => SetProperty(ref _progressValue, value);
        }

        public string RunTime
        {
            get => _runTime;
            set => SetProperty(ref _runTime, value);
        }

        public string CtTime
        {
            get => _ctTime;
            set => SetProperty(ref _ctTime, value);
        }

        public string SingleFlowTime
        {
            get => _singleFlowTime;
            set => SetProperty(ref _singleFlowTime, value);
        }
    }
}
