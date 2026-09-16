using UnityEngine;

namespace EliteCreaturesReborn.Rules
{
    /// <summary>
    /// How the world repopulates: camp spawners, dungeon spawners and dungeon loot chests, each with its own timer in
    /// world days. All three are off by default. This feature can quietly undo a player's sense of progress if it runs
    /// too fast, so a camp is worth clearing again after real days rather than hours, and loot is slower still.
    /// </summary>
    public sealed class RespawnRules
    {
        public bool Camps;
        public float CampDays = 5f;
        public bool Dungeons;
        public float DungeonDays = 7f;
        public bool DungeonLoot;
        public float DungeonLootDays = 14f;

        /// <summary>A world day is 1440 world minutes, which is the unit the game's own spawner timer counts in.</summary>
        public const float MinutesPerDay = 1440f;

        /// <summary>The spawner timer in the game's own unit, or 0 when this kind of spawner is left alone.</summary>
        public float MinutesFor(bool inDungeon)
        {
            if (inDungeon)
            {
                return Dungeons ? Mathf.Max(0f, DungeonDays) * MinutesPerDay : 0f;
            }
            return Camps ? Mathf.Max(0f, CampDays) * MinutesPerDay : 0f;
        }

        public RespawnRules Clone()
        {
            return new RespawnRules
            {
                Camps = Camps, CampDays = CampDays, Dungeons = Dungeons,
                DungeonDays = DungeonDays, DungeonLoot = DungeonLoot, DungeonLootDays = DungeonLootDays,
            };
        }
    }
}
