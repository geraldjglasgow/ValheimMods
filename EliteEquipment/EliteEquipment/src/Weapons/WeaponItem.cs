using BundlePrefabs;
using UnityEngine;

using SharedData = ItemDrop.ItemData.SharedData;

namespace EliteEquipment.Weapons
{
    /// <summary>
    /// One new weapon item prefab: a copy of its base game weapon (net view, rigidbody, attacks, animations, stamina, speed,
    /// hit and swing effects, trail, quality levels, durability, weight), wearing the workshop model in the look item's own
    /// material (<see cref="WeaponModel"/>, <see cref="WeaponPair"/> or <see cref="WeaponFist"/>), with its own name, icon
    /// and damage. Per-level damage is the base's scaled by the same ratio as the base damage.
    /// </summary>
    public static class WeaponItem
    {
        public static GameObject Build(WeaponSet set, GameObject game, GameObject look, AssetBundle bundle)
        {
            GameObject item = PrefabBench.Copy(game, set.Prefab);
            GameObject model = EmbeddedBundle.Prefab(bundle, set.Asset);
            Material material = WeaponParts.Material(look, set);
            if (set.Kind == WeaponKind.Dual)
                WeaponPair.Wear(item, model, material, set);
            else if (set.Kind == WeaponKind.Fist)
                WeaponFist.Wear(item, model, material, set);
            else
                WeaponModel.Wear(item, model, material, set);
            set.Projectile = WeaponProjectile.Build(set, item, model, material);
            SharedData shared = item.GetComponent<ItemDrop>().m_itemData.m_shared;
            Describe(shared, set, bundle);
            Damage(shared, set);
            return item;
        }

        private static void Describe(SharedData shared, WeaponSet set, AssetBundle bundle)
        {
            shared.m_name = set.Token;
            shared.m_description = set.Token + "_desc";
            Sprite icon = bundle.LoadAsset<Sprite>(set.Asset + "_icon");
            if (icon != null)
                shared.m_icons = new[] { icon };
            else
                Plugin.Log.LogWarning($"EliteEquipment: the bundle has no {set.Asset}_icon; the {set.Name} shows the {set.Base} icon");
        }

        private static void Damage(SharedData shared, WeaponSet set)
        {
            float ratio = set.Damage.GetTotalDamage() / Mathf.Max(1f, shared.m_damages.GetTotalDamage());
            HitData.DamageTypes perLevel = shared.m_damagesPerLevel.Clone();
            perLevel.Modify(ratio);
            shared.m_damagesPerLevel = perLevel;
            shared.m_damages = set.Damage;
        }
    }
}
