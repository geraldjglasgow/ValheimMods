using System.Collections.Generic;
using UnityEngine;

namespace EliteCrafting.Items
{
    /// <summary>One registered stone (or shard) prefab and what the mod keeps about it. Built once per process.</summary>
    internal sealed class StoneEntry
    {
        public StoneEntry(string prefabName, string? builtInId, StoneGroup group)
        {
            PrefabName = prefabName;
            BuiltInId = builtInId;
            Group = group;
        }

        public string PrefabName { get; }

        /// <summary>The built-in stone id, or null for a reserved <c>ECF_CustomNN</c> prefab and for a shard.</summary>
        public string? BuiltInId { get; }

        /// <summary>The shard id (salvage.md section 5) when this prefab is a shard, not a stone; else null.</summary>
        public string? ShardId { get; set; }

        public bool IsShard => ShardId != null;

        public StoneGroup Group { get; }
        public GameObject Prefab { get; set; } = null!;

        /// <summary>The prefab's own shared data; every live copy of the stone is linked to this one instance.</summary>
        public ItemDrop.ItemData.SharedData Shared { get; set; } = null!;

        /// <summary>The base item's icon, untinted (the fallback when tinting is impossible).</summary>
        public Sprite? BaseIcon { get; set; }

        /// <summary>Client-only visual state (materials, lights, scale); null on a dedicated server.</summary>
        public StoneLook? Look { get; set; }
    }

    /// <summary>
    /// Builds one stone prefab from a vanilla base (prefabs.md section 2): instantiated under an inactive parent so no
    /// Awake runs (no ZDO, no live item list entry), named without spaces, its own shared data reset to a plain
    /// material item. The YAML-driven values (name, stack, weight) are written later by <see cref="StoneItemData"/>.
    /// Every peer, identically.
    /// </summary>
    internal static class StoneCloner
    {
        public static void Build(StoneEntry entry, GameObject basePrefab, Transform holder)
        {
            GameObject clone = Object.Instantiate(basePrefab, holder, false);
            clone.name = entry.PrefabName;
            ItemDrop drop = clone.GetComponent<ItemDrop>();
            ItemDrop.ItemData data = drop.m_itemData;
            data.m_dropPrefab = clone;
            data.m_customData = new Dictionary<string, string>();
            data.m_stack = 1;
            data.m_quality = 1;
            data.m_variant = 0;
            entry.Prefab = clone;
            entry.Shared = data.m_shared;
            entry.BaseIcon = FirstIcon(basePrefab);
            ResetShared(data.m_shared, entry.BaseIcon);
        }

        private static Sprite? FirstIcon(GameObject basePrefab)
        {
            Sprite[]? icons = basePrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_icons;
            return icons != null && icons.Length > 0 ? icons[0] : null;
        }

        // A stone is a plain stackable material: no trade value, teleportable, no effects, no food, no set.
        private static void ResetShared(ItemDrop.ItemData.SharedData shared, Sprite? icon)
        {
            shared.m_itemType = ItemDrop.ItemData.ItemType.Material;
            shared.m_icons = icon != null ? new[] { icon } : System.Array.Empty<Sprite>();
            shared.m_subtitle = "";
            shared.m_dlc = "";
            shared.m_questItem = false;
            shared.m_value = 0;
            shared.m_teleportable = true;
            shared.m_autoStack = true;
            shared.m_maxQuality = 1;
            shared.m_scaleByQuality = 0f;
            shared.m_variants = 0;
            shared.m_setName = "";
            shared.m_setSize = 0;
            ClearEffects(shared);
        }

        private static void ClearEffects(ItemDrop.ItemData.SharedData shared)
        {
            shared.m_consumeStatusEffect = null;
            shared.m_equipStatusEffect = null;
            shared.m_setStatusEffect = null;
            shared.m_appendToolTip = null;
            shared.m_food = 0f;
            shared.m_foodStamina = 0f;
            shared.m_foodEitr = 0f;
            shared.m_foodBurnTime = 0f;
            shared.m_foodRegen = 0f;
            shared.m_isDrink = false;
        }
    }
}
