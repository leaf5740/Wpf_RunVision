using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using WindowsInput;
using WindowsInput.Native;

namespace Wpf_RunVision.Utils
{
    /// <summary>
    /// 屏幕键盘辅助类，兼容 Windows 10 和 Windows 11。
    /// 功能：
    /// 1. 弹出系统触摸键盘（优先用快捷键 Win+Ctrl+O 调用）
    /// 2. 快捷键调用失败则尝试启动触摸键盘程序 TabTip.exe
    /// 3. 若 TabTip.exe 不存在或启动失败，则启动传统屏幕键盘 osk.exe
    /// 4. 关闭屏幕键盘时，直接杀进程关闭，避免快捷键切换导致键盘反向打开
    /// </summary>
    internal static class KeyboardHelper
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_SHOW = 5;

        /// <summary>
        /// 显示系统屏幕键盘。
        /// 先尝试模拟快捷键 Win+Ctrl+O 调用触摸键盘（兼容 Win11 及部分 Win10），
        /// 如果快捷键调用失败，则尝试启动 TabTip.exe，
        /// 仍失败则尝试启动传统屏幕键盘 osk.exe。
        /// </summary>
        public static void ShowKeyboard()
        {
            if (TryToggleTouchKeyboardByHotkey())
                return;

            if (TryStartProcess(@"microsoft shared\ink\TabTip.exe", "TabTip"))
                return;

            TryStartProcess("osk.exe", "osk");
        }

        /// <summary>
        /// 关闭所有屏幕键盘相关进程（TabTip 和 osk）。
        /// 这里不使用快捷键切换关闭，直接杀进程，避免键盘反向打开。
        /// </summary>
        public static void CloseKeyboard()
        {
            KillProcess("TabTip");
            KillProcess("osk");
        }

        /// <summary>
        /// 模拟快捷键 Win+Ctrl+O，触发触摸键盘开关。
        /// 该快捷键是切换开关，调用时能弹出或关闭触摸键盘。
        /// 这里只用于打开键盘，关闭时不调用。
        /// </summary>
        /// <returns>是否成功模拟</returns>
        private static bool TryToggleTouchKeyboardByHotkey()
        {
            try
            {
                var sim = new InputSimulator();
                sim.Keyboard.ModifiedKeyStroke(
                    new[] { VirtualKeyCode.LWIN, VirtualKeyCode.CONTROL },
                    VirtualKeyCode.VK_O);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 尝试启动指定的程序（TabTip.exe 或 osk.exe），
        /// 如果程序已运行则激活其窗口。
        /// </summary>
        /// <param name="relativePath">程序路径（如果带路径则从 CommonProgramFiles 文件夹拼接，否则从 System 文件夹拼接）</param>
        /// <param name="processName">程序对应的进程名</param>
        /// <returns>是否成功启动或激活</returns>
        private static bool TryStartProcess(string relativePath, string processName)
        {
            string fullPath = relativePath.Contains("\\")
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles), relativePath)
                : Path.Combine(Environment.SystemDirectory, relativePath);

            if (!File.Exists(fullPath))
                return false;

            if (!IsRunning(processName))
            {
                try
                {
                    Process.Start(fullPath);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                ActivateWindow(processName);
                return true;
            }
        }

        private static bool IsRunning(string name)
            => Process.GetProcessesByName(name).Length > 0;

        private static void KillProcess(string name)
        {
            foreach (var p in Process.GetProcessesByName(name))
            {
                try
                {
                    p.Kill();
                }
                catch
                {
                    // 忽略异常（权限不足或已关闭）
                }
            }
        }

        private static void ActivateWindow(string name)
        {
            var ps = Process.GetProcessesByName(name);
            if (ps.Length > 0)
            {
                IntPtr hWnd = ps[0].MainWindowHandle;
                if (hWnd != IntPtr.Zero)
                {
                    ShowWindow(hWnd, SW_SHOW);
                    SetForegroundWindow(hWnd);
                }
            }
        }

    }
}
