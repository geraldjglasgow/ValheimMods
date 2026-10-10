using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Configuration;
using DevBridge.Server;

namespace DevBridge.Studio
{
    /// <summary>
    /// A bundle the workshop built for Windows: its name (the file's, without .windows), file, build time and category
    /// (the folder under Assets holding its asset folder or its file: Creatures, Weapons, Gear, Props...; null for neither).
    /// </summary>
    internal sealed class WorkshopFile
    {
        internal string Name;
        internal string Path;
        internal string Relative;
        internal string Category;
        internal DateTime Built;
        internal long Bytes;
        internal int Copies;
    }

    /// <summary>
    /// The asset workshop's built bundles (ValheimAssets: Assets/.../out, Exports/Bundles and the root out folder), found by
    /// their .windows files. One bundle built to several folders counts once, its newest build. The Unity projects'
    /// caches and the reference export are not searched.
    /// </summary>
    internal static class WorkshopFiles
    {
        private const string Suffix = ".windows";
        private static readonly HashSet<string> Skipped = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".git", "Library", "Temp", "Logs", "obj", "Packages", "UserSettings", "Reference", "node_modules",
        };

        private static ConfigEntry<string> folder;

        internal static void Bind(ConfigFile config) =>
            folder = config.Bind("Studio", "Workshop folder", "",
                "The asset workshop (ValheimAssets) whose built bundles the studio's Workshop tab lists. Empty looks in " +
                "<your user folder>\\projects\\ValheimAssets.");

        internal static string Folder()
        {
            string given = folder?.Value.Trim().Trim('"');
            string path = string.IsNullOrEmpty(given)
                ? System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "projects", "ValheimAssets")
                : given;
            return Directory.Exists(path) ? System.IO.Path.GetFullPath(path)
                : throw new BridgeException($"no workshop folder at {path}: set Workshop folder under [Studio] in DevBridge's .cfg");
        }

        /// <summary>Every bundle in the workshop, newest build first.</summary>
        internal static List<WorkshopFile> All()
        {
            string root = Folder();
            var files = new List<string>();
            var categories = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Walk(root, System.IO.Path.Combine(root, "Assets"), null, files, categories);
            return files.Select(path => Make(root, path, categories)).GroupBy(file => file.Name, StringComparer.OrdinalIgnoreCase)
                .Select(Newest).OrderByDescending(file => file.Built).ToList();
        }

        internal static WorkshopFile Find(string name) =>
            All().FirstOrDefault(file => string.Equals(file.Name, name, StringComparison.OrdinalIgnoreCase))
            ?? throw new BridgeException($"the workshop has no bundle {name} built for Windows (list them with /studio/workshop)");

        // The bundle files, and each folder under Assets by name with its category (the first one of a name wins).
        private static void Walk(string directory, string assets, string category, List<string> files, Dictionary<string, string> categories)
        {
            files.AddRange(Directory.GetFiles(directory, "*" + Suffix));
            foreach (string sub in Directory.GetDirectories(directory))
            {
                string name = System.IO.Path.GetFileName(sub);
                if (Skipped.Contains(name)) continue;
                string under = category ?? (string.Equals(directory, assets, StringComparison.OrdinalIgnoreCase) ? name : null);
                if (under != null && !categories.ContainsKey(name)) categories[name] = under;
                Walk(sub, assets, under, files, categories);
            }
        }

        private static WorkshopFile Make(string root, string path, Dictionary<string, string> categories)
        {
            var info = new FileInfo(path);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            string relative = path.Substring(root.Length).TrimStart('\\', '/').Replace('\\', '/');
            string[] parts = relative.Split('/');
            return new WorkshopFile
            {
                Name = name, Path = path, Relative = relative, Built = info.LastWriteTime, Bytes = info.Length,
                Category = categories.TryGetValue(name, out string category) ? category : parts.Length > 2 && parts[0] == "Assets" ? parts[1] : null,
            };
        }

        private static WorkshopFile Newest(IGrouping<string, WorkshopFile> builds)
        {
            WorkshopFile newest = builds.OrderByDescending(file => file.Built).First();
            newest.Copies = builds.Count();
            return newest;
        }
    }
}
