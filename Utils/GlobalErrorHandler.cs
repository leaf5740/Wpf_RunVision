using System;
using System.Threading.Tasks;
using System.Windows;
using Wpf_RunVision.Utils;

namespace Wpf_RunVision.Utils
{
    /// <summary>
    /// 全局错误处理类，提供统一的错误处理机制
    /// </summary>
    public static class GlobalErrorHandler
    {
        /// <summary>
        /// 初始化全局错误处理
        /// </summary>
        public static void Initialize()
        {
            // 注册MyLogger的全局异常处理
            MyLogger.Instance.RegisterGlobalExceptionHandlers(true);
            
            // 添加额外的AppDomain异常处理
            AppDomain.CurrentDomain.FirstChanceException += (sender, e) =>
            {
                // 记录所有第一次捕获的异常，有助于调试
                if (e.Exception is not OperationCanceledException)
                {
                    MyLogger.Debug($"首次捕获异常: {e.Exception.GetType().Name}: {e.Exception.Message}");
                }
            };
        }
        
        /// <summary>
        /// 安全执行操作，捕获并记录异常
        /// </summary>
        /// <param name="action">要执行的操作</param>
        /// <param name="operationName">操作名称，用于日志记录</param>
        /// <param name="showErrorToUser">是否向用户显示错误</param>
        /// <returns>操作是否成功</returns>
        public static bool SafeExecute(Action action, string operationName = "操作", bool showErrorToUser = true)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            
            try
            {
                action();
                MyLogger.Operation("系统", operationName, success: true);
                return true;
            }
            catch (Exception ex)
            {
                HandleException(ex, operationName, showErrorToUser);
                return false;
            }
        }
        
        /// <summary>
        /// 安全执行异步操作，捕获并记录异常
        /// </summary>
        /// <param name="func">要执行的异步操作</param>
        /// <param name="operationName">操作名称，用于日志记录</param>
        /// <param name="showErrorToUser">是否向用户显示错误</param>
        /// <returns>操作是否成功</returns>
        public static async Task<bool> SafeExecuteAsync(Func<Task> func, string operationName = "异步操作", bool showErrorToUser = true)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            
            try
            {
                await func();
                MyLogger.Operation("系统", operationName, success: true);
                return true;
            }
            catch (Exception ex)
            {
                HandleException(ex, operationName, showErrorToUser);
                return false;
            }
        }
        
        /// <summary>
        /// 安全执行带返回值的操作，捕获并记录异常
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="func">要执行的函数</param>
        /// <param name="defaultValue">异常时返回的默认值</param>
        /// <param name="operationName">操作名称，用于日志记录</param>
        /// <param name="showErrorToUser">是否向用户显示错误</param>
        /// <returns>操作结果或默认值</returns>
        public static T SafeExecute<T>(Func<T> func, T defaultValue = default(T), string operationName = "操作", bool showErrorToUser = false)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            
            try
            {
                var result = func();
                MyLogger.Operation("系统", operationName, success: true);
                return result;
            }
            catch (Exception ex)
            {
                HandleException(ex, operationName, showErrorToUser);
                return defaultValue;
            }
        }
        
        /// <summary>
        /// 安全执行带返回值的异步操作，捕获并记录异常
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="func">要执行的异步函数</param>
        /// <param name="defaultValue">异常时返回的默认值</param>
        /// <param name="operationName">操作名称，用于日志记录</param>
        /// <param name="showErrorToUser">是否向用户显示错误</param>
        /// <returns>操作结果或默认值</returns>
        public static async Task<T> SafeExecuteAsync<T>(Func<Task<T>> func, T defaultValue = default(T), string operationName = "异步操作", bool showErrorToUser = false)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            
            try
            {
                var result = await func();
                MyLogger.Operation("系统", operationName, success: true);
                return result;
            }
            catch (Exception ex)
            {
                HandleException(ex, operationName, showErrorToUser);
                return defaultValue;
            }
        }
        
        /// <summary>
        /// 处理异常
        /// </summary>
        /// <param name="ex">异常对象</param>
        /// <param name="operationName">操作名称</param>
        /// <param name="showErrorToUser">是否向用户显示错误</param>
        private static void HandleException(Exception ex, string operationName, bool showErrorToUser)
        {
            MyLogger.Error($"{operationName}失败: {ex.Message}", ex);
            MyLogger.Operation("系统", operationName, success: false);
            
            if (showErrorToUser)
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        $"{operationName}失败: {ex.Message}",
                        "错误",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
            }
        }
    }
}