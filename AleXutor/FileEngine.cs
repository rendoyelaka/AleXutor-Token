using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AleXutor
{
    public static class FileEngine
    {
        // ── File Operations ────────────────────────────────────────────────
        public static string Read(string path, Encoding? enc = null)
            => File.ReadAllText(path, enc ?? Encoding.UTF8);

        public static string[] ReadLines(string path)
            => File.ReadAllLines(path);

        public static void Write(string path, string content, bool append = false)
        {
            if (append) File.AppendAllText(path, content);
            else        File.WriteAllText(path, content);
        }

        public static void WriteLine(string path, string line)
            => File.AppendAllText(path, line + Environment.NewLine);

        public static void Copy(string src, string dst, bool overwrite = true)
            => File.Copy(src, dst, overwrite);

        public static void Move(string src, string dst)
            => File.Move(src, dst, true);

        public static void Delete(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }

        public static bool Exists(string path) => File.Exists(path);

        public static long GetSize(string path)
            => new FileInfo(path).Length;

        public static DateTime GetModified(string path)
            => File.GetLastWriteTime(path);

        public static void SetAttr(string path, FileAttributes attr)
            => File.SetAttributes(path, attr);

        public static FileAttributes GetAttr(string path)
            => File.GetAttributes(path);

        public static List<string> Find(string dir, string pattern = "*", bool recursive = false)
        {
            var opt = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            return Directory.GetFiles(dir, pattern, opt).ToList();
        }

        // ── Folder Operations ──────────────────────────────────────────────
        public static void CreateDir(string path)
            => Directory.CreateDirectory(path);

        public static void DeleteDir(string path, bool recursive = true)
        {
            if (Directory.Exists(path)) Directory.Delete(path, recursive);
        }

        public static void CopyDir(string src, string dst)
        {
            Directory.CreateDirectory(dst);
            foreach (var f in Directory.GetFiles(src))
                File.Copy(f, Path.Combine(dst, Path.GetFileName(f)), true);
            foreach (var d in Directory.GetDirectories(src))
                CopyDir(d, Path.Combine(dst, Path.GetFileName(d)));
        }

        public static void MoveDir(string src, string dst)
            => Directory.Move(src, dst);

        public static bool DirExists(string path) => Directory.Exists(path);

        public static List<string> ListDir(string path, string pattern = "*")
            => Directory.GetFileSystemEntries(path, pattern).ToList();

        public static List<string> ListFiles(string path, string pattern = "*", bool recursive = false)
        {
            var opt = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            return Directory.GetFiles(path, pattern, opt).ToList();
        }

        public static List<string> ListDirs(string path)
            => Directory.GetDirectories(path).ToList();

        public static long GetDirSize(string path)
        {
            long size = 0;
            foreach (var f in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            {
                try { size += new FileInfo(f).Length; } catch { }
            }
            return size;
        }

        public static string GetTempDir() => Path.GetTempPath();
        public static string GetCurrentDir() => Directory.GetCurrentDirectory();
        public static void SetCurrentDir(string path) => Directory.SetCurrentDirectory(path);

        // ── Path Helpers ───────────────────────────────────────────────────
        public static string GetFileName(string path) => Path.GetFileName(path);
        public static string GetDir(string path) => Path.GetDirectoryName(path) ?? "";
        public static string GetExt(string path) => Path.GetExtension(path);
        public static string GetNameNoExt(string path) => Path.GetFileNameWithoutExtension(path);
        public static string Combine(params string[] parts) => Path.Combine(parts);

        public static string FormatSize(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double size = bytes;
            int i = 0;
            while (size >= 1024 && i < units.Length - 1) { size /= 1024; i++; }
            return $"{size:F2} {units[i]}";
        }
    }
}
