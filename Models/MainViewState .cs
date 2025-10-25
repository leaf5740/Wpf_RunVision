using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace Wpf_RunVision.Models
{
    /// <summary>
    /// 主界面绑定状态类（单例模式）
    /// 用于存放 UI 实时显示的数据，例如：
    /// 蚀刻码、纸质码、数据库/PLC/MES/NAS 状态、进度条、运行时长等。
    /// </summary>
    public sealed class MainViewState : ObservableObject
    {
        #region 单例实现
        private static readonly MainViewState _instance = new MainViewState();
        public static MainViewState Instance => _instance;

        private MainViewState() { }
        #endregion

        private string _etchingCode;
        private string _paperCode;
        private bool _dbStatus;
        private bool _plcStatus;
        private bool _mesStatus;
        private bool _nasStatus;
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

        public bool DbStatus
        {
            get => _dbStatus;
            set => SetProperty(ref _dbStatus, value);
        }

        public bool PlcStatus
        {
            get => _plcStatus;
            set => SetProperty(ref _plcStatus, value);
        }

        public bool MesStatus
        {
            get => _mesStatus;
            set => SetProperty(ref _mesStatus, value);
        }

        public bool NasStatus
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
