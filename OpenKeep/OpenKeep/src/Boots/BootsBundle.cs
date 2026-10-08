using System;
using BundlePrefabs;
using UnityEngine;

namespace OpenKeep.Boots
{
    /// <summary>
    /// The embedded bundle <c>ok_boots</c> (ValheimAssets <c>Assets/Gear/SeparatedLegArmor</c>): per set a prefab
    /// <c>ok_pants_&lt;set&gt;</c> (the leggings without their boots, trousers to the ankle) and <c>ok_boots_&lt;set&gt;</c>, each with a
    /// <c>Male</c> and a <c>Female</c> SkinnedMeshRenderer weighted in the game player body's bone order with its bind
    /// poses, so the game's own armour attach fits them. Their placeholder materials carry the names the game's
    /// materials are matched by (<see cref="BootsMaterials"/>); nothing of the game's is in the bundle. Loaded once per
    /// process on every peer; a bundle that cannot load leaves the boots without a model and the leggings with their own.
    /// </summary>
    public static class BootsBundle
    {
        public const string Name = "ok_boots";

        private static AssetBundle bundle;
        private static bool tried;

        public static GameObject Pants(BootSet set) => Asset("ok_pants_" + set.BundleKey);

        public static GameObject Boots(BootSet set) => Asset("ok_boots_" + set.BundleKey);

        /// <summary>A texture of the bundle by name (the feet mask), or null.</summary>
        public static Texture2D Texture(string name)
        {
            AssetBundle loaded = Load();
            return loaded != null ? loaded.LoadAsset<Texture2D>(name) : null;
        }

        private static GameObject Asset(string name)
        {
            AssetBundle loaded = Load();
            GameObject asset = loaded != null ? loaded.LoadAsset<GameObject>(name) : null;
            if (loaded != null && asset == null)
                Plugin.Log.LogWarning($"OpenKeep: the boots bundle has no {name}");
            return asset;
        }

        private static AssetBundle Load()
        {
            if (tried)
                return bundle;
            tried = true;
            try
            {
                bundle = EmbeddedBundle.Load(typeof(BootsBundle).Assembly, Name);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"OpenKeep: the boots' models did not load; boots work but show nothing and leggings keep their own look: {e.Message}");
            }
            return bundle;
        }
    }
}
