using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BepInEx.Configuration;
using DevBridge.Capture;
using DevBridge.Server;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DevBridge.Studio
{
    /// <summary>
    /// The studio's screenshots: PNGs of the game window, UI included, named by the time taken ("2026-10-24 20-31-05.png"),
    /// in the Screenshots folder under [Studio], with a small JPG of each in its .thumbs folder for the Screenshots tab.
    /// The tab lists, renames and deletes them (to the Recycle Bin); any PNG or JPG put in the folder shows too.
    /// </summary>
    internal static class StudioShots
    {
        private const int ThumbWidth = 360;
        private static readonly string[] Kinds = { ".png", ".jpg", ".jpeg" };
        private static readonly HashSet<string> Pending = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static ConfigEntry<string> folder;

        internal static void Bind(ConfigFile config) =>
            folder = config.Bind("Studio", "Screenshots folder", "",
                "Where the studio's Screenshot button saves (and its Screenshots tab lists); empty for <your Pictures folder>\\DevBridge.");

        internal static string Folder
        {
            get
            {
                string chosen = folder == null || string.IsNullOrWhiteSpace(folder.Value)
                    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "DevBridge")
                    : Environment.ExpandEnvironmentVariables(folder.Value.Trim());
                return Path.GetFullPath(chosen);
            }
        }

        private static string Thumbs => Path.Combine(Folder, ".thumbs");

        internal static Dictionary<string, object> List()
        {
            Directory.CreateDirectory(Folder);
            List<FileInfo> files = new DirectoryInfo(Folder).GetFiles().Where(IsShot).OrderByDescending(f => f.LastWriteTime).ToList();
            return new Dictionary<string, object>
            {
                ["folder"] = Folder,
                ["shots"] = files.Select(Describe).ToList(),
            };
        }

        internal static Dictionary<string, object> Describe(FileInfo file) => new Dictionary<string, object>
        {
            ["name"] = file.Name,
            ["bytes"] = file.Length,
            ["taken"] = file.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"),
        };

        /// <summary>A new file named by the time, "(2)" and on when that second has one already; held until Done.</summary>
        internal static string NewPath()
        {
            Directory.CreateDirectory(Folder);
            string stamp = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss"), path = Path.Combine(Folder, stamp + ".png");
            for (int n = 2; File.Exists(path) || Pending.Contains(path); n++) path = Path.Combine(Folder, $"{stamp} ({n}).png");
            Pending.Add(path);
            return path;
        }

        internal static void Done(string path) => Pending.Remove(path);

        /// <summary>Writes the small copy the tab shows; takes the texture and destroys it.</summary>
        internal static void WriteThumb(Texture2D image, string path)
        {
            Texture2D thumb = FrameGrab.Shrink(image, ThumbWidth);
            try
            {
                Directory.CreateDirectory(Thumbs);
                File.WriteAllBytes(ThumbOf(path), thumb.EncodeToJPG(80));
            }
            finally
            {
                Object.Destroy(thumb);
            }
        }

        /// <summary>The small copy as a data URL, made now from the file when it is missing or older than the file.</summary>
        internal static string Thumb(string name)
        {
            string path = PathOf(name), thumb = ThumbOf(path);
            if (!File.Exists(thumb) || File.GetLastWriteTime(thumb) < File.GetLastWriteTime(path))
            {
                var image = new Texture2D(2, 2);
                if (!ImageCodec.Load(image, File.ReadAllBytes(path))) { Object.Destroy(image); throw new BridgeException($"{name} is not an image the game can read"); }
                WriteThumb(image, path);
            }
            return "data:image/jpeg;base64," + Convert.ToBase64String(File.ReadAllBytes(thumb));
        }

        internal static string Image(string name)
        {
            string path = PathOf(name), type = Path.GetExtension(path).ToLowerInvariant() == ".png" ? "png" : "jpeg";
            return $"data:image/{type};base64," + Convert.ToBase64String(File.ReadAllBytes(path));
        }

        /// <summary>A new name (the extension kept when it is left off); the small copy follows.</summary>
        internal static Dictionary<string, object> Rename(string name, string to)
        {
            string path = PathOf(name), wanted = (to ?? "").Trim();
            if (!Kinds.Contains(Path.GetExtension(wanted).ToLowerInvariant())) wanted += Path.GetExtension(path);
            if (wanted.Length == 0 || wanted.StartsWith(".") || wanted.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new BridgeException($"\"{to}\" cannot be a file name: leave out \\ / : * ? \" < > |");
            string target = Path.Combine(Folder, wanted);
            if (string.Equals(target, path, StringComparison.Ordinal)) return Describe(new FileInfo(path));
            if (File.Exists(target) && !string.Equals(target, path, StringComparison.OrdinalIgnoreCase)) throw new BridgeException($"{wanted} exists already");
            File.Move(path, target);
            MoveThumb(ThumbOf(path), ThumbOf(target));
            return Describe(new FileInfo(target));
        }

        // A stale small copy under the new name goes first; a change of case alone is the same file.
        private static void MoveThumb(string from, string into)
        {
            if (!File.Exists(from)) return;
            if (!string.Equals(from, into, StringComparison.OrdinalIgnoreCase) && File.Exists(into)) File.Delete(into);
            File.Move(from, into);
        }

        /// <summary>Moves the named shots to the Recycle Bin, with their small copies.</summary>
        internal static Dictionary<string, object> Delete(IEnumerable<string> names)
        {
            List<string> paths = names.Select(PathOf).Distinct().ToList();
            if (paths.Count == 0) throw new BridgeException("give names= of the screenshots to delete");
            RecycleBin.Send(paths);
            foreach (string thumb in paths.Select(ThumbOf).Where(File.Exists)) File.Delete(thumb);
            return new Dictionary<string, object> { ["deleted"] = paths.Count };
        }

        /// <summary>Explorer at the folder, or with one shot picked.</summary>
        internal static Dictionary<string, object> Open(string name)
        {
            string target = name == null ? Folder : PathOf(name);
            Directory.CreateDirectory(Folder);
            Process.Start(new ProcessStartInfo("explorer.exe", name == null ? $"\"{target}\"" : $"/select,\"{target}\"") { UseShellExecute = true });
            return new Dictionary<string, object> { ["opened"] = target };
        }

        // A shot in the folder by its bare file name: nothing above it or beside it.
        private static string PathOf(string name)
        {
            if (string.IsNullOrEmpty(name) || name != Path.GetFileName(name) || !Kinds.Contains(Path.GetExtension(name).ToLowerInvariant()))
                throw new BridgeException($"{name} is not a screenshot name");
            string path = Path.Combine(Folder, name);
            return File.Exists(path) ? path : throw new BridgeException($"no screenshot {name}");
        }

        private static string ThumbOf(string path) => Path.Combine(Thumbs, Path.GetFileName(path) + ".jpg");

        private static bool IsShot(FileInfo file) =>
            Kinds.Contains(file.Extension.ToLowerInvariant()) && !Pending.Contains(file.FullName) && (file.Attributes & FileAttributes.Hidden) == 0;
    }
}
