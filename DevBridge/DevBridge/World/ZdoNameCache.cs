using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using BepInEx;

namespace DevBridge.World
{
    /// <summary>One scanned file's string literals, with the size and write time of the file they were read from.</summary>
    internal sealed class FileNames
    {
        internal string Path;
        internal long Size;
        internal long Written;
        internal List<string> Texts;
    }

    /// <summary>
    /// The string literals ZdoNames scanned from each file, kept on disk (BepInEx/cache/DevBridge.zdonames) by path,
    /// size and write time, so a file unchanged since the last scan is not read with Cecil again. A cache that cannot be
    /// read (missing, from another DevBridge version, cut short) is ignored: every file is scanned and it is written again.
    /// </summary>
    internal static class ZdoNameCache
    {
        private const int Format = 1;

        private static string FilePath => System.IO.Path.Combine(Paths.CachePath, "DevBridge.zdonames");

        /// <summary>Each file's literals, from the cache while the file is unchanged, else from scan (null: unreadable, left out).</summary>
        internal static List<FileNames> For(IEnumerable<string> paths, Func<string, List<string>> scan)
        {
            Dictionary<string, FileNames> cached = Load();
            var files = new List<FileNames>();
            bool scanned = false;
            foreach (string path in paths)
            {
                var info = new FileInfo(path);
                if (!info.Exists) continue;
                if (cached.TryGetValue(path, out FileNames known) && known.Size == info.Length && known.Written == info.LastWriteTimeUtc.Ticks)
                {
                    files.Add(known);
                    continue;
                }
                scanned = true;
                List<string> texts = scan(path);
                if (texts != null) files.Add(new FileNames { Path = path, Size = info.Length, Written = info.LastWriteTimeUtc.Ticks, Texts = texts });
            }
            if (scanned || files.Count != cached.Count) Save(files);
            return files;
        }

        private static Dictionary<string, FileNames> Load()
        {
            var cached = new Dictionary<string, FileNames>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (!File.Exists(FilePath)) return cached;
                using (var reader = new BinaryReader(File.OpenRead(FilePath)))
                {
                    if (reader.ReadInt32() != Format) return cached;
                    for (int count = reader.ReadInt32(); count > 0; count--)
                    {
                        FileNames file = Read(reader);
                        cached[file.Path] = file;
                    }
                }
            }
            catch (Exception)
            {
                cached.Clear(); // unreadable: every file is scanned again
            }
            return cached;
        }

        private static FileNames Read(BinaryReader reader)
        {
            var file = new FileNames { Path = reader.ReadString(), Size = reader.ReadInt64(), Written = reader.ReadInt64() };
            int count = reader.ReadInt32();
            file.Texts = new List<string>(count);
            for (int i = 0; i < count; i++) file.Texts.Add(reader.ReadString());
            return file;
        }

        // Written beside it under this process's own name, then moved over it, so a second game on this machine reading
        // or writing at the same moment never meets half a file.
        private static void Save(List<FileNames> files)
        {
            string temp = FilePath + "." + Process.GetCurrentProcess().Id + ".tmp";
            try
            {
                Directory.CreateDirectory(Paths.CachePath);
                using (var writer = new BinaryWriter(File.Create(temp))) Write(writer, files);
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(temp, FilePath);
            }
            catch (Exception error)
            {
                UnityEngine.Debug.LogWarning($"DevBridge could not save the ZDO key names to {FilePath}: {error.Message}");
                Discard(temp);
            }
        }

        private static void Write(BinaryWriter writer, List<FileNames> files)
        {
            writer.Write(Format);
            writer.Write(files.Count);
            foreach (FileNames file in files)
            {
                writer.Write(file.Path);
                writer.Write(file.Size);
                writer.Write(file.Written);
                writer.Write(file.Texts.Count);
                foreach (string text in file.Texts) writer.Write(text);
            }
        }

        private static void Discard(string temp)
        {
            try
            {
                File.Delete(temp);
            }
            catch (Exception)
            {
                // left for the next save to overwrite
            }
        }
    }
}
