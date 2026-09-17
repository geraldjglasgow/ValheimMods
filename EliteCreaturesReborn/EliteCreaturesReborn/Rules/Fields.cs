namespace EliteCreaturesReborn.Rules
{
    /// <summary>
    /// The exact YAML key strings, in one place, so the parser, the defaults and the behaviours never disagree on a
    /// name. Star-power line names and every mutation-power field name the specification fixes live here.
    /// </summary>
    internal static class Fields
    {
        // star power lines
        public const string Growth = "growth";
        public const string Hp = "hp";
        public const string Attack = "attack";
        public const string SwingSpeed = "swing speed";
        public const string Speed = "speed";
        public const string Drops = "drops";

        // Mad
        public const string Move = "move";
        public const string AttackSpeed = "attack speed";
        public const string Health = "health";

        // Bloated
        public const string Delay = "delay";
        public const string Damage = "damage";
        public const string Radius = "radius";
        public const string BlastEffect = "blast effect";
        public const string WarningEffect = "warning effect";

        // Cloaked
        public const string RevealDistance = "reveal distance";
        public const string FadeTime = "fade time";
        public const string FadeMargin = "fade margin";

        // Splintering
        public const string MaxGenerations = "max generations";
        public const string MaxDescendants = "max descendants";

        // Leeching
        public const string Regen = "regen";
        public const string Lifesteal = "lifesteal";
        public const string RegenCap = "regen cap";
        public const string CombatCooldown = "combat cooldown";

        // Warding
        public const string Reflect = "reflect";
        public const string Knockback = "knockback";

        // Plated
        public const string Armour = "armour";
        public const string MaxReduction = "max reduction";

        // Miasmic
        public const string CloudLife = "cloud life";
        public const string CloudDamage = "cloud damage";
        public const string CloudsPerSecond = "clouds per second";
        public const string CloudRadius = "cloud radius";
        public const string CloudEffect = "cloud effect";
        public const string BodyEffect = "body effect";

        /// <summary>The mutation-power fields that name a vanilla prefab (a string) rather than carrying a number.</summary>
        public static bool IsPrefabField(string field) =>
            field == CloudEffect || field == BodyEffect || field == BlastEffect || field == WarningEffect;

        // Devouring
        public const string AbsorbHealth = "absorb health";
        public const string AbsorbDamage = "absorb damage";
        public const string SlowPer100Health = "slow per 100 health";
        public const string PlayerThreshold = "player threshold";
        public const string DevourCooldown = "devour cooldown";
    }
}
