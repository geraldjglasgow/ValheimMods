using BepInEx.Configuration;
using EliteCreaturesReborn.Rules;
using UnityEngine;

namespace EliteCreaturesReborn.Config
{
    /// <summary>
    /// Bringing back what the game places once: camp spawners, dungeon spawners and dungeon loot chests, each on its own
    /// timer in world days, all off by default. the rule file's old `respawning:` (<see cref="RuleSettings"/>);
    /// the parser builds each rule set's <see cref="RespawnRules"/> from these.
    /// </summary>
    public static class RespawnSettings
    {
        public const string Section = "3 - Respawning";
        private const string Days = " World days; a world day is 30 real minutes at the game's default speed.";

        public static ConfigEntry<bool> Camps = null!;
        public static ConfigEntry<float> CampDays = null!;
        public static ConfigEntry<bool> Dungeons = null!;
        public static ConfigEntry<float> DungeonDays = null!;
        public static ConfigEntry<bool> DungeonLoot = null!;
        public static ConfigEntry<float> DungeonLootDays = null!;

        public static void Bind(ConfigFile config)
        {
            Camps = config.Bind(Section, "Camps", false,
                "Cleared places out in the world fill up again: Fuling and Draugr villages, ruins, towers, camps and "
                + "the like. Only their creatures come back, never buildings, nests, rocks or pickables." + RuleSettings.ServerNote);
            CampDays = config.Bind(Section, "Camp days", 5f, "How long a camp's spawner waits after its creature died." + Days
                + RuleSettings.ServerNote);
            Dungeons = config.Bind(Section, "Dungeons", false,
                "Cleared dungeons behind a loading door fill up again: crypts, caves, mines." + RuleSettings.ServerNote);
            DungeonDays = config.Bind(Section, "Dungeon days", 7f, "How long a dungeon's spawner waits after its creature "
                + "died." + Days + RuleSettings.ServerNote);
            BindLoot(config);
            WatchAll();
        }

        private static void BindLoot(ConfigFile config)
        {
            DungeonLoot = config.Bind(Section, "Dungeon loot", false,
                "A dungeon chest refills from its own loot table once it has been fully emptied. Chests out in the world "
                + "are never refilled." + RuleSettings.ServerNote);
            DungeonLootDays = config.Bind(Section, "Dungeon loot days", 14f, "How long an emptied dungeon chest waits "
                + "before it refills." + Days + RuleSettings.ServerNote);
        }

        private static void WatchAll()
        {
            RuleSettings.Watch(Camps);
            RuleSettings.Watch(CampDays);
            RuleSettings.Watch(Dungeons);
            RuleSettings.Watch(DungeonDays);
            RuleSettings.Watch(DungeonLoot);
            RuleSettings.Watch(DungeonLootDays);
        }

        /// <summary>The timers in force; the server's while it binds. A negative day count counts as 0.</summary>
        public static RespawnRules Rules()
        {
            if (Camps == null)
            {
                return new RespawnRules();
            }
            return new RespawnRules
            {
                Camps = Camps.Value, CampDays = Mathf.Max(0f, CampDays.Value),
                Dungeons = Dungeons.Value, DungeonDays = Mathf.Max(0f, DungeonDays.Value),
                DungeonLoot = DungeonLoot.Value, DungeonLootDays = Mathf.Max(0f, DungeonLootDays.Value),
            };
        }
    }
}
