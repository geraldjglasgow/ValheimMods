using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

namespace OpenKeep.Blueprints.Bench
{
    /// <summary>
    /// The server's copy of the shared pool, on its own disk: BepInEx/config/OpenKeep.SharedBlueprints/&lt;world&gt;/, one
    /// folder per player named &lt;name&gt;_&lt;character id&gt; holding their shared blueprint files in their folders (empty
    /// folders kept: players organise their part), so an admin can read or tidy it by hand. The listing is kept in
    /// memory, read again after every change through the bench and at most every <see cref="Rescan"/> seconds otherwise
    /// (files changed by hand). Server only.
    /// </summary>
    public static class BenchStore
    {
        private const string FolderName = "OpenKeep.SharedBlueprints";
        private const float Rescan = 30f;

        private static List<BenchOwner> owners;
        private static string scannedRoot;
        private static float scannedAt;
        private static ZPackage listing;

        public static string Root =>
            Path.Combine(Path.Combine(BepInEx.Paths.ConfigPath, FolderName), BenchPaths.Safe(ZNet.instance.GetWorldName()));

        public static List<BenchOwner> Owners
        {
            get
            {
                string root = Root;
                float now = Time.realtimeSinceStartup;
                if (owners != null && root == scannedRoot && now - scannedAt < Rescan)
                    return owners;
                owners = Scan(root);
                scannedRoot = root;
                scannedAt = now;
                listing = null;
                return owners;
            }
        }

        /// <summary>The pool packed for <see cref="BenchRpc.Listing"/>, made once per change.</summary>
        public static ZPackage Listing
        {
            get
            {
                List<BenchOwner> current = Owners;
                return listing ?? (listing = BenchListing.Write(current));
            }
        }

        public static BenchOwner Find(long id) => Owners.FirstOrDefault(o => o.Id == id);

        public static bool Has(long id, string path) => Find(id)?.Paths.Contains(path, StringComparer.OrdinalIgnoreCase) ?? false;

        private static void Invalidate() => owners = null;

        private static List<BenchOwner> Scan(string root)
        {
            List<BenchOwner> found = new List<BenchOwner>();
            if (!Directory.Exists(root))
                return found;
            foreach (string dir in Directory.GetDirectories(root))
            {
                BenchOwner owner = OwnerOf(dir);
                if (owner == null || found.Any(o => o.Id == owner.Id))
                    continue;
                owner.Paths.AddRange(Directory.GetFiles(dir, "*.json", SearchOption.AllDirectories).Select(f => Relative(dir, f, true))
                    .Where(p => BenchPaths.Clean(p) != null).OrderBy(p => p, StringComparer.OrdinalIgnoreCase));
                owner.Folders.AddRange(Directory.GetDirectories(dir, "*", SearchOption.AllDirectories).Select(d => Relative(dir, d, false))
                    .Where(p => BenchPaths.Clean(p) != null).OrderBy(p => p, StringComparer.OrdinalIgnoreCase));
                if (owner.Paths.Count > 0 || owner.Folders.Count > 0)
                    found.Add(owner);
            }
            return found;
        }

