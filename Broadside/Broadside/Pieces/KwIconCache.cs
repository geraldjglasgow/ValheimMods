using System;
using System.IO;
using System.IO.Compression;
using BepInEx;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Rendered hammer icons kept, so a launch renders only what is new: rendering one stalls the frame on the graphics
    /// card (two renders read back, 20 to 165 ms a saved ship measured on 2026-10-27), reading one back is a small file.
    /// An icon is found by its prefab's name and a stamp of what it was drawn from (for a saved ship a hash of its file's
    /// text, <see cref="Hash"/>; nothing for the mod's own pieces and items): first among those the mod carries
    /// (<c>assets/icons</c>, made with <c>tools/bake_icons.py</c>: the ready-made ships, the pieces and items that are
    /// rendered), else on disk in <c>BepInEx/cache/Broadside.Icons</c>, else it is rendered and kept there. A player's own
    /// ship is so rendered once, and again only when its file changes (saved again at the table), whatever the mod's
    /// version (the user, 2026-10-27: "the only time we really need to refresh the icon is when we make changes to the
    /// ship"). Keeping a new one drops the name's older ones. A file that cannot be read or written is passed over and the
    /// icon rendered as before. Clients only.
    /// </summary>
    public static class KwIconCache
    {
        /// <summary>
        /// How ships, pieces and items are drawn in their icons. Raise it with a change that alters how they look in an
        /// icon (a material, a mesh, the icon camera or light): every kept icon is then rendered once more, and the
        /// carried ones must be made again (<c>tools/bake_icons.py</c>, which reads this number from here). Left alone,
        /// icons last across updates (the user, 2026-10-27: "some way to say if something has changed that would require
        /// us to redo the ship icon. Else don't. Between updates.").
        /// </summary>
        public const int Look = 1;

        private const int Magic = 0x3149574B; // "KWI1"
        private const string Carried = "Broadside.icons.";
        private static bool settled, warned;

        private static string Folder => Path.Combine(Paths.CachePath, "Broadside.Icons");

        /// <summary>The icon carried or kept for a prefab drawn from this source (null: the mod's own pieces and items), or null.</summary>
        public static Sprite? Find(string name, string? source)
        {
            string file = FileName(name, source);
            Sprite? carried = CarriedIcon(file, name);
            if (carried != null)
                return carried;
            try
            {
                Settle();
                string path = Path.Combine(Folder, file);
                return File.Exists(path) ? Decode(File.ReadAllBytes(path), name) : null;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                return null;
            }
        }

        // An icon the mod carries, by its file name (assets/icons, gzipped, embedded as Broadside.icons.<file>).
        private static Sprite? CarriedIcon(string file, string name)
        {
            using Stream? stream = typeof(KwIconCache).Assembly.GetManifestResourceStream(Carried + file);
            if (stream == null)
                return null;
            using GZipStream unzipped = new GZipStream(stream, CompressionMode.Decompress);
            using MemoryStream bytes = new MemoryStream();
            unzipped.CopyTo(bytes);
            return Decode(bytes.ToArray(), name);
        }

        /// <summary>Keeps a rendered icon's pixels (bottom row first, size by size) for the next launch.</summary>
        public static void Keep(string name, string? source, Color32[] pixels, int size)
        {
            try
            {
                Settle();
                Directory.CreateDirectory(Folder);
                foreach (string older in Directory.GetFiles(Folder, Safe(name) + "@*.icon"))
                    File.Delete(older);
                File.WriteAllBytes(Path.Combine(Folder, FileName(name, source)), Encode(pixels, size));
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                if (!warned)
                    Log.Warn($"Broadside: icons could not be kept in {Folder} ({e.Message}); they are rendered each launch.");
                warned = true;
            }
        }

        // Once a launch: icons kept in a folder per build or version (the first builds of the cache, 2026-10-27) moved up
        // into the one folder, so none is rendered again for it, and those folders gone.
        private static void Settle()
        {
            if (settled || !Directory.Exists(Folder))
                return;
            settled = true;
            foreach (string folder in Directory.GetDirectories(Folder))
            {
                foreach (string icon in Directory.GetFiles(folder, "*.icon"))
                {
                    string to = Path.Combine(Folder, Path.GetFileName(icon));
                    if (!File.Exists(to))
                        File.Move(icon, to);
                }
                Directory.Delete(folder, true);
            }
        }

        // "<name>@<hash of the look and source>.icon"; tools/bake_icons.py names the carried ones the same way. Look 1 is
        // the source alone (the first icons kept), a later look in front of it.
        private static string FileName(string name, string? source)
        {
            string stamped = Look == 1 ? source ?? "" : Look + "|" + (source ?? "");
            return $"{Safe(name)}@{Hash(stamped)}.icon";
        }

        private static string Safe(string name)
        {
            char[] chars = name.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '_' && chars[i] != '-')
                    chars[i] = '_';
            }
            return new string(chars);
        }

        /// <summary>FNV-1a, 64 bits, over a text's UTF-16 code units, as 16 hex digits (tools/bake_icons.py works it out the same way).</summary>
        public static string Hash(string text)
        {
            ulong hash = 14695981039346656037UL;
            foreach (char c in text)
                hash = (hash ^ c) * 1099511628211UL;
            return hash.ToString("x16");
        }

        private static byte[] Encode(Color32[] pixels, int size)
        {
            byte[] bytes = new byte[8 + pixels.Length * 4];
            BitConverter.GetBytes(Magic).CopyTo(bytes, 0);
            BitConverter.GetBytes(size).CopyTo(bytes, 4);
            for (int i = 0; i < pixels.Length; i++)
            {
                int at = 8 + i * 4;
                (bytes[at], bytes[at + 1], bytes[at + 2], bytes[at + 3]) = (pixels[i].r, pixels[i].g, pixels[i].b, pixels[i].a);
            }
            return bytes;
        }

        private static Sprite? Decode(byte[] bytes, string name)
        {
            if (bytes.Length < 8 || BitConverter.ToInt32(bytes, 0) != Magic)
                return null;
            int size = BitConverter.ToInt32(bytes, 4);
            if (size <= 0 || size > 1024 || bytes.Length != 8 + size * size * 4)
                return null;
            Color32[] pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                int at = 8 + i * 4;
                pixels[i] = new Color32(bytes[at], bytes[at + 1], bytes[at + 2], bytes[at + 3]);
            }
            return KwIconPicture.Make(pixels, size, name);
        }
    }
}
