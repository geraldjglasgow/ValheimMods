using BepInEx.Configuration;
using UnityEngine;

namespace EliteCreaturesReborn.Config
{
    /// <summary>
    /// The horde's four settings (features/horde.md): more creatures in the wild, each weaker. Requested on Discord; the
    /// user chose an on/off and three multipliers. They change the game, so like the raids' and the arena's switches they
    /// are not a player's own: <see cref="Rules.ServerLock"/> registers them with the mod's Charter, and while the server
    /// binds its players (the .cfg's `Lock to server`) the server's values hold for every player. The loot share is
    /// no setting: a horde creature drops 1 / the spawn multiplier of its own loot, so the extra creatures pay no extra.
    /// </summary>
    public static class HordeSettings
    {
        public const string Section = "13 - Horde";

        public static ConfigEntry<bool> Horde = null!;
        public static ConfigEntry<float> SpawnMultiplier = null!;
        public static ConfigEntry<float> HealthMultiplier = null!;
        public static ConfigEntry<float> AttackMultiplier = null!;

        /// <summary>True while the horde is on; the server's value while it binds.</summary>
        public static bool Enabled => Horde != null && Horde.Value;

        /// <summary>The spawn multiplier, 1 to 3.</summary>
        public static float Spawns => SpawnMultiplier != null ? Mathf.Clamp(SpawnMultiplier.Value, 1f, 3f) : 1f;

        /// <summary>A horde creature's health multiplier, 0.1 to 2.</summary>
        public static float Health => HealthMultiplier != null ? Mathf.Clamp(HealthMultiplier.Value, 0.1f, 2f) : 1f;

        /// <summary>A horde creature's attack multiplier, 0.1 to 2.</summary>
        public static float Attack => AttackMultiplier != null ? Mathf.Clamp(AttackMultiplier.Value, 0.1f, 2f) : 1f;

        public static void Bind(ConfigFile config)
        {
            Horde = config.Bind(Section, "Horde", false,
                "More creatures in the wild, each weaker: the world's own spawns come in groups the spawn multiplier "
                + "larger, with that many more allowed around you, and every creature they bring has the health and attack "
                + "multipliers below and drops 1 / the spawn multiplier of its loot (half at 2), so the extra creatures pay "
                + "no extra. Each one still rolls its own stars and mutations. Bosses, the game's raids, this mod's raids "
                + "and arena, creature spawners in camps and dungeons, and tamed creatures are left as they are. A "
                + "creature's health follows a change the next time it loads; its attack and loot at once. Set on the "
                + "server: while it binds its players ('Lock to server' in 1 - General), the server's values hold for "
                + "everyone.");
            SpawnMultiplier = config.Bind(Section, "Spawn Multiplier", 2f, new ConfigDescription(
                "How many times as many creatures the world spawns while Horde is on: group sizes and the most of each kind "
                + "around you, rounded up or down at random so 1.5 averages one and a half times as many.",
                new AcceptableValueRange<float>(1f, 3f)));
            HealthMultiplier = config.Bind(Section, "Creature Health Multiplier", 0.65f, new ConfigDescription(
                "A horde creature's health while Horde is on, times its starred health: 0.65 is 65%.",
                new AcceptableValueRange<float>(0.1f, 2f)));
            AttackMultiplier = config.Bind(Section, "Creature Attack Multiplier", 0.75f, new ConfigDescription(
                "The damage a horde creature deals while Horde is on, times its starred damage: 0.75 is 75%.",
                new AcceptableValueRange<float>(0.1f, 2f)));
        }
    }
}
