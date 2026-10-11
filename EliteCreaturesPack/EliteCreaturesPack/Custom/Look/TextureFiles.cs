using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using EliteCreaturesPack.Core;
using EliteCreaturesPack.Custom.Build;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EliteCreaturesPack.Custom.Look
{
    /// <summary>
    /// The image files a definition's <c>texture:</c> names. Images are never sent over the network: each player keeps
    /// their own copy in the BepInEx config folder, where the creature files are. The name is a path relative to that
    /// folder (<c>RedTroll.png</c>, <c>creatures/RedTroll.png</c>); when nothing is at that path, the first file of that
    /// name in its subfolders is taken (sorted, so it is always the same one). PNG or JPG, decoded by the game's own image
    /// loader (<c>ImageConversion</c>, found by reflection: its module targets netstandard 2.1, which a net48 project
    /// cannot reference) with mipmaps and point filtering like the game's own art, once per file, and again only when the
    /// file changes. A dedicated server draws nothing and loads nothing. A missing file is a warning in that player's log,
    /// once per session: the creature keeps its base's texture there.
    /// </summary>
    internal static class TextureFiles
    {
        private static readonly string[] Kinds = { ".png", ".jpg", ".jpeg" };
        private static readonly char[] Refused = { '<', '>', '|', ':', '*', '?', '"' };
        private static readonly Dictionary<string, KeyValuePair<DateTime, Texture2D>> loaded =
            new Dictionary<string, KeyValuePair<DateTime, Texture2D>>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> warned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static MethodInfo? decoder;
        private static bool resolved;

        /// <summary>
        /// One pass's <c>texture:</c>. A name that is not a plain relative image path fails the creature, alike on every
        /// peer (nothing is read for it); a file this machine does not have is only a warning, and only where it draws.
        /// </summary>
        public static void Check(CreatureBuild build)
        {
            string? name = build.Definition.Texture;
            if (name == null)
            {
                return;
            }
            string? problem = Malformed(name);
            if (problem != null)
            {
                build.Report.Fail(problem, "texture");
                return;
            }
            if (!Drawing.Headless && Find(name) == null && warned.Add(name))
            {
                build.Report.Warn($"the image '{name}' is not in the config folder ({Paths.ConfigPath}) on this machine, "
                    + "so the creature keeps its base's texture here. Images are not sent over the network: every player "
                    + "puts the file there.", "texture");
            }
        }

        /// <summary>The image, loaded or kept from before; null on a dedicated server or when it is missing or unreadable.</summary>
        public static Texture2D? Load(string name)
        {
            if (Drawing.Headless || Malformed(name) != null)
            {
                return null;
            }
            string? path = Find(name);
            return path != null ? Cached(path) : null;
        }

        /// <summary>
        /// Why a name is not a relative image path, or null. The same answer on every machine (a Linux server and Windows
        /// players differ in which characters a path may hold and what counts as rooted), so the characters refused and
        /// the rooted test are this mod's own, and nothing of the platform's path rules is asked before they pass.
        /// </summary>
        private static string? Malformed(string name)
        {
            if (name.StartsWith("/") || name.StartsWith("\\") || name.IndexOfAny(Refused) >= 0 || name.Any(char.IsControl)
                || name.Split('/', '\\').Contains(".."))
            {
                return $"'{name}' is not a file name or a path inside the config folder";
            }
            string kind = Path.GetExtension(name);
            return Kinds.Contains(kind, StringComparer.OrdinalIgnoreCase) ? null
                : $"'{name}' is not an image the game can read: use a .png or .jpg file";
        }

        /// <summary>The file's full path on this machine, or null; a folder that cannot be read counts as no file.</summary>
        private static string? Find(string name)
        {
            try
            {
                string root = Paths.ConfigPath;
                string direct = Path.Combine(root, name.Replace('\\', '/').Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(direct))
                {
                    return direct;
                }
                return Directory.GetFiles(root, Path.GetFileName(name), SearchOption.AllDirectories)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
            }
            catch (Exception)
            {
                return null; // this player's folder, this player's problem: never a failure of the creature
            }
        }

        private static Texture2D? Cached(string path)
        {
            DateTime written = File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
            if (loaded.TryGetValue(path, out KeyValuePair<DateTime, Texture2D> kept) && kept.Value != null)
            {
                if (kept.Key == written)
                {
                    return kept.Value;
                }
                MaterialRecipes.Forget(kept.Value);
                Object.Destroy(kept.Value);
            }
            loaded.Remove(path);
            Texture2D? texture = Decode(path);
            if (texture != null)
            {
                loaded[path] = new KeyValuePair<DateTime, Texture2D>(written, texture);
            }
            return texture;
        }

        private static Texture2D? Decode(string path)
        {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, true)
            {
                name = "ecp_" + Path.GetFileNameWithoutExtension(path),
                filterMode = FilterMode.Point,
            };
            if (Decoded(texture, path))
            {
                return texture;
            }
            Object.Destroy(texture);
            if (warned.Add(path))
            {
                Log.Warn($"Custom creatures: the image '{path}' could not be read; the creatures naming it keep their base's texture.");
            }
            return null;
        }

        /// <summary>A file that cannot be read (locked, not an image) is this player's problem only: never a failure.</summary>
        private static bool Decoded(Texture2D texture, string path)
        {
            try
            {
                MethodInfo? load = Decoder();
                return load != null && (bool)load.Invoke(null, new object[] { texture, File.ReadAllBytes(path), true });
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary><c>ImageConversion.LoadImage(Texture2D, byte[], bool)</c>, looked up once.</summary>
        private static MethodInfo? Decoder()
        {
            if (!resolved)
            {
                resolved = true;
                decoder = Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule")
                    ?.GetMethod("LoadImage", new[] { typeof(Texture2D), typeof(byte[]), typeof(bool) });
            }
            return decoder;
        }
    }
}
