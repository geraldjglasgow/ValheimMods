using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BepInEx;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The saved ships on this machine: <c>BepInEx/config/Broadside.Ships/</c>, one file per ship (<see cref="KwBpFile"/>),
    /// in folders the table's window makes (<see cref="KwBpShelf"/>); a ship is named by its path from the top with "/"
    /// ("karves/longboat", no ".json"), a saved name in letters, digits, "-" and "_". The folder and its subfolders are
    /// looked at again at most every two seconds when the names are asked for; <see cref="Version"/> moves on whenever a
    /// file comes, goes or changes (the hammer's saved-ship entries follow it, <see cref="KwBpHammer"/>).
    /// </summary>
    public static class KwBpLibrary
    {
        private const float Every = 2f;
        private const int Longest = 40;
        private const string Extension = ".json";
        private static readonly List<string> names = new List<string>();
        private static readonly Dictionary<string, DateTime> written = new Dictionary<string, DateTime>();
        private static float nextScan = float.MinValue;

        public static int Version { get; private set; }

        public static string Folder => Path.Combine(Paths.ConfigPath, "Broadside.Ships");

        /// <summary>Every saved ship's path, in order, subfolders included.</summary>
        public static IReadOnlyList<string> Names()
        {
            if (Time.realtimeSinceStartup >= nextScan)
                Scan();
            return names;
        }

        /// <summary>When a saved ship's file was last written (as last looked at); the earliest time for one not there.</summary>
        public static DateTime WrittenOf(string name) => written.TryGetValue(name, out DateTime time) ? time : DateTime.MinValue;

        public static string PathOf(string name) => Path.Combine(Folder, name.Replace('/', Path.DirectorySeparatorChar) + Extension);

        /// <summary>A typed name as a file name: anything but letters, digits, "-" and "_" becomes "_"; "ship" when nothing is left.</summary>
        public static string SafeName(string typed)
        {
            string safe = Regex.Replace((typed ?? "").Trim(), "[^A-Za-z0-9_-]", "_").Trim('_');
            if (safe.Length > Longest)
                safe = safe.Substring(0, Longest);
            return safe.Length == 0 ? "ship" : safe;
        }

        /// <summary>A saved ship, or null (and a warning in the log) when it cannot be read.</summary>
        public static KwBlueprint? Load(string name) => Load(name, out _);

        /// <summary>As <see cref="Load(string)"/>, with the file's text as read (null when it could not be read).</summary>
        public static KwBlueprint? Load(string name, out string? text)
        {
            text = null;
            try
            {
                text = File.ReadAllText(PathOf(name));
                return KwBpStrays.Trim(KwBpFile.Read(name, text));
            }
            catch (Exception e) when (e is IOException || e is FormatException || e is UnauthorizedAccessException)
            {
                Log.Warn($"Broadside blueprint {name} could not be read: {e.Message}");
                return null;
            }
        }

        /// <summary>Writes the ship (replacing one at the same path); the reason when it could not, else null.</summary>
        public static string? Save(KwBlueprint blueprint)
        {
            try
            {
                string path = PathOf(blueprint.Name), temporary = path + ".tmp";
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(temporary, KwBpFile.Write(blueprint));
                if (File.Exists(path))
                    File.Delete(path);
                File.Move(temporary, path);
                Scan();
                return null;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Log.Warn($"Broadside blueprint {blueprint.Name} could not be saved: {e.Message}");
                return e.Message;
            }
        }

        /// <summary>Looks at the folder now; the names are listed again and the version moves on when anything in it changed.</summary>
        public static void Scan()
        {
            nextScan = Time.realtimeSinceStartup + Every;
            Dictionary<string, DateTime> now = Read();
            if (now.Count == written.Count && now.All(p => written.TryGetValue(p.Key, out DateTime t) && t == p.Value))
                return;
            written.Clear();
            foreach (KeyValuePair<string, DateTime> pair in now)
                written[pair.Key] = pair.Value;
            names.Clear();
            names.AddRange(now.Keys.OrderBy(n => n, StringComparer.OrdinalIgnoreCase));
            Version++;
        }

        private static Dictionary<string, DateTime> Read()
        {
            Dictionary<string, DateTime> found = new Dictionary<string, DateTime>(StringComparer.Ordinal);
            try
            {
                if (!Directory.Exists(Folder))
                    return found;
                foreach (string path in Directory.GetFiles(Folder, "*" + Extension, SearchOption.AllDirectories))
                {
                    if (string.Equals(Path.GetExtension(path), Extension, StringComparison.OrdinalIgnoreCase))
                        found[Relative(path)] = File.GetLastWriteTimeUtc(path);
                }
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Log.Warn($"Broadside blueprints folder could not be read: {e.Message}");
            }
            return found;
        }

        // "<folder>\karves\longboat.json" as "karves/longboat".
        private static string Relative(string file) =>
            file.Substring(Folder.Length + 1, file.Length - Folder.Length - 1 - Extension.Length).Replace('\\', '/');
    }
}
