using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace AleXutor
{
    public static class WindowEngine
    {
        public static IntPtr Find(string title)
            => WinAPI.FindWindow(null, title);

        public static void Show(string title)
        {
            var h = Find(title);
            if (h != IntPtr.Zero) WinAPI.ShowWindow(h, WinAPI.SW_SHOW);
        }

        public static void Hide(string title)
        {
            var h = Find(title);
            if (h != IntPtr.Zero) WinAPI.ShowWindow(h, WinAPI.SW_HIDE);
        }

        public static void Minimize(string title)
        {
            var h = Find(title);
            if (h != IntPtr.Zero) WinAPI.ShowWindow(h, WinAPI.SW_MINIMIZE);
        }

        public static void Maximize(string title)
        {
            var h = Find(title);
            if (h != IntPtr.Zero) WinAPI.ShowWindow(h, WinAPI.SW_MAXIMIZE);
        }

        public static void Restore(string title)
        {
            var h = Find(title);
            if (h != IntPtr.Zero) WinAPI.ShowWindow(h, WinAPI.SW_RESTORE);
        }

        public static void Close(string title)
        {
            var h = Find(title);
            if (h != IntPtr.Zero) WinAPI.PostMessage(h, WinAPI.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
        }

        public static void Activate(string title)
        {
            var h = Find(title);
            if (h != IntPtr.Zero)
            {
                WinAPI.ShowWindow(h, WinAPI.SW_RESTORE);
                WinAPI.SetForegroundWindow(h);
            }
        }

        public static void Move(string title, int x, int y, int w, int h)
        {
            var hw = Find(title);
            if (hw != IntPtr.Zero) WinAPI.MoveWindow(hw, x, y, w, h, true);
        }

        public static (int x, int y, int w, int h) GetPos(string title)
        {
            var h = Find(title);
            if (h == IntPtr.Zero) return (0, 0, 0, 0);
            WinAPI.GetWindowRect(h, out var r);
            return (r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top);
        }

        public static string GetActiveTitle()
        {
            var h = WinAPI.GetForegroundWindow();
            var sb = new StringBuilder(256);
            WinAPI.GetWindowText(h, sb, 256);
            return sb.ToString();
        }

        public static List<string> ListAll()
        {
            var titles = new List<string>();
            WinAPI.EnumWindows((hWnd, _) =>
            {
                if (WinAPI.IsWindowVisible(hWnd))
                {
                    var sb = new StringBuilder(256);
                    WinAPI.GetWindowText(hWnd, sb, 256);
                    var t = sb.ToString().Trim();
                    if (!string.IsNullOrEmpty(t)) titles.Add(t);
                }
                return true;
            }, IntPtr.Zero);
            return titles;
        }

        public static Process? Run(string path, string args = "")
        {
            try
            {
                return Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = args,
                    UseShellExecute = true
                });
            }
            catch { return null; }
        }

        public static void KillByName(string procName)
        {
            foreach (var p in Process.GetProcessesByName(procName))
            {
                try { p.Kill(); } catch { }
            }
        }

        public static List<string> ListProcesses()
        {
            var list = new List<string>();
            foreach (var p in Process.GetProcesses())
            {
                try { list.Add($"{p.ProcessName} (PID {p.Id})"); } catch { }
            }
            list.Sort();
            return list;
        }
    }
}
