# AleXutor — Windows Automation Suite

> AutoIt-equivalent automation tool built in C# / .NET 8 — compiles to a single standalone `.exe`.

---

## Features

| Module | Capabilities |
|---|---|
| 🖱 Mouse | Move, Left/Right/Middle Click, Double Click, Drag, Scroll |
| ⌨ Keyboard | Send text, AutoIt-style `{KEY}` tokens, Hotkeys |
| 🪟 Windows | Activate, Minimize, Maximize, Restore, Hide, Show, Close, Move/Resize |
| 📁 Files | Copy, Move, Delete, Read, Write, List, Size — files & folders |
| ⚙ Processes | List, Run, Kill by name |
| 📜 Script | Built-in script runner — `.axs` script files with AutoIt-style syntax |

---

## Getting the EXE

### Option 1 — GitHub Actions (automatic)

1. Push this repo to GitHub
2. GitHub Actions builds `AleXutor.exe` automatically
3. Download from **Actions → latest run → Artifacts → AleXutor-win-x64**

### Option 2 — Tagged Release

```bash
git tag v1.0.0
git push origin v1.0.0
```

GitHub will build and create a Release with `AleXutor.exe` attached for download.

### Option 3 — Build locally

```bash
dotnet publish AleXutor/AleXutor.csproj \
  --configuration Release \
  --runtime win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  --output ./publish
```

Requires: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

---

## Script Syntax (.axs)

```autoit
; Comments start with ;
; Variables
$name = "World"

; Timing
Sleep(1000)

; Mouse
MouseMove(400, 300, 10)
MouseClick("left", 400, 300, 1)
MouseClickDrag(100, 100, 400, 400)
MouseScroll("down", 400, 300, 3)

; Keyboard
Send("Hello {ENTER}")
Send("Hello World", 1)   ; mode 1 = raw (no special key parsing)
HotKey("CTRL", "C")

; Windows
WinActivate("Notepad")
WinMinimize("Notepad")
WinMaximize("Notepad")
WinRestore("Notepad")
WinHide("Notepad")
WinShow("Notepad")
WinClose("Notepad")
WinMove("Notepad", 100, 100, 800, 600)

; Processes
Run("notepad.exe")
Run("C:\Program Files\App\app.exe", "--flag")
ProcessClose("notepad")

; Files
FileWrite("C:\temp\test.txt", "Hello")
FileWriteLine("C:\temp\test.txt", "New line")
FileCopy("C:\src\file.txt", "C:\dst\file.txt")
FileMove("C:\src\file.txt", "C:\dst\file.txt")
FileDelete("C:\temp\test.txt")

; Directories
DirCreate("C:\temp\myfolder")
DirDelete("C:\temp\myfolder", 1)

; UI
MsgBox(0, "Title", "Message")
Echo("Log this message")
```

### Special Keys (use inside `{ }`)

`ENTER` `ESC` `TAB` `SPACE` `BACKSPACE` `DELETE` `HOME` `END` `PGUP` `PGDN`  
`UP` `DOWN` `LEFT` `RIGHT` `INSERT` `F1`–`F12`  
`LWIN` `CTRL` `ALT` `SHIFT` `CAPSLOCK` `NUMLOCK` `SCROLLLOCK` `PRINTSCREEN` `PAUSE`

Repeat a key: `{KEY n}` → e.g. `{DOWN 5}` presses Down 5 times.

---

## Requirements

- Windows 10 / 11 (x64)
- No install needed — single `.exe`, self-contained

---

## License

MIT
