using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AleXutor
{
    public partial class MainForm : Form
    {
        private TabControl tabs = null!;
        private RichTextBox logBox = null!;
        private ScriptEngine? scriptEngine;
        private Thread? scriptThread;

        // Tab panels
        private Panel mousePanel = null!, keyPanel = null!, winPanel = null!,
                       filePanel = null!, scriptPanel = null!, procPanel = null!;

        public MainForm()
        {
            Text = "AleXutor — Automation Suite";
            Size = new Size(900, 680);
            MinimumSize = new Size(800, 580);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(18, 18, 24);
            ForeColor = Color.FromArgb(220, 220, 240);
            Font = new Font("Segoe UI", 9.5f);
            Icon = SystemIcons.Application;

            BuildUI();
        }

        // ──────────────────────────────────────────────────────────────────
        // UI BUILDER
        // ──────────────────────────────────────────────────────────────────
        private void BuildUI()
        {
            // Header
            var header = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.FromArgb(30, 30, 42) };
            var title  = new Label
            {
                Text = "AleXutor",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.FromArgb(120, 180, 255),
                Dock = DockStyle.Left,
                AutoSize = false,
                Width = 180,
                TextAlign = ContentAlignment.MiddleCenter
            };
            var sub = new Label
            {
                Text = "Windows Automation Suite",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(140, 140, 160),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            header.Controls.Add(sub);
            header.Controls.Add(title);

            // Log box at bottom
            var logPanel = new Panel { Dock = DockStyle.Bottom, Height = 140, BackColor = Color.FromArgb(12, 12, 16) };
            var logLabel = new Label { Text = " Output Log", Dock = DockStyle.Top, Height = 22,
                ForeColor = Color.FromArgb(100, 160, 255), Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            logBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(10, 10, 14),
                ForeColor = Color.FromArgb(140, 255, 140),
                Font = new Font("Consolas", 9f),
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };
            var clearBtn = MakeButton("Clear Log", 80, 22);
            clearBtn.Dock = DockStyle.Bottom;
            clearBtn.Click += (_, _) => logBox.Clear();
            logPanel.Controls.Add(logBox);
            logPanel.Controls.Add(logLabel);
            logPanel.Controls.Add(clearBtn);

            // Tabs
            tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5f),
            };

            mousePanel  = BuildMouseTab();
            keyPanel    = BuildKeyboardTab();
            winPanel    = BuildWindowTab();
            filePanel   = BuildFileTab();
            procPanel   = BuildProcessTab();
            scriptPanel = BuildScriptTab();

            tabs.TabPages.Add(MakePage("🖱 Mouse",    mousePanel));
            tabs.TabPages.Add(MakePage("⌨ Keyboard",  keyPanel));
            tabs.TabPages.Add(MakePage("🪟 Windows",  winPanel));
            tabs.TabPages.Add(MakePage("📁 Files",    filePanel));
            tabs.TabPages.Add(MakePage("⚙ Processes", procPanel));
            tabs.TabPages.Add(MakePage("📜 Script",   scriptPanel));

            Controls.Add(tabs);
            Controls.Add(logPanel);
            Controls.Add(header);

            StyleTabs();
        }

        private TabPage MakePage(string title, Panel content)
        {
            var p = new TabPage(title)
            {
                BackColor = Color.FromArgb(22, 22, 32),
                ForeColor = Color.FromArgb(220, 220, 240),
                Padding = new Padding(8)
            };
            content.Dock = DockStyle.Fill;
            p.Controls.Add(content);
            return p;
        }

        private void StyleTabs()
        {
            tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabs.ItemSize = new Size(120, 32);
            tabs.DrawItem += (s, e) =>
            {
                bool sel = e.Index == tabs.SelectedIndex;
                using var bg = new SolidBrush(sel ? Color.FromArgb(40, 40, 60) : Color.FromArgb(22, 22, 32));
                e.Graphics.FillRectangle(bg, e.Bounds);
                using var fg = new SolidBrush(sel ? Color.FromArgb(120, 180, 255) : Color.FromArgb(160, 160, 180));
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                e.Graphics.DrawString(tabs.TabPages[e.Index].Text, Font, fg, e.Bounds, sf);
            };
        }

        // ──────────────────────────────────────────────────────────────────
        // MOUSE TAB
        // ──────────────────────────────────────────────────────────────────
        private Panel BuildMouseTab()
        {
            var p = new Panel { Padding = new Padding(12) };
            var layout = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, AutoSize = true, Padding = new Padding(8) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            // Current pos display
            var posLbl = MakeLabel("Cursor: X=0, Y=0");
            var posTimer = new System.Windows.Forms.Timer { Interval = 100, Enabled = true };
            posTimer.Tick += (_, _) => {
                var pos = MouseEngine.GetPos();
                posLbl.Text = $"Cursor: X={pos.X}, Y={pos.Y}";
            };

            var xBox = MakeNumBox(400); var yBox = MakeNumBox(300);
            var spdBox = MakeNumBox(10);

            layout.Controls.Add(MakeLabel("X:"), 0, 0); layout.Controls.Add(xBox, 1, 0);
            layout.Controls.Add(MakeLabel("Y:"), 0, 1); layout.Controls.Add(yBox, 1, 1);
            layout.Controls.Add(MakeLabel("Speed (steps):"), 0, 2); layout.Controls.Add(spdBox, 1, 2);

            var btnMove   = MakeButton("Move To", 120, 32);
            var btnLeft   = MakeButton("Left Click", 120, 32);
            var btnRight  = MakeButton("Right Click", 120, 32);
            var btnDouble = MakeButton("Double Click", 120, 32);
            var btnMiddle = MakeButton("Middle Click", 120, 32);
            var btnScrollU = MakeButton("Scroll Up", 120, 32);
            var btnScrollD = MakeButton("Scroll Down", 120, 32);

            btnMove.Click   += (_, _) => DoAsync(() => { MouseEngine.MoveSmooth((int)xBox.Value, (int)yBox.Value, (int)spdBox.Value); Log($"Moved to {xBox.Value},{yBox.Value}"); });
            btnLeft.Click   += (_, _) => DoAsync(() => { MouseEngine.LeftClick((int)xBox.Value, (int)yBox.Value); Log("Left click"); });
            btnRight.Click  += (_, _) => DoAsync(() => { MouseEngine.RightClick((int)xBox.Value, (int)yBox.Value); Log("Right click"); });
            btnDouble.Click += (_, _) => DoAsync(() => { MouseEngine.DoubleClick((int)xBox.Value, (int)yBox.Value); Log("Double click"); });
            btnMiddle.Click += (_, _) => DoAsync(() => { MouseEngine.MiddleClick((int)xBox.Value, (int)yBox.Value); Log("Middle click"); });
            btnScrollU.Click += (_, _) => DoAsync(() => { MouseEngine.ScrollUp((int)xBox.Value, (int)yBox.Value); Log("Scroll up"); });
            btnScrollD.Click += (_, _) => DoAsync(() => { MouseEngine.ScrollDown((int)xBox.Value, (int)yBox.Value); Log("Scroll down"); });

            // Drag section
            var dx1 = MakeNumBox(0); var dy1 = MakeNumBox(0);
            var dx2 = MakeNumBox(200); var dy2 = MakeNumBox(200);
            var btnDrag = MakeButton("Drag", 120, 32);
            btnDrag.Click += (_, _) => DoAsync(() => {
                MouseEngine.Drag((int)dx1.Value, (int)dy1.Value, (int)dx2.Value, (int)dy2.Value);
                Log($"Drag from {dx1.Value},{dy1.Value} to {dx2.Value},{dy2.Value}");
            });

            var dragGroup = MakeGroupBox("Drag");
            var dragLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2, AutoSize = true };
            dragLayout.Controls.Add(MakeLabel("From X:"), 0, 0); dragLayout.Controls.Add(dx1, 1, 0);
            dragLayout.Controls.Add(MakeLabel("From Y:"), 2, 0); dragLayout.Controls.Add(dy1, 3, 0);
            dragLayout.Controls.Add(MakeLabel("To X:"),   0, 1); dragLayout.Controls.Add(dx2, 1, 1);
            dragLayout.Controls.Add(MakeLabel("To Y:"),   2, 1); dragLayout.Controls.Add(dy2, 3, 1);
            dragGroup.Controls.Add(dragLayout);

            var flow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(8), FlowDirection = FlowDirection.LeftToRight, WrapContents = true };
            flow.Controls.AddRange(new Control[] { btnMove, btnLeft, btnRight, btnDouble, btnMiddle, btnScrollU, btnScrollD });

            p.Controls.Add(dragGroup);
            p.Controls.Add(btnDrag);
            p.Controls.Add(flow);
            p.Controls.Add(layout);
            p.Controls.Add(posLbl);
            ReflowChildren(p);
            return p;
        }

        // ──────────────────────────────────────────────────────────────────
        // KEYBOARD TAB
        // ──────────────────────────────────────────────────────────────────
        private Panel BuildKeyboardTab()
        {
            var p = new Panel { Padding = new Padding(12) };

            var txtBox = new TextBox { Width = 400, BackColor = Color.FromArgb(30, 30, 45), ForeColor = Color.White,
                Font = new Font("Consolas", 10f), BorderStyle = BorderStyle.FixedSingle,
                Text = "Hello{ENTER}World" };
            var delayBox = MakeNumBox(30); delayBox.Maximum = 5000;
            var btnSend  = MakeButton("Send Text (AutoIt Style)", 200, 32);
            var btnRaw   = MakeButton("Send Raw Text", 160, 32);

            btnSend.Click += (_, _) => DoAsync(() => { KeyboardEngine.SendAutoItStyle(txtBox.Text, (int)delayBox.Value); Log($"Sent: {txtBox.Text}"); });
            btnRaw.Click  += (_, _) => DoAsync(() => { KeyboardEngine.SendKeys(txtBox.Text, (int)delayBox.Value); Log($"Raw sent: {txtBox.Text}"); });

            // Single key
            var keyBox = new ComboBox { Width = 140, DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(30, 30, 45), ForeColor = Color.White };
            keyBox.Items.AddRange(new object[] { "ENTER","ESC","TAB","SPACE","BACKSPACE","DELETE",
                "F1","F2","F3","F4","F5","F6","F7","F8","F9","F10","F11","F12",
                "UP","DOWN","LEFT","RIGHT","HOME","END","PGUP","PGDN","INSERT",
                "LWIN","PRINTSCREEN","PAUSE","CAPSLOCK","NUMLOCK","SCROLLLOCK" });
            keyBox.SelectedIndex = 0;
            var btnKey = MakeButton("Send Key", 100, 32);
            btnKey.Click += (_, _) => DoAsync(() => { KeyboardEngine.SendKey(keyBox.Text); Log($"Key: {keyBox.Text}"); });

            // Hotkey
            var modBox = new ComboBox { Width = 100, DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(30, 30, 45), ForeColor = Color.White };
            modBox.Items.AddRange(new object[] { "CTRL", "ALT", "SHIFT", "WIN" });
            modBox.SelectedIndex = 0;
            var hotKeyBox = new TextBox { Width = 60, BackColor = Color.FromArgb(30, 30, 45), ForeColor = Color.White,
                Text = "C", BorderStyle = BorderStyle.FixedSingle };
            var btnHot = MakeButton("Send Hotkey", 120, 32);
            btnHot.Click += (_, _) => DoAsync(() => { KeyboardEngine.HotKey(modBox.Text, hotKeyBox.Text); Log($"Hotkey: {modBox.Text}+{hotKeyBox.Text}"); });

            var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
                AutoSize = true, Padding = new Padding(8), WrapContents = false };

            layout.Controls.Add(MakeLabel("Send String (use {KEY} for special keys):"));
            layout.Controls.Add(txtBox);
            layout.Controls.Add(MakeLabelRow("Delay between chars (ms):", delayBox));
            layout.Controls.Add(MakeRow(btnSend, btnRaw));
            layout.Controls.Add(MakeSep());
            layout.Controls.Add(MakeLabelRow("Single Key:", keyBox, btnKey));
            layout.Controls.Add(MakeSep());
            layout.Controls.Add(MakeLabel("Hotkey:"));
            layout.Controls.Add(MakeRow(modBox, MakeLabel("+"), hotKeyBox, btnHot));

            p.Controls.Add(layout);
            return p;
        }

        // ──────────────────────────────────────────────────────────────────
        // WINDOW TAB
        // ──────────────────────────────────────────────────────────────────
        private Panel BuildWindowTab()
        {
            var p = new Panel { Padding = new Padding(12) };

            var winList = new ListBox { Width = 360, Height = 200, BackColor = Color.FromArgb(25, 25, 38), ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9f) };
            var btnRefresh = MakeButton("↺ Refresh", 100, 28);
            btnRefresh.Click += (_, _) => {
                winList.Items.Clear();
                foreach (var t in WindowEngine.ListAll())
                    winList.Items.Add(t);
                Log($"Found {winList.Items.Count} windows");
            };

            var titleBox = new TextBox { Width = 300, BackColor = Color.FromArgb(30, 30, 45), ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle, PlaceholderText = "Window title..." };
            winList.SelectedIndexChanged += (_, _) => {
                if (winList.SelectedItem != null)
                    titleBox.Text = winList.SelectedItem.ToString();
            };

            var btnAct  = MakeButton("Activate",  100, 28);
            var btnMin  = MakeButton("Minimize",  100, 28);
            var btnMax  = MakeButton("Maximize",  100, 28);
            var btnRst  = MakeButton("Restore",   100, 28);
            var btnHide = MakeButton("Hide",       80, 28);
            var btnShow = MakeButton("Show",       80, 28);
            var btnCls  = MakeButton("Close",      80, 28);

            btnAct.Click  += (_, _) => { WindowEngine.Activate(titleBox.Text); Log($"Activated: {titleBox.Text}"); };
            btnMin.Click  += (_, _) => { WindowEngine.Minimize(titleBox.Text); Log($"Minimized: {titleBox.Text}"); };
            btnMax.Click  += (_, _) => { WindowEngine.Maximize(titleBox.Text); Log($"Maximized: {titleBox.Text}"); };
            btnRst.Click  += (_, _) => { WindowEngine.Restore(titleBox.Text);  Log($"Restored: {titleBox.Text}"); };
            btnHide.Click += (_, _) => { WindowEngine.Hide(titleBox.Text);     Log($"Hidden: {titleBox.Text}"); };
            btnShow.Click += (_, _) => { WindowEngine.Show(titleBox.Text);     Log($"Shown: {titleBox.Text}"); };
            btnCls.Click  += (_, _) => { if (MessageBox.Show($"Close '{titleBox.Text}'?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes) { WindowEngine.Close(titleBox.Text); Log($"Closed: {titleBox.Text}"); } };

            // Move/Resize
            var wx = MakeNumBox(100); var wy = MakeNumBox(100);
            var ww = MakeNumBox(800); var wh = MakeNumBox(600);
            var btnMove = MakeButton("Move / Resize", 140, 28);
            btnMove.Click += (_, _) => { WindowEngine.Move(titleBox.Text, (int)wx.Value, (int)wy.Value, (int)ww.Value, (int)wh.Value); Log($"Moved: {titleBox.Text}"); };

            // Active window label
            var activeLbl = MakeLabel("Active window: —");
            var activeTimer = new System.Windows.Forms.Timer { Interval = 500, Enabled = true };
            activeTimer.Tick += (_, _) => activeLbl.Text = $"Active: {WindowEngine.GetActiveTitle()}";

            var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
                AutoSize = true, Padding = new Padding(8), WrapContents = false };
            layout.Controls.Add(activeLbl);
            layout.Controls.Add(MakeRow(MakeLabel("Window list:"), btnRefresh));
            layout.Controls.Add(winList);
            layout.Controls.Add(MakeLabelRow("Title:", titleBox));
            layout.Controls.Add(MakeRow(btnAct, btnMin, btnMax, btnRst, btnHide, btnShow, btnCls));
            layout.Controls.Add(MakeSep());
            layout.Controls.Add(MakeLabel("Move / Resize:"));
            layout.Controls.Add(MakeRow(MakeLabel("X:"), wx, MakeLabel("Y:"), wy, MakeLabel("W:"), ww, MakeLabel("H:"), wh, btnMove));

            p.Controls.Add(layout);
            return p;
        }

        // ──────────────────────────────────────────────────────────────────
        // FILE TAB
        // ──────────────────────────────────────────────────────────────────
        private Panel BuildFileTab()
        {
            var p = new Panel { Padding = new Padding(12) };

            var srcBox  = MakePathBox("Source path...");
            var dstBox  = MakePathBox("Destination path...");
            var srcBrowse = MakeButton("…", 30, 26);
            var dstBrowse = MakeButton("…", 30, 26);
            srcBrowse.Click += (_, _) => BrowsePath(srcBox);
            dstBrowse.Click += (_, _) => BrowsePath(dstBox);

            var btnCopy = MakeButton("Copy File", 110, 28);
            var btnMove = MakeButton("Move File", 110, 28);
            var btnDel  = MakeButton("Delete File", 110, 28);
            var btnExist = MakeButton("File Exists?", 110, 28);
            var btnSize = MakeButton("File Size", 110, 28);

            btnCopy.Click  += (_, _) => { FileEngine.Copy(srcBox.Text, dstBox.Text); Log($"Copied: {srcBox.Text}"); };
            btnMove.Click  += (_, _) => { FileEngine.Move(srcBox.Text, dstBox.Text); Log($"Moved: {srcBox.Text}"); };
            btnDel.Click   += (_, _) => { if (Confirm($"Delete {srcBox.Text}?")) { FileEngine.Delete(srcBox.Text); Log($"Deleted: {srcBox.Text}"); } };
            btnExist.Click += (_, _) => Log($"Exists: {FileEngine.Exists(srcBox.Text)}");
            btnSize.Click  += (_, _) => { if (FileEngine.Exists(srcBox.Text)) Log($"Size: {FileEngine.FormatSize(FileEngine.GetSize(srcBox.Text))}"); };

            // Dir ops
            var dirBox = MakePathBox("Directory path...");
            var dirBrowse = MakeButton("…", 30, 26);
            dirBrowse.Click += (_, _) => BrowseDir(dirBox);
            var btnMkDir  = MakeButton("Create Dir",  110, 28);
            var btnRmDir  = MakeButton("Delete Dir",  110, 28);
            var btnLsDir  = MakeButton("List Dir",    110, 28);
            var btnDirSz  = MakeButton("Dir Size",    110, 28);

            btnMkDir.Click += (_, _) => { FileEngine.CreateDir(dirBox.Text); Log($"Created dir: {dirBox.Text}"); };
            btnRmDir.Click += (_, _) => { if (Confirm($"Delete dir {dirBox.Text}?")) { FileEngine.DeleteDir(dirBox.Text); Log($"Deleted dir: {dirBox.Text}"); } };
            btnLsDir.Click += (_, _) => {
                var items = FileEngine.ListDir(dirBox.Text);
                Log($"Listing {dirBox.Text} ({items.Count} items):");
                foreach (var item in items) Log("  " + item);
            };
            btnDirSz.Click += (_, _) => Log($"Dir size: {FileEngine.FormatSize(FileEngine.GetDirSize(dirBox.Text))}");

            // Read/Write
            var contentBox = new RichTextBox { Height = 80, Width = 500, BackColor = Color.FromArgb(25, 25, 38), ForeColor = Color.White,
                Font = new Font("Consolas", 9f), BorderStyle = BorderStyle.FixedSingle };
            var btnRead  = MakeButton("Read File", 110, 28);
            var btnWrite = MakeButton("Write File", 110, 28);
            btnRead.Click  += (_, _) => { if (FileEngine.Exists(srcBox.Text)) { contentBox.Text = FileEngine.Read(srcBox.Text); Log("File read."); } };
            btnWrite.Click += (_, _) => { FileEngine.Write(srcBox.Text, contentBox.Text); Log("File written."); };

            var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
                AutoSize = true, Padding = new Padding(8), WrapContents = false };
            layout.Controls.Add(MakeLabel("File Operations:"));
            layout.Controls.Add(MakeRow(srcBox, srcBrowse));
            layout.Controls.Add(MakeRow(MakeLabel("→"), dstBox, dstBrowse));
            layout.Controls.Add(MakeRow(btnCopy, btnMove, btnDel, btnExist, btnSize));
            layout.Controls.Add(MakeSep());
            layout.Controls.Add(MakeLabel("Directory Operations:"));
            layout.Controls.Add(MakeRow(dirBox, dirBrowse));
            layout.Controls.Add(MakeRow(btnMkDir, btnRmDir, btnLsDir, btnDirSz));
            layout.Controls.Add(MakeSep());
            layout.Controls.Add(MakeLabel("Read / Write Content:"));
            layout.Controls.Add(contentBox);
            layout.Controls.Add(MakeRow(btnRead, btnWrite));

            p.Controls.Add(layout);
            return p;
        }

        // ──────────────────────────────────────────────────────────────────
        // PROCESS TAB
        // ──────────────────────────────────────────────────────────────────
        private Panel BuildProcessTab()
        {
            var p = new Panel { Padding = new Padding(12) };

            var procList = new ListBox { Width = 460, Height = 200, BackColor = Color.FromArgb(25, 25, 38),
                ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Consolas", 8.5f) };
            var searchBox = new TextBox { Width = 200, BackColor = Color.FromArgb(30, 30, 45), ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle, PlaceholderText = "Filter..." };

            List<string> allProcs = new();
            Action refreshProcs = () => {
                allProcs = WindowEngine.ListProcesses();
                procList.Items.Clear();
                string filter = searchBox.Text.ToLower();
                foreach (var pr in allProcs)
                    if (string.IsNullOrEmpty(filter) || pr.ToLower().Contains(filter))
                        procList.Items.Add(pr);
                Log($"{allProcs.Count} processes");
            };
            searchBox.TextChanged += (_, _) => refreshProcs();

            var btnRefresh = MakeButton("↺ Refresh", 100, 28);
            btnRefresh.Click += (_, _) => refreshProcs();

            // Run program
            var runBox  = MakePathBox("Program path...");
            var runArgs = new TextBox { Width = 200, BackColor = Color.FromArgb(30, 30, 45), ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle, PlaceholderText = "Arguments (optional)" };
            var runBrowse = MakeButton("…", 30, 26);
            runBrowse.Click += (_, _) => BrowsePath(runBox, "Executables|*.exe|All Files|*.*");
            var btnRun  = MakeButton("▶ Run", 80, 28);
            btnRun.Click += (_, _) => { WindowEngine.Run(runBox.Text, runArgs.Text); Log($"Ran: {runBox.Text}"); };

            // Kill
            var killBox = new TextBox { Width = 200, BackColor = Color.FromArgb(30, 30, 45), ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle, PlaceholderText = "Process name (no .exe)" };
            procList.SelectedIndexChanged += (_, _) => {
                if (procList.SelectedItem != null)
                {
                    var txt = procList.SelectedItem.ToString()!;
                    var name = txt.Split(' ')[0];
                    killBox.Text = name;
                }
            };
            var btnKill = MakeButton("✕ Kill Process", 120, 28);
            btnKill.Click += (_, _) => {
                if (Confirm($"Kill {killBox.Text}?")) { WindowEngine.KillByName(killBox.Text); Log($"Killed: {killBox.Text}"); refreshProcs(); }
            };

            var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
                AutoSize = true, Padding = new Padding(8), WrapContents = false };
            layout.Controls.Add(MakeRow(MakeLabel("Running Processes:"), searchBox, btnRefresh));
            layout.Controls.Add(procList);
            layout.Controls.Add(MakeSep());
            layout.Controls.Add(MakeLabel("Run Program:"));
            layout.Controls.Add(MakeRow(runBox, runBrowse, runArgs, btnRun));
            layout.Controls.Add(MakeSep());
            layout.Controls.Add(MakeLabel("Kill Process:"));
            layout.Controls.Add(MakeRow(killBox, btnKill));

            p.Controls.Add(layout);
            refreshProcs();
            return p;
        }

        // ──────────────────────────────────────────────────────────────────
        // SCRIPT TAB
        // ──────────────────────────────────────────────────────────────────
        private Panel BuildScriptTab()
        {
            var p = new Panel { Padding = new Padding(12) };

            var scriptBox = new RichTextBox
            {
                Height = 320, Width = 700,
                BackColor = Color.FromArgb(16, 16, 24),
                ForeColor = Color.FromArgb(200, 230, 255),
                Font = new Font("Consolas", 10f),
                BorderStyle = BorderStyle.FixedSingle,
                AcceptsTab = true,
                ScrollBars = RichTextBoxScrollBars.Both,
                WordWrap = false,
                Text = @"; AleXutor Script Example
; Lines starting with ; are comments

; Move mouse and click
MouseMove(400, 300, 10)
Sleep(500)
MouseClick(""left"", 400, 300, 1)

; Type text
Sleep(300)
Send(""Hello from AleXutor!{ENTER}"")

; Open Notepad
Run(""notepad.exe"")
Sleep(1000)
WinActivate(""Untitled - Notepad"")
Send(""Automated by AleXutor{ENTER}"")

; File operation
FileWriteLine(""C:\temp\test.txt"", ""Hello world"")

MsgBox(0, ""Done"", ""Script complete!"")
"
            };

            var btnRun  = MakeButton("▶ Run Script", 130, 32);
            var btnStop = MakeButton("■ Stop", 90, 32);
            var btnOpen = MakeButton("Open File", 100, 32);
            var btnSave = MakeButton("Save File", 100, 32);
            var btnClear = MakeButton("Clear", 80, 32);

            btnRun.BackColor = Color.FromArgb(40, 120, 60);
            btnStop.BackColor = Color.FromArgb(120, 40, 40);

            btnRun.Click += (_, _) =>
            {
                if (scriptThread?.IsAlive == true) { Log("Script already running."); return; }
                scriptEngine = new ScriptEngine(Log);
                string code = scriptBox.Text;
                scriptThread = new Thread(() =>
                {
                    Log("=== Script started ===");
                    scriptEngine.RunScript(code);
                    Log("=== Script finished ===");
                }) { IsBackground = true };
                scriptThread.Start();
            };

            btnStop.Click += (_, _) =>
            {
                scriptEngine?.Abort();
                Log("=== Script aborted ===");
            };

            btnOpen.Click += (_, _) =>
            {
                var dlg = new OpenFileDialog { Filter = "AleXutor Scripts|*.axs;*.txt|All Files|*.*" };
                if (dlg.ShowDialog() == DialogResult.OK)
                    scriptBox.Text = File.ReadAllText(dlg.FileName);
            };

            btnSave.Click += (_, _) =>
            {
                var dlg = new SaveFileDialog { Filter = "AleXutor Script|*.axs|Text File|*.txt", DefaultExt = "axs" };
                if (dlg.ShowDialog() == DialogResult.OK)
                    File.WriteAllText(dlg.FileName, scriptBox.Text);
            };

            btnClear.Click += (_, _) => scriptBox.Clear();

            var hint = MakeLabel("Commands: MouseMove, MouseClick, MouseClickDrag, Send, HotKey, WinActivate, WinClose, WinMinimize, WinMaximize, WinMove, Run, ProcessClose, FileWrite, FileCopy, FileDelete, DirCreate, MsgBox, Sleep");
            hint.AutoSize = false;
            hint.Width = 700;
            hint.Height = 36;
            hint.ForeColor = Color.FromArgb(100, 140, 180);

            var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
                AutoSize = true, Padding = new Padding(8), WrapContents = false };
            layout.Controls.Add(scriptBox);
            layout.Controls.Add(MakeRow(btnRun, btnStop, btnOpen, btnSave, btnClear));
            layout.Controls.Add(hint);

            p.Controls.Add(layout);
            return p;
        }

        // ──────────────────────────────────────────────────────────────────
        // HELPERS
        // ──────────────────────────────────────────────────────────────────
        private void Log(string msg)
        {
            if (logBox.InvokeRequired)
                logBox.Invoke(() => Log(msg));
            else
            {
                logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}");
                logBox.ScrollToCaret();
            }
        }

        private void DoAsync(Action action) => Task.Run(action);

        private bool Confirm(string msg) => MessageBox.Show(msg, "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;

        private Label MakeLabel(string text)
        {
            return new Label { Text = text, AutoSize = true, ForeColor = Color.FromArgb(180, 180, 200),
                Margin = new Padding(4, 6, 4, 2) };
        }

        private Button MakeButton(string text, int w, int h)
        {
            return new Button
            {
                Text = text, Width = w, Height = h,
                BackColor = Color.FromArgb(40, 60, 100),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(4, 4, 4, 4)
            };
        }

        private NumericUpDown MakeNumBox(int val)
        {
            return new NumericUpDown
            {
                Width = 80, Value = val, Minimum = -9999, Maximum = 9999,
                BackColor = Color.FromArgb(30, 30, 45), ForeColor = Color.White,
                Margin = new Padding(2, 4, 2, 4)
            };
        }

        private TextBox MakePathBox(string placeholder)
        {
            return new TextBox
            {
                Width = 320, BackColor = Color.FromArgb(30, 30, 45), ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle, PlaceholderText = placeholder,
                Margin = new Padding(2, 4, 2, 4)
            };
        }

        private GroupBox MakeGroupBox(string text)
        {
            return new GroupBox
            {
                Text = text, ForeColor = Color.FromArgb(120, 160, 220),
                AutoSize = true, Margin = new Padding(4, 8, 4, 4)
            };
        }

        private Panel MakeSep()
        {
            return new Panel { Height = 1, Width = 700, BackColor = Color.FromArgb(50, 50, 70), Margin = new Padding(0, 8, 0, 8) };
        }

        private FlowLayoutPanel MakeRow(params Control[] controls)
        {
            var row = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 4, 0, 4) };
            row.Controls.AddRange(controls);
            return row;
        }

        private FlowLayoutPanel MakeLabelRow(string label, params Control[] controls)
        {
            var row = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 4, 0, 4) };
            row.Controls.Add(MakeLabel(label));
            row.Controls.AddRange(controls);
            return row;
        }

        private void BrowsePath(TextBox box, string filter = "All Files|*.*")
        {
            var dlg = new OpenFileDialog { Filter = filter };
            if (dlg.ShowDialog() == DialogResult.OK) box.Text = dlg.FileName;
        }

        private void BrowseDir(TextBox box)
        {
            var dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() == DialogResult.OK) box.Text = dlg.SelectedPath;
        }

        private static void ReflowChildren(Panel p)
        {
            // Reverse controls so they stack top→down naturally
            var ctrls = new List<Control>();
            foreach (Control c in p.Controls) ctrls.Add(c);
            ctrls.Reverse();
            p.Controls.Clear();
            p.Controls.AddRange(ctrls.ToArray());
        }
    }
}
