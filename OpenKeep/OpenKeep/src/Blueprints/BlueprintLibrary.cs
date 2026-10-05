using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using OpenKeep.Core;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// The blueprint files of this machine: BepInEx/config/OpenKeep.Blueprints, with folders. A blueprint or folder is
    /// named by its path from the top folder with "/" ("houses/plain_wood_house", no ".json"). The top folder is made on
    /// first use with the blueprints that ship inside the DLL. A folder's listing is read when the Blueprints tab shows
    /// it and again every few seconds; a file is read when the tab first lists it and again when it changes on disk.
    /// The folder the tab shows (<see cref="CurrentFolder"/>) is kept for the session.
    /// </summary>
    public static class BlueprintLibrary
    {
        private const string FolderName = "OpenKeep.Blueprints";
        private const string ResourcePrefix = "OpenKeep.blueprints.";

        /// <summary>One read file: what it gave, when it was written, and when to look at the file again.</summary>
        private sealed class Entry
        {
            public DateTime Written;
            public Blueprint Blueprint;
            public string Error;
            public float Checked;
        }

        /// <summary>One folder as last read: its blueprints and subfolders (paths from the top, sorted), and when to read it again.</summary>
        private sealed class Listing
        {
            public List<string> Blueprints = new List<string>();
            public List<string> Folders = new List<string>();
            public float Next;

            public bool SameAs(Listing other) => Blueprints.SequenceEqual(other.Blueprints) && Folders.SequenceEqual(other.Folders);
        }

        private static readonly Dictionary<string, Entry> cache = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Listing> listings = new Dictionary<string, Listing>(StringComparer.OrdinalIgnoreCase);

        /// <summary>A folder is looked at again (new files, changed files) at most this often, seconds.</summary>
        private const float RescanInterval = 2f;

        /// <summary>The reason the file last loaded could not be read, or null.</summary>
        public static string Error { get; private set; }

        public static string Folder => Path.Combine(BepInEx.Paths.ConfigPath, FolderName);

        /// <summary>The folder the Blueprints tab shows, relative to <see cref="Folder"/> ("" for the top); kept for the session.</summary>
        public static string CurrentFolder { get; private set; } = "";

        /// <summary>Goes up whenever a folder's listing changed, a folder was opened or a file was made, renamed or moved.</summary>
        public static int Version { get; private set; }

        /// <summary>The blueprints of a folder (the current one when null), sorted; the folder is read again every few seconds.</summary>
        public static List<string> Names(string folder = null) => ListingOf(folder ?? CurrentFolder).Blueprints;

        /// <summary>The subfolders of a folder (the current one when null), sorted.</summary>
        public static List<string> Folders(string folder = null) => ListingOf(folder ?? CurrentFolder).Folders;

        /// <summary>Every folder is read again at its next use (after saving, renaming or moving a file).</summary>
        public static void Rescan()
        {
            listings.Clear();
            Version++;
        }

        /// <summary>Shows another folder in the tab; false when it does not exist (any more).</summary>
        public static bool Open(string folder)
        {
            if (folder == null || !Directory.Exists(FullPath(folder)))
                return false;
            CurrentFolder = folder;
            Version++;
            return true;
        }

        /// <summary>A folder was renamed or moved: the tab follows it when it showed it or a folder inside it.</summary>
        public static void FollowMove(string from, string to)
        {
            string moved = Moved(CurrentFolder, from, to);
            if (moved != CurrentFolder)
                Open(moved);
        }

        private static Listing ListingOf(string folder)
        {
            float now = UnityEngine.Time.realtimeSinceStartup;
            if (listings.TryGetValue(folder, out Listing known) && now < known.Next)
                return known;
            Listing read = Read(folder);
            read.Next = now + RescanInterval;
            if (known != null && known.SameAs(read))
            {
                known.Next = read.Next;
                return known;
            }
            listings[folder] = read;
            Version++;
            return read;
        }

        /// <summary>A folder's files and subfolders; a folder that went away shows as empty (and the tab goes back to the top).</summary>
        private static Listing Read(string folder)
        {
            EnsureFolder();
            string dir = FullPath(folder);
            Listing listing = new Listing();
            if (!Directory.Exists(dir))
            {
                if (folder == CurrentFolder)
                    CurrentFolder = "";
                return listing;
            }
            listing.Blueprints = Directory.GetFiles(dir, "*.json").Select(f => Join(folder, Path.GetFileNameWithoutExtension(f)))
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
            listing.Folders = Directory.GetDirectories(dir).Select(Path.GetFileName).Where(n => !n.StartsWith("."))
                .Select(n => Join(folder, n)).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
            return listing;
        }

        /// <summary>Every blueprint (or every folder) below the top folder, as paths from the top, sorted.</summary>
        public static List<string> Everything(bool folders)
        {
            EnsureFolder();
            IEnumerable<string> found = folders
                ? Directory.GetDirectories(Folder, "*", SearchOption.AllDirectories)
                : Directory.GetFiles(Folder, "*.json", SearchOption.AllDirectories).Select(f => f.Substring(0, f.Length - 5));
            return found.Select(Relative).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static string Relative(string full) => full.Substring(Folder.Length).TrimStart('\\', '/').Replace('\\', '/');

        /// <summary>A blueprint by path: from the cache while its file is unchanged (looked at every few seconds), else read.</summary>
        public static Blueprint Load(string name)
        {
            Error = null;
            if (name == null)
                return null;
            string path = PathOf(name);
            float now = UnityEngine.Time.realtimeSinceStartup;
            if (!cache.TryGetValue(path, out Entry entry) || (now >= entry.Checked && File.GetLastWriteTimeUtc(path) != entry.Written))
                entry = cache[path] = ReadFile(path);
            if (now >= entry.Checked)
                entry.Checked = now + RescanInterval;
            Error = entry.Error;
            return entry.Blueprint;
        }

        /// <summary>Reads a file; its name in game is the file's name (a renamed file never shows the name inside the JSON).</summary>
        private static Entry ReadFile(string path)
        {
            Entry entry = new Entry { Written = File.GetLastWriteTimeUtc(path) };
            try
            {
                entry.Blueprint = BlueprintReader.Read(path);
                entry.Blueprint.Name = Path.GetFileNameWithoutExtension(path);
            }
            catch (Exception e)
            {
                entry.Error = Path.GetFileName(path) + ": " + e.Message;
                Plugin.Log.LogWarning("OpenKeep: cannot read blueprint " + entry.Error);
            }
            return entry;
        }

        /// <summary>The file of a blueprint path.</summary>
        public static string PathOf(string name) => FullPath(name) + ".json";

        /// <summary>The directory of a folder path ("" is the top folder).</summary>
        public static string FullPath(string relative) =>
            string.IsNullOrEmpty(relative) ? Folder : Path.Combine(Folder, relative.Replace('/', Path.DirectorySeparatorChar));

        public static string Join(string folder, string name) => string.IsNullOrEmpty(folder) ? name : folder + "/" + name;

        /// <summary>The folder a path sits in ("" for the top).</summary>
        public static string Parent(string path)
        {
            int slash = path?.LastIndexOf('/') ?? -1;
            return slash < 0 ? "" : path.Substring(0, slash);
        }

        /// <summary>The last part of a path: the file or folder name.</summary>
        public static string Leaf(string path) => path == null ? null : path.Substring(path.LastIndexOf('/') + 1);

        /// <summary>A path after <paramref name="from"/> moved to <paramref name="to"/>: itself, or inside it, follows; anything else stays.</summary>
        public static string Moved(string path, string from, string to)
        {
            if (path == null || from == null)
                return path;
            if (string.Equals(path, from, StringComparison.OrdinalIgnoreCase))
                return to;
            return path.StartsWith(from + "/", StringComparison.OrdinalIgnoreCase) ? to + path.Substring(from.Length) : path;
        }

        /// <summary>
        /// Writes a new blueprint named <paramref name="name"/> into the current folder; false (with the reason) when the
        /// name is not a valid file name or is taken. The menu picks it up at once.
        /// </summary>
        public static bool SaveNew(Blueprint bp, string name, out string error)
        {
            error = BlueprintFiles.CheckName(name);
            string path = PathOf(Join(CurrentFolder, name));
            if (error == null && File.Exists(path))
                error = BlueprintWords.Format(BlueprintWords.Exists, Join(CurrentFolder, name));
            if (error != null)
                return false;
            bp.Name = name;
            Directory.CreateDirectory(FullPath(CurrentFolder));
            BlueprintWriter.Write(bp, path);
            Rescan();
            return true;
        }

        /// <summary>Makes the folder with the shipped blueprints the first time; never touches an existing folder.</summary>
        private static void EnsureFolder()
        {
            if (Directory.Exists(Folder))
                return;
            Directory.CreateDirectory(Folder);
            Assembly assembly = typeof(BlueprintLibrary).Assembly;
            foreach (string resource in assembly.GetManifestResourceNames().Where(r => r.StartsWith(ResourcePrefix)))
                BlueprintSafe.Run("OpenKeep blueprint defaults", () => WriteResource(assembly, resource));
        }

        private static void WriteResource(Assembly assembly, string resource)
        {
            using (Stream input = assembly.GetManifestResourceStream(resource))
            using (FileStream output = File.Create(Path.Combine(Folder, resource.Substring(ResourcePrefix.Length))))
                input.CopyTo(output);
        }
    }
}
