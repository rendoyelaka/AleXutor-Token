using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AleXutor
{
    /// <summary>
    /// Learns a token format from a sample file.
    /// The user marks which part is the phone number and which is the message.
    /// Once learned, it parses any clipboard text using the same pattern.
    /// </summary>
    public class FormatEngine
    {
        private static readonly string ConfigPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                         "AleXutor", "format.json");

        public class FormatConfig
        {
            public string SampleText      { get; set; } = "";
            public string NumberPattern   { get; set; } = "";
            public string MessagePattern  { get; set; } = "";
            public string NumberTag       { get; set; } = ""; // literal text user marked as number
            public string MessageTag      { get; set; } = ""; // literal text user marked as message
            public string Delimiter       { get; set; } = "\n";
            public int    NumberLineIndex { get; set; } = 0;
            public int    MessageLineIndex{ get; set; } = 1;
            public bool   IsLearned       { get; set; } = false;
        }

        public FormatConfig Config { get; private set; } = new();

        public FormatEngine()
        {
            Load();
        }

        // ── Save / Load ───────────────────────────────────────────────────

        public void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(Config, new JsonSerializerOptions { WriteIndented = true }));
        }

        public void Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                    Config = JsonSerializer.Deserialize<FormatConfig>(File.ReadAllText(ConfigPath)) ?? new FormatConfig();
            }
            catch { Config = new FormatConfig(); }
        }

        // ── Learn from sample ─────────────────────────────────────────────

        public void LearnFromSample(string sampleText, string markedNumber, string markedMessage)
        {
            Config.SampleText       = sampleText;
            Config.NumberTag        = markedNumber.Trim();
            Config.MessageTag       = markedMessage.Trim();

            // Detect delimiter
            if (sampleText.Contains("\r\n")) Config.Delimiter = "\r\n";
            else if (sampleText.Contains("\n")) Config.Delimiter = "\n";
            else Config.Delimiter = "\n";

            // Find which line the number is on and which is the message
            var lines = sampleText.Split(new[] { Config.Delimiter }, StringSplitOptions.None);
            Config.NumberLineIndex  = -1;
            Config.MessageLineIndex = -1;

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains(markedNumber.Trim()))
                    Config.NumberLineIndex = i;
                if (lines[i].Contains(markedMessage.Trim()))
                    Config.MessageLineIndex = i;
            }

            // Build regex patterns — escape everything around the marked values
            if (Config.NumberLineIndex >= 0)
            {
                string line = lines[Config.NumberLineIndex];
                int idx = line.IndexOf(markedNumber.Trim());
                string prefix = Regex.Escape(line.Substring(0, idx).Trim());
                Config.NumberPattern = string.IsNullOrEmpty(prefix)
                    ? @"(.+)"
                    : prefix + @"\s*(.+)";
            }

            if (Config.MessageLineIndex >= 0)
            {
                string line = lines[Config.MessageLineIndex];
                int idx = line.IndexOf(markedMessage.Trim());
                string prefix = Regex.Escape(line.Substring(0, idx).Trim());
                Config.MessagePattern = string.IsNullOrEmpty(prefix)
                    ? @"(.+)"
                    : prefix + @"\s*(.+)";
            }

            Config.IsLearned = true;
            Save();
        }

        // ── Parse clipboard text ──────────────────────────────────────────

        public (string number, string message, bool success) Parse(string clipboardText)
        {
            if (!Config.IsLearned || string.IsNullOrWhiteSpace(clipboardText))
                return ("", "", false);

            try
            {
                var lines = clipboardText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

                string number  = "";
                string message = "";

                // Extract number
                if (Config.NumberLineIndex >= 0 && Config.NumberLineIndex < lines.Length)
                {
                    string line = lines[Config.NumberLineIndex];
                    if (!string.IsNullOrEmpty(Config.NumberPattern))
                    {
                        var m = Regex.Match(line, Config.NumberPattern);
                        number = m.Success ? m.Groups[1].Value.Trim() : line.Trim();
                    }
                    else number = line.Trim();
                }

                // Extract message — could be multi-line from MessageLineIndex onwards
                if (Config.MessageLineIndex >= 0 && Config.MessageLineIndex < lines.Length)
                {
                    if (!string.IsNullOrEmpty(Config.MessagePattern))
                    {
                        string line = lines[Config.MessageLineIndex];
                        var m = Regex.Match(line, Config.MessagePattern);
                        string firstPart = m.Success ? m.Groups[1].Value.Trim() : line.Trim();

                        // Grab remaining lines as part of message too
                        var msgLines = new List<string> { firstPart };
                        for (int i = Config.MessageLineIndex + 1; i < lines.Length; i++)
                            if (!string.IsNullOrWhiteSpace(lines[i]))
                                msgLines.Add(lines[i].Trim());

                        message = string.Join("\n", msgLines);
                    }
                    else message = lines[Config.MessageLineIndex].Trim();
                }

                bool valid = !string.IsNullOrEmpty(number) && !string.IsNullOrEmpty(message);
                return (number, message, valid);
            }
            catch
            {
                return ("", "", false);
            }
        }

        // ── Quick phone number validator ──────────────────────────────────
        public static bool LooksLikePhoneNumber(string s)
        {
            // Must contain at least 6 digits, allow +, spaces, dashes
            return Regex.IsMatch(s.Trim(), @"^[\+\d\s\-\(\)]{6,20}$");
        }
    }
}
