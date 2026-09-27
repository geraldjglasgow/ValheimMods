using System.Collections.Generic;
using EarthWright.Core;
using UnityEngine;

namespace EarthWright.Gear
{
    /// <summary>
    /// Applies the tool level settings: highest level and durability per level of the hoe, cultivator and shovel (into
    /// the prefabs and every live copy), the shovel's durability and wear, the shovel recipe, and the hoe and cultivator
    /// upgrade costs. The game's own values of the hoe and cultivator are remembered first, so switching EarthWright off
    /// restores them. Runs on every machine whenever an object database is ready and whenever a setting changes, so client
    /// and server agree on what can be upgraded.
    /// </summary>
    public static class ToolLevels
    {
        /// <summary>The game's own level values of a tool, as first seen, restored while EarthWright is off.</summary>
        private sealed class GameValues
        {
            public int MaxQuality;
            public float DurabilityPerLevel;
        }

        private static readonly Dictionary<string, GameValues> game = new Dictionary<string, GameValues>();

        public static IEnumerable<ToolLevelSettings> Tools => new[] { GearSettings.Hoe, GearSettings.Cultivator, GearSettings.Shovel };

        /// <summary>Applies everything to the current object database; nothing happens before one exists.</summary>
        public static void ApplyAll() => ApplyAll(ObjectDB.instance);

        /// <summary>Applies everything to this object database (the one just woken, or the current one).</summary>
        public static void ApplyAll(ObjectDB db)
        {
            if (db == null || db.m_items == null || db.m_items.Count == 0)
                return;
            foreach (ToolLevelSettings tool in Tools)
                ApplyLevels(db, tool);
            ShovelRecipe.Apply(db);
            ToolRecipes.ApplyUpgradeCost(db, GearSettings.Hoe);
            ToolRecipes.ApplyUpgradeCost(db, GearSettings.Cultivator);
        }

        private static void ApplyLevels(ObjectDB db, ToolLevelSettings tool)
        {
            GameObject prefab = db.GetItemPrefab(tool.Prefab);
            ItemDrop drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            if (drop == null)
                return;
            GameValues original = Remember(tool.Prefab, drop.m_itemData.m_shared);
            // The shovel is EarthWright's own item: its settings apply even with the master switch off.
            bool own = GeneralSettings.Enabled.Value || tool.Prefab == ToolNames.Shovel;
            int maxQuality = own ? tool.MaxLevel.Value : original.MaxQuality;
            float perLevel = own ? tool.DurabilityPerLevel.Value : original.DurabilityPerLevel;
            bool shovel = tool.Prefab == ToolNames.Shovel;
            ItemCopies.Copies.Apply(tool.Prefab, shared =>
            {
                shared.m_maxQuality = maxQuality;
                shared.m_durabilityPerLevel = perLevel;
                if (shovel)
                    WriteShovel(shared);
            });
        }

        private static void WriteShovel(ItemDrop.ItemData.SharedData shared)
        {
            shared.m_maxDurability = GearSettings.ShovelMaxDurability.Value;
            shared.m_useDurabilityDrain = GearSettings.ShovelWearPerUse.Value;
            shared.m_useDurability = true;
            shared.m_canBeReparied = true;
        }

        private static GameValues Remember(string prefab, ItemDrop.ItemData.SharedData shared)
        {
            if (!game.TryGetValue(prefab, out GameValues values))
            {
                values = new GameValues { MaxQuality = shared.m_maxQuality, DurabilityPerLevel = shared.m_durabilityPerLevel };
                game[prefab] = values;
            }
            return values;
        }
    }
}
