using System.Collections.Generic;
using BepInEx.Configuration;
using EarthWright.Core;
using UnityEngine;

namespace EarthWright.Gear
{
    /// <summary>
    /// Applies the tool level settings: highest level and durability per level of the hoe and cultivator (into the
    /// prefabs and every live copy) and their upgrade costs. The game's own values of the hoe and cultivator are remembered first, so switching EarthWright off
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

        public static IEnumerable<ToolLevelSettings> Tools => new[] { GearSettings.Hoe, GearSettings.Cultivator };

        /// <summary>Applies everything to the current object database; nothing happens before one exists.</summary>
        public static void ApplyAll() => ApplyAll(ObjectDB.instance);

        /// <summary>Applies everything to this object database (the one just woken, or the current one).</summary>
        public static void ApplyAll(ObjectDB db)
        {
            if (db == null || db.m_items == null || db.m_items.Count == 0)
                return;
            foreach (ToolLevelSettings tool in Tools)
                ApplyLevels(db, tool);
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
            bool on = GeneralSettings.Enabled.Value;
            int maxQuality = on ? tool.MaxLevel.Value : original.MaxQuality;
            float perLevel = on ? tool.DurabilityPerLevel.Value : original.DurabilityPerLevel;
            ItemCopies.Copies.Apply(tool.Prefab, shared =>
            {
                shared.m_maxQuality = maxQuality;
                shared.m_durabilityPerLevel = perLevel;
            });
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

        /// <summary>Re-applies the levels and upgrade costs when their settings change (hot reload or the server's values).</summary>
        public static void WatchSettings()
        {
            Watch(GeneralSettings.Enabled);
            foreach (ToolLevelSettings tool in Tools)
            {
                Watch(tool.MaxLevel);
                Watch(tool.UpgradeCost);
                Watch(tool.DurabilityPerLevel);
            }
        }

        private static void Watch<T>(ConfigEntry<T> entry)
        {
            string name = "EarthWright tools: " + entry.Definition.Key;
            entry.SettingChanged += (_, __) => Safe.Run(name, ApplyAll);
        }
    }
}
