using System;
using System.Collections.Generic;
using BundlePrefabs;
using EliteCreaturesReborn.Util;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Hall
{
    /// <summary>
    /// The arena's embedded bundles, one per platform: Windows for clients, Linux for the dedicated server, loaded by
    /// BundlePrefabs' EmbeddedBundle once per process and kept. The outdoor arena's floor, walls, cover, land, cave and
    /// river (the river bundle also holds the bubble and mist effects) (ValheimAssets <c>Assets/Locations/HelArena</c>,
    /// its CONTRACT.md), the indoor hall's throne
    /// (<c>Assets/Locations/HelHall</c>), and Hel with her spear (<c>Assets/Creatures/Hel</c>). Each piece's prefab carries
    /// its bundle's name; Hel's bundle holds her and her spear. A bundle that will not load is logged once.
    /// </summary>
    internal static class HallBundles
    {
        public const string Floor = "ecr_helarena_floor";
        public const string Walls = "ecr_helarena_walls";
        public const string Cover = "ecr_helarena_cover";
        public const string Land = "ecr_helarena_land";
        public const string Cave = "ecr_helarena_cave";
        public const string River = "ecr_helarena_river";
        public const string Bubble = "ecr_helarena_bubble";
        public const string RiverMist = "ecr_helarena_rivermist";
        public const string Anvil = "ecr_helarena_anvil";
        public const string Throne = "ecr_helhall_throne";
        public const string Hel = "ecr_hel";
        public const string Spear = "ecr_hel_spear";

        private static readonly HashSet<string> Failed = new HashSet<string>();
        private static string[]? _resources;

        /// <summary>A prefab of an embedded bundle; null (logged once) when the bundle or the prefab is missing.</summary>
        public static GameObject? Prefab(string bundle, string asset)
        {
            if (Failed.Contains(bundle))
            {
                return null;
            }
            try
            {
                return EmbeddedBundle.Prefab(EmbeddedBundle.Load(typeof(HallBundles).Assembly, bundle), asset);
            }
            catch (Exception e)
            {
                Failed.Add(bundle);
                Log.Error($"Hel's arena: {asset} did not load from {bundle}: {e.Message}");
                return null;
            }
        }

        /// <summary>An embedded bundle, for its effects (BundleEffects); null (logged once) when it will not load.</summary>
        public static AssetBundle? Load(string bundle)
        {
            if (Failed.Contains(bundle))
            {
                return null;
            }
            try
            {
                return EmbeddedBundle.Load(typeof(HallBundles).Assembly, bundle);
            }
            catch (Exception e)
            {
                Failed.Add(bundle);
                Log.Error($"Hel's arena: the bundle {bundle} did not load: {e.Message}");
                return null;
            }
        }

        /// <summary>True when the mod embeds the bundle for some platform (an asset not yet shipped is not an error).</summary>
        public static bool Embedded(string bundle)
        {
            _resources ??= typeof(HallBundles).Assembly.GetManifestResourceNames();
            string prefix = "." + bundle + ".";
            foreach (string resource in _resources)
            {
                if (resource.IndexOf(prefix, StringComparison.Ordinal) >= 0)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
