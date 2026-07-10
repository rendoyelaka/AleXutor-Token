using System;
using System.Threading;
using System.Windows.Forms;

namespace AleXutor
{
    /// <summary>
    /// Watches the clipboard silently.
    /// When content changes and matches the learned format,
    /// fires OnTokenDetected with parsed number + message.
    /// </summary>
    public class ClipboardWatcher
    {
        private readonly FormatEngine _format;
        private readonly Action<string> _log;
        private System.Windows.Forms.Timer? _timer;
        private string _lastClip = "";
        private bool _enabled = false;

        public event Action<string, string>? OnTokenDetected; // (number, message)

        public bool IsRunning => _enabled;

        public ClipboardWatcher(FormatEngine format, Action<string> log)
        {
            _format = format;
            _log    = log;
        }

        public void Start()
        {
            if (_enabled) return;
            _enabled = true;

            _timer = new System.Windows.Forms.Timer { Interval = 500 };
            _timer.Tick += Check;
            _timer.Start();
            _log("[Watcher] Clipboard watcher started.");
        }

        public void Stop()
        {
            _enabled = false;
            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;
            _log("[Watcher] Clipboard watcher stopped.");
        }

        private void Check(object? sender, EventArgs e)
        {
            if (!_enabled || !_format.Config.IsLearned) return;

            try
            {
                string clip = Clipboard.GetText();
                if (string.IsNullOrWhiteSpace(clip) || clip == _lastClip) return;
                _lastClip = clip;

                var (number, message, success) = _format.Parse(clip);
                if (!success) return;

                _log($"[Watcher] Token detected → Number: {number}");
                OnTokenDetected?.Invoke(number, message);
            }
            catch { }
        }
    }
}