        /// <summary>A player's folder "name_id": the id after the last "_"; null for any other folder.</summary>
        private static BenchOwner OwnerOf(string dir)
        {
            string name = Path.GetFileName(dir);
            int cut = name.LastIndexOf('_');
            if (cut <= 0 || !long.TryParse(name.Substring(cut + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out long id) || id == 0L)
                return null;
            return new BenchOwner { Id = id, Name = name.Substring(0, cut) };
        }

        private static string Relative(string dir, string full, bool file) =>
            full.Substring(dir.Length + 1, full.Length - dir.Length - 1 - (file ? ".json".Length : 0)).Replace('\\', '/');

        /// <summary>The player's folder, or null when they never shared anything here.</summary>
        private static string FolderOf(long id)
        {
            string root = Root;
            return Directory.Exists(root) ? Directory.GetDirectories(root).FirstOrDefault(d => OwnerOf(d)?.Id == id) : null;
        }

        /// <summary>The player's folder, made under their name when they have none yet (no name: none made).</summary>
        private static string PartOf(long id, string name) =>
            FolderOf(id) ?? (name != null ? Path.Combine(Root, BenchPaths.Safe(name) + "_" + id.ToString(CultureInfo.InvariantCulture)) : null);

        private static string Under(string dir, string path) =>
            path.Length == 0 ? dir : Path.Combine(dir, path.Replace('/', Path.DirectorySeparatorChar));

        private static string FileOf(string dir, string path) => Under(dir, path) + ".json";

        /// <summary>Writes (or replaces) one shared blueprint of a player; their folder is made under their name on first use.</summary>
        public static void Save(long id, string name, string path, byte[] json)
        {
            string file = FileOf(PartOf(id, name), path);
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.WriteAllBytes(file, json);
            Invalidate();
        }

        /// <summary>A shared blueprint's file, or null when it is not there.</summary>
        public static byte[] Read(long id, string path)
        {
            string dir = FolderOf(id);
            string file = dir != null ? FileOf(dir, path) : null;
            return file != null && File.Exists(file) ? File.ReadAllBytes(file) : null;
        }

        /// <summary>
        /// Removes one blueprint, or a folder with everything in it ("" is all of the player's): how many blueprints went
        /// (0 for an empty folder), or -1 when it was not there.
        /// </summary>
        public static int Delete(long id, string path, bool folder)
        {
            string dir = FolderOf(id);
            string target = dir == null ? null : folder ? Under(dir, path) : FileOf(dir, path);
            int removed = -1;
            if (folder && Directory.Exists(target))
            {
                removed = Directory.GetFiles(target, "*.json", SearchOption.AllDirectories).Length;
                Directory.Delete(target, true);
            }
            else if (!folder && File.Exists(target))
            {
                File.Delete(target);
                removed = 1;
            }
            Invalidate();
            return removed;
        }

        /// <summary>Renames or moves a blueprint or folder within one player's blueprints; null when done, else the reason's word.</summary>
        public static string Move(long id, string from, string to, bool folder)
        {
            string dir = FolderOf(id);
            string source = dir == null ? null : folder ? Under(dir, from) : FileOf(dir, from);
            if (source == null || !(folder ? Directory.Exists(source) : File.Exists(source)))
                return BenchWords.Gone;
            bool caseOnly = string.Equals(from, to, StringComparison.OrdinalIgnoreCase);
            if (folder && !caseOnly && BenchPaths.Within(to, from))
                return BlueprintWords.IntoItself;
            string target = folder ? Under(dir, to) : FileOf(dir, to);
            if (!caseOnly && (Directory.Exists(target) || File.Exists(target)))
                return BlueprintWords.Exists;
            MoveOnDisk(source, target, folder);
            Invalidate();
            return null;
        }

        /// <summary>
        /// Makes an empty folder in a player's blueprints; their own part is made under <paramref name="name"/> when they
        /// have none yet (null: only an existing part). Null when made, else the reason's word.
        /// </summary>
        public static string MakeFolder(long id, string name, string path)
        {
            string dir = PartOf(id, name);
            if (dir == null)
                return BenchWords.Gone;
            string target = Under(dir, path);
            if (Directory.Exists(target) || File.Exists(target + ".json"))
                return BlueprintWords.Exists;
            Directory.CreateDirectory(target);
            Invalidate();
            return null;
        }

        /// <summary>Through a temporary name, so a change of case alone also works on Windows.</summary>
        private static void MoveOnDisk(string source, string target, bool folder)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            string step = source + ".openkeep-move";
            if (folder)
            {
                Directory.Move(source, step);
                Directory.Move(step, target);
            }
            else
            {
                File.Move(source, step);
                File.Move(step, target);
            }
        }
    }
}
