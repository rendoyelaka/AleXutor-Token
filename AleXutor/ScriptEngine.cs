using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace AleXutor
{
    /// <summary>
    /// Lightweight AutoIt-style script interpreter.
    /// Supports: Sleep, MouseMove, MouseClick, MouseDrag, Send, WinActivate,
    /// WinClose, WinMin, WinMax, Run, FileWrite, FileRead, FileDelete,
    /// DirCreate, DirDelete, MsgBox, Echo, #comment
    /// </summary>
    public class ScriptEngine
    {
        private readonly Action<string> _log;
        private bool _abort;
        private readonly Dictionary<string, string> _vars = new(StringComparer.OrdinalIgnoreCase);

        public ScriptEngine(Action<string> logCallback) => _log = logCallback;

        public void Abort() => _abort = true;

        public void RunScript(string script)
        {
            _abort = false;
            _vars.Clear();

            var lines = script.Split('\n');
            int i = 0;
            while (i < lines.Length)
            {
                if (_abort) break;
                string raw = lines[i].Trim();
                i++;
                if (string.IsNullOrWhiteSpace(raw) || raw.StartsWith(';') || raw.StartsWith('#'))
                    continue;

                raw = SubstituteVars(raw);
                ExecuteLine(raw);
            }
        }

        private void ExecuteLine(string line)
        {
            try
            {
                // Variable assignment:  $var = value
                var varAssign = Regex.Match(line, @"^\$(\w+)\s*=\s*(.+)$");
                if (varAssign.Success)
                {
                    _vars[varAssign.Groups[1].Value] = varAssign.Groups[2].Value.Trim().Trim('"');
                    return;
                }

                var (cmd, args) = ParseCommand(line);

                switch (cmd.ToUpper())
                {
                    // ── Timing ─────────────────────────────────────────────
                    case "SLEEP":
                        Thread.Sleep(int.Parse(args[0]));
                        break;

                    // ── Mouse ──────────────────────────────────────────────
                    case "MOUSEMOVE":
                        int mx = int.Parse(args[0]), my = int.Parse(args[1]);
                        int mspeed = args.Count > 2 ? int.Parse(args[2]) : 10;
                        if (mspeed > 0) MouseEngine.MoveSmooth(mx, my, mspeed);
                        else MouseEngine.MoveTo(mx, my);
                        _log($"MouseMove({mx}, {my})");
                        break;

                    case "MOUSECLICK":
                        string btn   = args.Count > 0 ? args[0] : "left";
                        int cx = args.Count > 1 ? int.Parse(args[1]) : MouseEngine.GetPos().X;
                        int cy = args.Count > 2 ? int.Parse(args[2]) : MouseEngine.GetPos().Y;
                        int clicks   = args.Count > 3 ? int.Parse(args[3]) : 1;
                        for (int n = 0; n < clicks; n++)
                        {
                            switch (btn.ToLower())
                            {
                                case "left":   MouseEngine.LeftClick(cx, cy);   break;
                                case "right":  MouseEngine.RightClick(cx, cy);  break;
                                case "middle": MouseEngine.MiddleClick(cx, cy); break;
                            }
                            Thread.Sleep(50);
                        }
                        _log($"MouseClick({btn}, {cx}, {cy} x{clicks})");
                        break;

                    case "MOUSECLICKDRAG":
                        MouseEngine.Drag(int.Parse(args[0]), int.Parse(args[1]),
                                         int.Parse(args[2]), int.Parse(args[3]));
                        _log($"MouseDrag({args[0]},{args[1]} → {args[2]},{args[3]})");
                        break;

                    case "MOUSESCROLL":
                        string dir   = args[0].ToLower();
                        int scx = int.Parse(args[1]), scy = int.Parse(args[2]);
                        int amount   = args.Count > 3 ? int.Parse(args[3]) : 3;
                        if (dir == "up") MouseEngine.ScrollUp(scx, scy, amount);
                        else MouseEngine.ScrollDown(scx, scy, amount);
                        _log($"MouseScroll({dir})");
                        break;

                    // ── Keyboard ───────────────────────────────────────────
                    case "SEND":
                        string txt = args[0];
                        int mode   = args.Count > 1 ? int.Parse(args[1]) : 0;
                        if (mode == 1)
                            KeyboardEngine.SendKeys(txt);
                        else
                            KeyboardEngine.SendAutoItStyle(txt);
                        _log($"Send(\"{txt}\")");
                        break;

                    case "HOTKEY":
                        KeyboardEngine.HotKey(args[0], args[1]);
                        _log($"HotKey({args[0]}+{args[1]})");
                        break;

                    // ── Window ─────────────────────────────────────────────
                    case "WINACTIVATE":
                        WindowEngine.Activate(args[0]);
                        _log($"WinActivate(\"{args[0]}\")");
                        break;

                    case "WINCLOSE":
                        WindowEngine.Close(args[0]);
                        _log($"WinClose(\"{args[0]}\")");
                        break;

                    case "WINHIDE":
                        WindowEngine.Hide(args[0]);
                        _log($"WinHide(\"{args[0]}\")");
                        break;

                    case "WINSHOW":
                        WindowEngine.Show(args[0]);
                        _log($"WinShow(\"{args[0]}\")");
                        break;

                    case "WINMINIMIZE":
                        WindowEngine.Minimize(args[0]);
                        _log($"WinMinimize(\"{args[0]}\")");
                        break;

                    case "WINMAXIMIZE":
                        WindowEngine.Maximize(args[0]);
                        _log($"WinMaximize(\"{args[0]}\")");
                        break;

                    case "WINRESTORE":
                        WindowEngine.Restore(args[0]);
                        _log($"WinRestore(\"{args[0]}\")");
                        break;

                    case "WINMOVE":
                        WindowEngine.Move(args[0],
                            int.Parse(args[1]), int.Parse(args[2]),
                            int.Parse(args[3]), int.Parse(args[4]));
                        _log($"WinMove(\"{args[0]}\")");
                        break;

                    // ── Process ────────────────────────────────────────────
                    case "RUN":
                        string runPath = args[0];
                        string runArgs = args.Count > 1 ? args[1] : "";
                        WindowEngine.Run(runPath, runArgs);
                        _log($"Run(\"{runPath}\")");
                        break;

                    case "PROCESSCLOSE":
                        WindowEngine.KillByName(args[0]);
                        _log($"ProcessClose(\"{args[0]}\")");
                        break;

                    // ── File ───────────────────────────────────────────────
                    case "FILEWRITE":
                        FileEngine.Write(args[0], args.Count > 1 ? args[1] : "");
                        _log($"FileWrite(\"{args[0]}\")");
                        break;

                    case "FILEWRITELINE":
                        FileEngine.WriteLine(args[0], args.Count > 1 ? args[1] : "");
                        _log($"FileWriteLine(\"{args[0]}\")");
                        break;

                    case "FILECOPY":
                        FileEngine.Copy(args[0], args[1]);
                        _log($"FileCopy(\"{args[0]}\" → \"{args[1]}\")");
                        break;

                    case "FILEMOVE":
                        FileEngine.Move(args[0], args[1]);
                        _log($"FileMove(\"{args[0]}\" → \"{args[1]}\")");
                        break;

                    case "FILEDELETE":
                        FileEngine.Delete(args[0]);
                        _log($"FileDelete(\"{args[0]}\")");
                        break;

                    case "DIRCREATE":
                        FileEngine.CreateDir(args[0]);
                        _log($"DirCreate(\"{args[0]}\")");
                        break;

                    case "DIRDELETE":
                        FileEngine.DeleteDir(args[0], args.Count < 2 || args[1] == "1");
                        _log($"DirDelete(\"{args[0]}\")");
                        break;

                    // ── UI ─────────────────────────────────────────────────
                    case "MSGBOX":
                        string mbText  = args.Count > 1 ? args[1] : args[0];
                        string mbTitle = args.Count > 2 ? args[2] : "AleXutor";
                        System.Windows.Forms.MessageBox.Show(mbText, mbTitle);
                        _log($"MsgBox(\"{mbText}\")");
                        break;

                    case "ECHO":
                    case "CONSOLEWRITE":
                        _log(args.Count > 0 ? args[0] : "");
                        break;

                    default:
                        _log($"[Unknown] {line}");
                        break;
                }
            }
            catch (Exception ex)
            {
                _log($"[Error] {line} → {ex.Message}");
            }
        }

        private string SubstituteVars(string line)
        {
            foreach (var kv in _vars)
                line = line.Replace($"${kv.Key}", kv.Value, StringComparison.OrdinalIgnoreCase);
            return line;
        }

        private static (string cmd, List<string> args) ParseCommand(string line)
        {
            // Match: CMD("arg1", "arg2", ...)  or  CMD(arg1, arg2, ...)
            var m = Regex.Match(line, @"^(\w+)\s*\((.*)?\)\s*$", RegexOptions.Singleline);
            if (!m.Success) return (line.Trim(), new List<string>());

            string cmd  = m.Groups[1].Value;
            string raw  = m.Groups[2].Value.Trim();
            var argList = new List<string>();

            if (!string.IsNullOrEmpty(raw))
            {
                // Split by comma, respecting quoted strings
                var args = Regex.Split(raw, @",(?=(?:[^""]*""[^""]*"")*[^""]*$)");
                foreach (var a in args)
                    argList.Add(a.Trim().Trim('"'));
            }

            return (cmd, argList);
        }
    }
}
