using System;
using System.Collections.Generic;
using BundlePrefabs;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The casters' workshop bundles (features/casters.md, "Bundles and what each holds"), each loaded once, embedded per
    /// platform. A bundle that is missing or does not load is logged once and reads as null: what it would dress keeps the
    /// game item's look (or, for an icon or effect, goes without).
    /// </summary>
    public static class CasterBundles
    {
        public const string Spells = "ee_spells_meadows";
        public const string Effects = "ee_casterfx_meadows";

        private static readonly Dictionary<string, AssetBundle> loaded = new Dictionary<string, AssetBundle>();

        public static AssetBundle Get(string name)
        {
            if (loaded.TryGetValue(name, out AssetBundle bundle))
                return bundle;
            try
            {
                bundle = EmbeddedBundle.Load(typeof(CasterBundles).Assembly, name);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"EliteEquipment: the casters' bundle {name} is missing: {e.Message}");
                bundle = null;
            }
            loaded[name] = bundle;
            return bundle;
        }

        public static GameObject Prefab(string bundle, string asset)
        {
            AssetBundle source = Get(bundle);
            GameObject prefab = source != null ? source.LoadAsset<GameObject>(asset) : null;
            if (source != null && prefab == null)
                Plugin.Log.LogError($"EliteEquipment: the bundle {bundle} has no {asset}");
            return prefab;
        }

        public static Texture2D Texture(string bundle, string asset)
        {
            AssetBundle source = Get(bundle);
            Texture2D texture = source != null ? source.LoadAsset<Texture2D>(asset) : null;
            if (source != null && texture == null)
                Plugin.Log.LogWarning($"EliteEquipment: the bundle {bundle} has no texture {asset}");
            return texture;
        }

        public static Sprite Sprite(string bundle, string asset)
        {
            AssetBundle source = Get(bundle);
            Sprite sprite = source != null ? source.LoadAsset<Sprite>(asset) : null;
            if (source != null && sprite == null)
                Plugin.Log.LogWarning($"EliteEquipment: the bundle {bundle} has no sprite {asset}");
            return sprite;
        }
    }
}
