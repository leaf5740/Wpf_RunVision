using NLog.Config;
using NLog.Targets;
using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using YourApp.Tools;

namespace Wpf_RunVision
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            //如果 NLog.config 在项目根目录，一般不需要手动配置
            //var logger = LogManager.GetCurrentClassLogger();
            //logger.Info("程序启动！");

            // 注册全局异常捕获
            MyLogger.Instance.RegisterGlobalExceptionHandlers();
        }
    }
}
