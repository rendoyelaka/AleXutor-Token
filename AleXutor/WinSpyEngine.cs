using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace AleXutor
{
    public static class WinSpyEngine
    {
        // ── Additional P/Invoke ────────────────────────────────────────────

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(POINT pt);

        [DllImport("user32.dll")]
        private static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);

        [DllImport("user32.dll")]
        private static extern int GetDlgCtrlID(IntPtr hWnd);

        [DllImport("gdi32.dll")]
        private static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        private const uint GA_ROOT = 2;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT { public int X, Y; }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int Left, Top, Right, Bottom; }

        // ── Public result struct ───────────────────────────────────────────

        public class SpyResult
        {
            public string WindowTitle  { get; set; } = "";
            public string ClassName    { get; set; } = "";
            public int    ControlID    { get; set; }
            public int    MouseX       { get; set; }
            public int    MouseY       { get; set; }
            public int    ClientX      { get; set; }
            public int    ClientY      { get; set; }
            public string PixelColor   { get; set; } = "";
            public string WinPos       { get; set; } = "";
            public IntPtr Handle       { get; set; }
        }

        // ── Main capture ──────────────────────────────────────────────────

        public static SpyResult Capture()
        {
            GetCursorPos(out POINT pt);

            IntPtr hWnd = WindowFromPoint(pt);
            IntPtr hRoot = GetAncestor(hWnd, GA_ROOT);

            // Window title (from root)
            var titleSb = new StringBuilder(256);
            GetWindowText(hRoot, titleSb, 256);

            // Class name (of control under cursor)
            var classSb = new StringBuilder(256);
            GetClassName(hWnd, classSb, 256);

            // Control ID
            int ctrlId = GetDlgCtrlID(hWnd);

            // Window rect
            string winPos = "";
            if (GetWindowRect(hRoot, out RECT r))
                winPos = $"X={r.Left}, Y={r.Top}, W={r.Right - r.Left}, H={r.Bottom - r.Top}";

            // Client-relative coords
            POINT clientPt = pt;
            ScreenToClient(hWnd, ref clientPt);

            // Pixel color
            IntPtr hdc = GetDC(IntPtr.Zero); // screen DC
            uint raw = GetPixel(hdc, pt.X, pt.Y);
            ReleaseDC(IntPtr.Zero, hdc);
            Color color = Color.FromArgb(
                (int)(raw & 0xFF),
                (int)((raw >> 8) & 0xFF),
                (int)((raw >> 16) & 0xFF));
            string hex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";

            return new SpyResult
            {
                Handle       = hWnd,
                WindowTitle  = titleSb.ToString(),
                ClassName    = classSb.ToString(),
                ControlID    = ctrlId,
                MouseX       = pt.X,
                MouseY       = pt.Y,
                ClientX      = clientPt.X,
                ClientY      = clientPt.Y,
                PixelColor   = hex,
                WinPos       = winPos
            };
        }
    }
}
