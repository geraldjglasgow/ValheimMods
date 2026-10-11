using System.Collections.Generic;

namespace EliteCreaturesPack.Custom.Definitions
{
    /// <summary>
    /// `character:` who the creature is: its name, health, speeds, faction, whether it is a boss and the boss fight it
    /// brings, its resistances, what hurts it and whether a blocked attack staggers it. Applied by the character step
    /// (Custom/Nature). Every value null keeps the base's.
    /// </summary>
    public sealed class CharacterBlock
    {
        /// <summary>`display name`: the name players see (plain text, or a <c>$word</c> of the game's translations).</summary>
        public string? DisplayName;

        /// <summary>`health`: health at one star, 1 to 1,000,000 (stars multiply it as the game does).</summary>
        public float? Health;

        /// <summary>`speed scale`: multiplies every speed of the base (walk, normal, run, turn, swim, fly), 0.05 to 20;
        /// the speeds below then set single ones outright.</summary>
        public float? SpeedScale;

        /// <summary>`walk speed`: metres a second walking, 0 to 100 (Character.m_walkSpeed).</summary>
        public float? WalkSpeed;

        /// <summary>`speed`: its normal pace in metres a second, 0 to 100 (Character.m_speed).</summary>
        public float? Speed;

        /// <summary>`run speed`: metres a second running, 0 to 100 (Character.m_runSpeed).</summary>
        public float? RunSpeed;

        /// <summary>`turn speed`: degrees a second turning, walking and running alike, 0 to 10,000.</summary>
        public float? TurnSpeed;

        /// <summary>`swim speed`: metres a second swimming, 0 to 100.</summary>
        public float? SwimSpeed;

        /// <summary>`fly speed`: a flyer's metres a second when not hurrying, 0 to 100 (Character.m_flySlowSpeed).</summary>
        public float? FlySpeed;

        /// <summary>`fly fast speed`: a flyer's metres a second when hurrying, 0 to 100 (Character.m_flyFastSpeed).</summary>
        public float? FlyFastSpeed;

        /// <summary>`faction`: one of the game's factions (players, animals veg, forest monsters, undead, demon, mountain
        /// monsters, sea monsters, plains monsters, boss, mistlands monsters, dverger, player spawned, training dummy,
        /// deep north). With players it fights monsters instead of players.</summary>
        public Character.Faction? Faction;

        /// <summary>`boss`: a boss (health bar at the top of the screen, boss rules everywhere).</summary>
        public bool? Boss;

        /// <summary>`boss fight`: the name of one of the game's boss events (like <c>boss_eikthyr</c>) whose weather and
        /// music play while it is the boss on screen.</summary>
        public string? BossFight;

        /// <summary>`resistances:` a map of damage type to normal, weak, very weak, slightly weak, resistant, very
        /// resistant, slightly resistant, immune or ignore. Types left out keep the base's.</summary>
        public Dictionary<DamageKind, HitData.DamageModifier>? Resistances;

        /// <summary>`hurt by water`: water hurts it (Character.m_tolerateWater false).</summary>
        public bool? HurtByWater;

        /// <summary>`hurt by fire`: standing in fire hurts it (Character.m_tolerateFire false).</summary>
        public bool? HurtByFire;

        /// <summary>`hurt by smoke`: smoke hurts it (Character.m_tolerateSmoke false).</summary>
        public bool? HurtBySmoke;

        /// <summary>`staggers when blocked`: it staggers when its attack is blocked (Character.m_staggerWhenBlocked).</summary>
        public bool? StaggersWhenBlocked;
    }

    /// <summary>`progress:` what its death and arrival mean to the world. Applied by the character step.</summary>
    public sealed class ProgressBlock
    {
        /// <summary>`key on death`: a world progress (global) key set when it dies, for raids and other mods.</summary>
        public string? KeyOnDeath;

        /// <summary>`spawn message`: shown to nearby players when it spawns.</summary>
        public string? SpawnMessage;

        /// <summary>`death message`: shown to nearby players when it dies.</summary>
        public string? DeathMessage;
    }
}
