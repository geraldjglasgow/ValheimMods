using System.Collections.Generic;

namespace Broadside
{
    /// <summary>
    /// What one pull of a weapon's trigger does: the ammunition it takes from the gunner, the reload before the next,
    /// the projectiles it launches from the muzzle (a copy of a game projectile, <c>AL_shot_&lt;name&gt;</c>, built by
    /// <see cref="KwShots"/> and launched by <see cref="KwShooter"/>), how they fly and hit, what bursts where they land,
    /// and the game effects played at the muzzle on every peer that sees it.
    /// </summary>
    public sealed class KwShot
    {
        public KwShot(string name) => Name = name;

        /// <summary>Unique among shots: the projectile prefab is <c>AL_shot_</c> + this.</summary>
        public string Name { get; }
        public string Prefab => KwShots.Prefix + Name;

        /// <summary>The game projectile copied (its look, trail and hit effects): the ballista's bolt by default.</summary>
        public string Projectile { get; set; } = "Turret_projectile";

        /// <summary>A cannon item whose model the projectile wears in flight in place of the game's (its trail and effects kept); empty: the game's look.</summary>
        public string Wears { get; set; } = "";

        /// <summary>Metres a second; metres a second squared downward; scatter in degrees each way; seconds aloft.</summary>
        public float Speed { get; set; } = 60f;
        public float Gravity { get; set; }
        public float Spread { get; set; } = 0.5f;
        public float Life { get; set; } = 8f;

        /// <summary>Projectiles a pull launches, each scattered on its own (a volley, a load of grape).</summary>
        public int Count { get; set; } = 1;

        public HitData.DamageTypes Damage { get; set; }
        public float Push { get; set; } = 40f;
        public float Stagger { get; set; } = 1f;

        /// <summary>
        /// Times the projectile's own hit is as hard against a building piece or a ship (anything with the game's wear,
        /// <c>WearNTear</c>) as against a creature or a player (<see cref="KwShotHits"/>); its burst hits all alike.
        /// </summary>
        public float Structures { get; set; } = 1f;

        /// <summary>What a hit can break, as a tool's tier does (rocks, trees, ore).</summary>
        public int ToolTier { get; set; }
        public Skills.SkillType Skill { get; set; } = Skills.SkillType.Crossbows;

        /// <summary>A status effect a hit puts on, by its ObjectDB name ("Burning", "Poison", "Frost", "Tared"); empty for none.</summary>
        public string Status { get; set; } = "";

        /// <summary>Whether the projectile stops at the sea's surface (bursting there) rather than flying on under it.</summary>
        public bool HitsWater { get; set; } = true;

        /// <summary>
        /// Whether it goes on under the water instead (a cannonball, <see cref="KwShotWater"/>): splashing as it enters,
        /// keeping its speed for a metre, then slowed hard and sinking, what it strikes on its way hit by the speed it has
        /// left, its burst only above water.
        /// </summary>
        public bool Dives { get; set; }
        public KwBurst? Burst { get; set; }

        /// <summary>Game effects where a shot meets the sea (networked, as the game's hit effects are); none: it just goes.</summary>
        public List<string> Splash { get; } = new List<string> { "vfx_WaterImpact_Raft" };

        /// <summary>Ammunition item prefab names, the first the gunner carries enough of is used; none: free.</summary>
        public List<string> Ammo { get; } = new List<string>();
        public int AmmoPerShot { get; set; } = 1;

        /// <summary>Items every pull also takes besides its ammunition, each so many and all of them needed (a cannon's powder); none: the ammunition alone.</summary>
        public List<(string Item, int Amount)> Charge { get; } = new List<(string Item, int Amount)>();

        /// <summary>Seconds before the weapon fires again.</summary>
        public float Reload { get; set; } = 3f;

        /// <summary>Whether holding the button fires again as soon as it has reloaded.</summary>
        public bool Repeats { get; set; }

        /// <summary>Game effect prefabs (sound, smoke, flash) played at the muzzle, facing along it, local on every peer.</summary>
        public List<string> Effects { get; } = new List<string> { "fx_turret_fire" };
        public float EffectScale { get; set; } = 1f;

        /// <summary>Last touches to the projectile prefab once it is made, with that prefab (a field of the game's projectile the shot has no word for); none by default.</summary>
        public System.Action<UnityEngine.GameObject>? Finish { get; set; }
    }

    /// <summary>
    /// What a shot leaves where it lands: an area hit (a copy of a game area effect, its radius, damage and status), or
    /// fragments (a shot of their own, so many, thrown off at a speed between two, scattered from the flight's way toward
    /// any way by 0 to 1, always away from the surface hit). One or the other: fragments win when both are set.
    /// </summary>
    public sealed class KwBurst
    {
        public const string Prefix = "BS_burst_";

        /// <summary>The game area effect copied for an area hit (its look and sound): the catapult's impact by default.</summary>
        public string Area { get; set; } = "Catapult_Ammo_Projectile_AOE";
        public float Radius { get; set; } = 3f;
        public HitData.DamageTypes Damage { get; set; }
        public string Status { get; set; } = "";
        public float Push { get; set; }

        /// <summary>Whether the area also hurts the gunner's friends, as the game's bombs do.</summary>
        public bool HitsFriends { get; set; }

        public KwShot? Fragments { get; set; }
        public int FragmentCount { get; set; } = 6;
        public float FragmentSpeedMin { get; set; } = 8f;
        public float FragmentSpeedMax { get; set; } = 16f;
        public float FragmentScatter { get; set; } = 0.6f;
    }
}
