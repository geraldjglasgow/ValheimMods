using System;
using System.Collections.Generic;
using BundlePrefabs;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The slatted crates' own albedos (<see cref="KwCrateCoat"/>): each bundle's three readable sources read once on the
    /// first crate that needs them (<c>&lt;bundle&gt;_clean</c>, the atlas without marks; <c>&lt;bundle&gt;_marksmask</c>,
    /// where marks may go; <c>bs_crate_marks</c>, the 1024 px pattern), and each seed's texture made by
    /// <see cref="KwCrateMarks"/>, compressed and no longer readable, shared by every crate of that bundle and seed while
    /// one is loaded and destroyed with the last (<see cref="Release"/>). A bundle whose sources cannot be read is logged
    /// once; its crates keep the bundle's own albedo.
    /// </summary>
    public static class KwCratePattern
    {
        public const string PatternName = "bs_crate_marks";

        private sealed class Sources
        {
            public Color32[] Clean = null!;
            public byte[] Mask = null!, Pattern = null!;
            public int Width, Height;
        }

        private static readonly Dictionary<string, Sources?> read = new Dictionary<string, Sources?>();
        private static readonly Dictionary<(string, int), (Texture2D Texture, int Users)> made = new Dictionary<(string, int), (Texture2D, int)>();

        /// <summary>The bundle's crate albedo for the seed, made on the first ask and counted; null when the sources cannot be read.</summary>
        public static Texture2D? Take(string bundle, int seed, Texture? like)
        {
            if (made.TryGetValue((bundle, seed), out (Texture2D Texture, int Users) kept) && kept.Texture != null)
            {
                made[(bundle, seed)] = (kept.Texture, kept.Users + 1);
                return kept.Texture;
            }
            Sources? sources = Ready(bundle);
            if (sources == null)
                return null;
            Texture2D texture = Make(sources, seed, like);
            texture.name = bundle + "_albedo_" + seed;
            made[(bundle, seed)] = (texture, 1);
            return texture;
        }

        /// <summary>One crate let go of its albedo; the last one destroys it.</summary>
        public static void Release(string bundle, int seed)
        {
            if (!made.TryGetValue((bundle, seed), out (Texture2D Texture, int Users) kept))
                return;
            if (kept.Users > 1)
            {
                made[(bundle, seed)] = (kept.Texture, kept.Users - 1);
                return;
            }
            made.Remove((bundle, seed));
            if (kept.Texture != null)
                UnityEngine.Object.Destroy(kept.Texture);
        }

        // Point filtered like the bundle's albedo, mipmapped, compressed as the bundle's textures are, then no longer readable.
        private static Texture2D Make(Sources sources, int seed, Texture? like)
        {
            Texture2D texture = new Texture2D(sources.Width, sources.Height, TextureFormat.RGBA32, true, false);
            (texture.filterMode, texture.wrapMode, texture.anisoLevel) = like != null ? (like.filterMode, like.wrapMode, like.anisoLevel) : (FilterMode.Point, TextureWrapMode.Clamp, 1);
            texture.SetPixels32(KwCrateMarks.Albedo(sources.Clean, sources.Mask, sources.Pattern, sources.Width, seed));
            texture.Apply(true, false);
            texture.Compress(false);
            texture.Apply(false, true);
            return texture;
        }

        private static Sources? Ready(string bundle)
        {
            if (read.TryGetValue(bundle, out Sources? sources))
                return sources;
            try
            {
                sources = Read(EmbeddedBundle.Load(typeof(KwCratePattern).Assembly, bundle), bundle);
            }
            catch (Exception e)
            {
                Log.Warn($"Broadside: the {bundle} crate marks could not be read; those crates all look alike: {e.Message}");
            }
            return read[bundle] = sources;
        }

        private static Sources Read(AssetBundle source, string bundle)
        {
            Texture2D clean = Texture(source, bundle + "_clean"), mask = Texture(source, bundle + "_marksmask"), pattern = Texture(source, PatternName);
            if (mask.width != clean.width || mask.height != clean.height || pattern.width != KwCrateMarks.Tile || pattern.height != KwCrateMarks.Tile)
                throw new InvalidOperationException($"sizes {clean.width}x{clean.height}, mask {mask.width}x{mask.height}, pattern {pattern.width}x{pattern.height}");
            return new Sources { Clean = clean.GetPixels32(), Mask = Channel(mask), Pattern = Channel(pattern), Width = clean.width, Height = clean.height };
        }

        private static Texture2D Texture(AssetBundle source, string name) =>
            source.LoadAsset<Texture2D>(name) ?? throw new InvalidOperationException($"no texture {name}");

        // A one-channel texture's values, bottom row first: an R8 or Alpha8 one's bytes as stored (its first mip), any other's red.
        private static byte[] Channel(Texture2D texture)
        {
            int count = texture.width * texture.height;
            if (texture.format != TextureFormat.R8 && texture.format != TextureFormat.Alpha8)
                return Array.ConvertAll(texture.GetPixels32(), c => c.r);
            byte[] raw = texture.GetRawTextureData();
            if (raw.Length < count)
                throw new InvalidOperationException($"{texture.name} holds {raw.Length} bytes, not {count}");
            Array.Resize(ref raw, count);
            return raw;
        }
    }
}
