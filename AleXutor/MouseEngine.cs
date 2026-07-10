using System;
using System.Threading;
using System.Windows.Forms;

namespace AleXutor
{
    public static class MouseEngine
    {
        public static void MoveTo(int x, int y)
        {
            WinAPI.SetCursorPos(x, y);
        }

        public static void MoveSmooth(int x, int y, int steps = 20, int delayMs = 10)
        {
            WinAPI.GetCursorPos(out var cur);
            double dx = (x - cur.X) / (double)steps;
            double dy = (y - cur.Y) / (double)steps;
            for (int i = 1; i <= steps; i++)
            {
                WinAPI.SetCursorPos(cur.X + (int)(dx * i), cur.Y + (int)(dy * i));
                Thread.Sleep(delayMs);
            }
        }

        public static System.Drawing.Point GetPos()
        {
            WinAPI.GetCursorPos(out var p);
            return new System.Drawing.Point(p.X, p.Y);
        }

        public static void LeftClick(int x, int y)
        {
            MoveTo(x, y);
            Thread.Sleep(30);
            WinAPI.mouse_event(WinAPI.MOUSEEVENTF_LEFTDOWN, x, y, 0, IntPtr.Zero);
            Thread.Sleep(30);
            WinAPI.mouse_event(WinAPI.MOUSEEVENTF_LEFTUP, x, y, 0, IntPtr.Zero);
        }

        public static void DoubleClick(int x, int y)
        {
            LeftClick(x, y);
            Thread.Sleep(80);
            LeftClick(x, y);
        }

        public static void RightClick(int x, int y)
        {
            MoveTo(x, y);
            Thread.Sleep(30);
            WinAPI.mouse_event(WinAPI.MOUSEEVENTF_RIGHTDOWN, x, y, 0, IntPtr.Zero);
            Thread.Sleep(30);
            WinAPI.mouse_event(WinAPI.MOUSEEVENTF_RIGHTUP, x, y, 0, IntPtr.Zero);
        }

        public static void MiddleClick(int x, int y)
        {
            MoveTo(x, y);
            Thread.Sleep(30);
            WinAPI.mouse_event(WinAPI.MOUSEEVENTF_MIDDLEDOWN, x, y, 0, IntPtr.Zero);
            Thread.Sleep(30);
            WinAPI.mouse_event(WinAPI.MOUSEEVENTF_MIDDLEUP, x, y, 0, IntPtr.Zero);
        }

        public static void ScrollUp(int x, int y, int amount = 3)
        {
            MoveTo(x, y);
            for (int i = 0; i < amount; i++)
            {
                WinAPI.mouse_event(WinAPI.MOUSEEVENTF_WHEEL, x, y, 120, IntPtr.Zero);
                Thread.Sleep(20);
            }
        }

        public static void ScrollDown(int x, int y, int amount = 3)
        {
            MoveTo(x, y);
            for (int i = 0; i < amount; i++)
            {
                WinAPI.mouse_event(WinAPI.MOUSEEVENTF_WHEEL, x, y, unchecked((uint)-120), IntPtr.Zero);
                Thread.Sleep(20);
            }
        }

        public static void Drag(int fromX, int fromY, int toX, int toY)
        {
            MoveTo(fromX, fromY);
            Thread.Sleep(50);
            WinAPI.mouse_event(WinAPI.MOUSEEVENTF_LEFTDOWN, fromX, fromY, 0, IntPtr.Zero);
            Thread.Sleep(50);
            MoveSmooth(toX, toY);
            Thread.Sleep(50);
            WinAPI.mouse_event(WinAPI.MOUSEEVENTF_LEFTUP, toX, toY, 0, IntPtr.Zero);
        }
    }
}
