using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The emblems a sail's pattern can carry in its middle (<see cref="KwSailPattern"/>): the game's eleven banners
    /// (<c>piece_banner01</c> to <c>11</c>, by their names in the game), and the player's own pictures, PNG files in
    /// <c>BepInEx/config/Broadside.Sails/</c> (made when missing), listed by name. The game's banners carry no emblem of
    /// their own, so a banner is drawn whole as the game hangs it: its texture's cloth strip (from its left edge to where
    /// the cloth's colour stops) and the same strip mirrored beside it, as tall as the banner (1.18 by 2.93 m), its foot cut
    /// to the point it hangs to. A player's picture is saved in the sail by its file name ("img/raven.png") and shows only
    /// on games that have a file of that name (the sail's other patterning shows everywhere). Pictures are read once each,
    /// again when their file changes; only where there is a screen.
    /// </summary>
    public static class KwSailEmblems
    {
        public const string ImagePrefix = "img/";
        private const int Banners = 11, Largest = 512;
        private const long MostBytes = 8L << 20;
        private const float BannerAspect = 1.18f / 2.93f, BannerFoot = 0.14f;
        private static readonly Dictionary<string, KwEmblemArt?> loaded = new Dictionary<string, KwEmblemArt?>();

        public static string Folder => Path.Combine(Paths.ConfigPath, "Broadside.Sails");

        /// <summary>The emblems the windows offer: the game's banners by name, then the player's pictures.</summary>
        public static List<(string Id, string Label)> Choices()
        {
            List<(string Id, string Label)> choices = new List<(string Id, string Label)>();
            for (int n = 1; n <= Banners; n++)
            {
                GameObject? prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(BannerId(n)) : null;
                if (prefab != null)
                    choices.Add((BannerId(n), Label(prefab, n)));
            }
            foreach (string file in Images())
                choices.Add((ImagePrefix + file, "Picture: " + Path.GetFileNameWithoutExtension(file)));
            return choices;
        }

        private static string BannerId(int n) => "piece_banner" + n.ToString("00");

        // A banner's name in the game's language, or its number.
        private static string Label(GameObject prefab, int n)
        {
            Piece? piece = prefab.GetComponent<Piece>();
            string name = piece != null && Localization.instance != null ? Localization.instance.Localize(piece.m_name) : "";
            return name.Length > 0 && !name.StartsWith("[") ? name : "Banner " + n;
        }

        /// <summary>The player's pictures: the PNG files in the folder (made when missing) whose names a sail's text can hold, by name.</summary>
        public static List<string> Images()
        {
            try
            {
                Directory.CreateDirectory(Folder);
                return Directory.GetFiles(Folder, "*.png").Select(Path.GetFileName).Where(n => PathOf(ImagePrefix + n) != null)
                    .OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
            }
            catch (Exception e)
            {
                Log.Warn($"Broadside: the sail pictures in {Folder} could not be listed: {e.Message}");
                return new List<string>();
            }
        }

        /// <summary>Whether an emblem is a player's own picture, shown only where the same file is.</summary>
        public static bool IsPicture(string id) => id.StartsWith(ImagePrefix, StringComparison.Ordinal);

        // A picture's file in the folder; null for any other id, or a name that would reach outside the folder.
        private static string? PathOf(string id)
        {
            string name = IsPicture(id) ? id.Substring(ImagePrefix.Length) : "";
            bool plain = name.Length > 4 && Path.GetFileName(name) == name && !name.Contains("..") && KwSailPattern.Safe(id);
            return plain && name.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? Path.Combine(Folder, name) : null;
        }

        /// <summary>What an emblem's picture is made from, for sharing the sails painted with it: a file's size and time, or that it is missing.</summary>
        public static string Stamp(string id)
        {
            string? path = IsPicture(id) ? PathOf(id) : null;
            if (path == null)
                return "";
            FileInfo file = new FileInfo(path);
            return file.Exists ? file.Length + "@" + file.LastWriteTimeUtc.Ticks : "none";
        }

        /// <summary>An emblem's picture; null when this game has none (another player's picture, a banner the game lacks).</summary>
        public static KwEmblemArt? Art(string id)
        {
            string key = id + "|" + Stamp(id);
            if (!loaded.TryGetValue(key, out KwEmblemArt? art))
                loaded[key] = art = IsPicture(id) ? Picture(id) : Banner(id);
            return art;
        }

        /// <summary>Every picture read is let go (a new world: the banners are read again from its scene).</summary>
        public static void Clear() => loaded.Clear();

        // A player's picture, read at most Largest pixels on its longer side.
        private static KwEmblemArt? Picture(string id)
        {
            string? path = PathOf(id);
            FileInfo? file = path != null ? new FileInfo(path) : null;
            if (file == null || !file.Exists || file.Length > MostBytes)
                return null;
            Texture2D? texture = KwSailPixels.Decode(File.ReadAllBytes(file.FullName), file.Name, false);
            if (texture == null)
            {
                Log.Warn($"Broadside: the sail picture {file.Name} is not a PNG the game can read.");
                return null;
            }
            float scale = Mathf.Min(1f, (float)Largest / Mathf.Max(texture.width, texture.height));
            int width = Mathf.Max(1, Mathf.RoundToInt(texture.width * scale)), height = Mathf.Max(1, Mathf.RoundToInt(texture.height * scale));
            KwEmblemArt art = new KwEmblemArt(KwSailPixels.Read(texture, width, height), width, height, (float)texture.width / texture.height);
            UnityEngine.Object.Destroy(texture);
            return art;
        }

        // One of the game's banners, whole, from its cloth's texture.
        private static KwEmblemArt? Banner(string id)
        {
            bool banner = id.StartsWith("piece_banner", StringComparison.Ordinal);
            GameObject? prefab = banner && ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(id) : null;
            Texture? texture = prefab != null ? Cloth(prefab) : null;
            if (texture == null)
                return null;
            Color32[] pixels = KwSailPixels.Read(texture, texture.width, texture.height);
            return Whole(pixels, texture.width, texture.height);
        }

        // The banner's cloth texture: the first of its materials named for a banner that has one.
        private static Texture? Cloth(GameObject prefab)
        {
            foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material != null && material.mainTexture != null && material.name.IndexOf("banner", StringComparison.OrdinalIgnoreCase) >= 0)
                        return material.mainTexture;
                }
            }
            return null;
        }

        /// <summary>A banner drawn whole from its texture: the cloth strip at its left and the strip mirrored beside it, its foot cut to a point.</summary>
        public static KwEmblemArt Whole(Color32[] pixels, int width, int height)
        {
            int strip = Strip(pixels, width, height), wide = 2 * strip;
            Color32[] made = new Color32[wide * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < wide; x++)
                {
                    Color32 c = pixels[y * width + (x < strip ? x : wide - 1 - x)];
                    float off = Mathf.Abs(2f * (x + 0.5f) / wide - 1f);
                    c.a = (y + 0.5f) / height < BannerFoot * off ? (byte)0 : c.a;
                    made[y * wide + x] = c;
                }
            }
            return new KwEmblemArt(made, wide, height, BannerAspect);
        }

        // How wide the cloth's strip is: solid from the texture's left edge along its middle row (a quarter of it when not).
        private static int Strip(Color32[] pixels, int width, int height)
        {
            int row = height / 2 * width, x = 0;
            while (x < width / 2 && pixels[row + x].a >= 200)
                x++;
            return x >= 4 ? x : Mathf.Max(1, width / 4);
        }
    }
}
