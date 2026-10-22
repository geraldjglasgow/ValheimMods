using System.Collections.Generic;
using BundlePrefabs;
using UnityEngine;

using SharedData = ItemDrop.ItemData.SharedData;

namespace EliteEquipment.Shields
{
    /// <summary>
    /// One new shield item prefab: a copy of the game item it is based on (net view, rigidbody, block and bash sounds and
    /// effects, quality levels, equip timing, its place on the arm and on the back), wearing the workshop's model
    /// (<see cref="ShieldModel"/>), with its own name, icon and numbers. Still a Shield item, so the game, tooltips and
    /// other mods treat it as one.
    /// </summary>
    public static class ShieldItem
    {
        private const float BlockPerLevel = 6f;
        private const float DurabilityPerLevel = 50f;

        public static GameObject Build(ShieldSet set, GameObject game, GameObject look, AssetBundle bundle)
        {
            GameObject item = PrefabBench.Copy(game, set.Prefab);
            ShieldModel.Wear(item, game, look, EmbeddedBundle.Prefab(bundle, set.Asset), set);
            SharedData shared = item.GetComponent<ItemDrop>().m_itemData.m_shared;
            Describe(shared, set, bundle);
            Stats(shared, set);
            return item;
        }

        private static void Describe(SharedData shared, ShieldSet set, AssetBundle bundle)
        {
            shared.m_name = set.Token;
            shared.m_description = set.DescriptionToken;
            shared.m_variants = 0;
            Sprite icon = bundle.LoadAsset<Sprite>(set.Asset + "_icon");
            if (icon != null)
                shared.m_icons = new[] { icon };
            else
                Plugin.Log.LogWarning($"EliteEquipment: the bundle has no {set.Asset}_icon; the {set.Prefab} shows the {set.Base} icon");
        }

        private static void Stats(SharedData shared, ShieldSet set)
        {
            shared.m_blockPower = set.Block;
            shared.m_blockPowerPerLevel = BlockPerLevel;
            shared.m_deflectionForce = set.Deflection;
            shared.m_deflectionForcePerLevel = set.DeflectionPerLevel;
            shared.m_timedBlockBonus = set.Kind.Parry;
            shared.m_perfectBlockAdrenaline = set.Kind.Adrenaline;
            shared.m_movementModifier = set.Kind.Movement;
            shared.m_maxDurability = set.Durability;
            shared.m_durabilityPerLevel = DurabilityPerLevel;
            shared.m_weight = set.Weight;
            shared.m_damageModifiers = new List<HitData.DamageModPair>();
        }
    }
}
