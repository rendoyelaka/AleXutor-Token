using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

namespace AleXutor
{
    public static class KeyboardEngine
    {
        private static readonly Dictionary<string, byte> SpecialKeys = new()
        {
            { "ENTER",     0x0D }, { "ESC",       0x1B }, { "TAB",       0x09 },
            { "SPACE",     0x20 }, { "BACKSPACE",  0x08 }, { "DELETE",    0x2E },
            { "HOME",      0x24 }, { "END",        0x23 }, { "PGUP",      0x21 },
            { "PGDN",      0x22 }, { "UP",         0x26 }, { "DOWN",      0x28 },
            { "LEFT",      0x25 }, { "RIGHT",      0x27 }, { "INSERT",    0x2D },
            { "F1",        0x70 }, { "F2",         0x71 }, { "F3",        0x72 },
            { "F4",        0x73 }, { "F5",         0x74 }, { "F6",        0x75 },
            { "F7",        0x76 }, { "F8",         0x77 }, { "F9",        0x78 },
            { "F10",       0x79 }, { "F11",        0x7A }, { "F12",       0x7B },
            { "LWIN",      0x5B }, { "RWIN",       0x5C }, { "CTRL",      0x11 },
            { "ALT",       0x12 }, { "SHIFT",      0x10 }, { "CAPSLOCK",  0x14 },
            { "NUMLOCK",   0x90 }, { "SCROLLLOCK", 0x91 }, { "PRINTSCREEN", 0x2C },
            { "PAUSE",     0x13 },
        };

        public static void SendKey(string key)
        {
            key = key.ToUpper();
            if (SpecialKeys.TryGetValue(key, out byte vk))
            {
                WinAPI.keybd_event(vk, 0, 0, IntPtr.Zero);
                Thread.Sleep(30);
                WinAPI.keybd_event(vk, 0, WinAPI.KEYEVENTF_KEYUP, IntPtr.Zero);
            }
            else if (key.Length == 1)
            {
                SendChar(key[0]);
            }
        }

        public static void SendChar(char c)
        {
            short vkShift = WinAPI.VkKeyScan(c);
            byte vk     = (byte)(vkShift & 0xFF);
            bool shift  = (vkShift & 0x100) != 0;

            if (shift) WinAPI.keybd_event(0x10, 0, 0, IntPtr.Zero);
            WinAPI.keybd_event(vk, 0, 0, IntPtr.Zero);
            Thread.Sleep(10);
            WinAPI.keybd_event(vk, 0, WinAPI.KEYEVENTF_KEYUP, IntPtr.Zero);
            if (shift) WinAPI.keybd_event(0x10, 0, WinAPI.KEYEVENTF_KEYUP, IntPtr.Zero);
        }

        public static void SendKeys(string text, int delayMs = 30)
        {
            foreach (char c in text)
            {
                SendChar(c);
                Thread.Sleep(delayMs);
            }
        }

        /// <summary>
        /// Parses AutoIt-style send string e.g. "Hello{ENTER}World{F5}"
        /// </summary>
        public static void SendAutoItStyle(string text, int delayMs = 30)
        {
            int i = 0;
            while (i < text.Length)
            {
                if (text[i] == '{')
                {
                    int end = text.IndexOf('}', i);
                    if (end > i)
                    {
                        string tag = text.Substring(i + 1, end - i - 1).ToUpper();
                        // Handle repeat: {KEY n}
                        int repeat = 1;
                        var parts = tag.Split(' ');
                        if (parts.Length == 2 && int.TryParse(parts[1], out int r))
                        {
                            tag = parts[0];
                            repeat = r;
                        }
                        for (int j = 0; j < repeat; j++)
                            SendKey(tag);
                        i = end + 1;
                        continue;
                    }
                }
                SendChar(text[i]);
                Thread.Sleep(delayMs);
                i++;
            }
        }

        public static void HotKey(string modifier, string key)
        {
            modifier = modifier.ToUpper();
            byte modVk = modifier switch
            {
                "CTRL"  => 0x11,
                "ALT"   => 0x12,
                "SHIFT" => 0x10,
                "WIN"   => 0x5B,
                _       => 0x11
            };

            WinAPI.keybd_event(modVk, 0, 0, IntPtr.Zero);
            SendKey(key);
            WinAPI.keybd_event(modVk, 0, WinAPI.KEYEVENTF_KEYUP, IntPtr.Zero);
        }
    }
}
