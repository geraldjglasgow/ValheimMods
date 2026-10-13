using System;
using System.Collections.Generic;
using BundlePrefabs;
using EliteCreaturesReborn.Util;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Hall
{
    /// <summary>
    /// The hall's embedded bundles (ValheimAssets <c>Assets/Locations/HelHall</c> and <c>Assets/Creatures/Hel</c>), one
    /// per platform: Windows for clients, Linux for the dedicated server, loaded by BundlePrefabs' EmbeddedBundle once per
    /// process and kept. Each hall piece's prefab carries its bundle's name; Hel's bundle holds her and her spear. A
    /// bundle that will not load is logged once and the hall is not built.
    /// </summary>
    internal static class HallBundles
    {
        public const string Floor = "ecr_helhall_floor";
        public const string Walls = "ecr_helhall_walls";
        public const string Throne = "ecr_helhall_throne";
        public const string Ceiling = "ecr_helhall_ceiling";
        public const string Hel = "ecr_hel";
        public const string Spear = "ecr_hel_spear";

        private static readonly HashSet<string> Failed = new HashSet<string>();

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
                Log.Error($"Hel's hall: {asset} did not load from {bundle}: {e.Message}");
                return null;
            }
        }
    }
}
