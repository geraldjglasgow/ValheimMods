namespace GrindstoneSkills.Api
{
    /// <summary>
    /// Sailing for other mods, read by reflection (ShipConfig's ship panel does): plain types only, so a caller
    /// references nothing of GrindstoneSkills. Every value is the local player's, on this machine. Main thread only.
    /// Later versions only add endpoints.
    /// </summary>
    public static class SailingApi
    {
        public static int GetApiVersion() => 1;

        /// <summary>How much faster the helmsman's Sailing makes this ship at top speed: 1.2 is 20% faster; 1 without a helmsman or with Sailing off.</summary>
        public static float GetShipSpeedFactor(Ship ship) => ship != null ? HelmSpeed.SpeedFactor(ship) : 1f;

        /// <summary>The factor on the local player's map exploration radius while aboard a ship: 2 doubles it; 1 with Sailing off.</summary>
        public static float GetExploreRadiusFactor() => SeaExploration.RadiusFactor();

        /// <summary>The ids of the Sailing actives the local player has unlocked ("windcall", "lookout").</summary>
        public static string[] GetAbilities() => SailingAbilities.Unlocked();

        public static string GetAbilityName(string id) => SailingAbilities.Name(id);

        /// <summary>The local player's key for it, short ("K", "Shift+O"); empty when unbound.</summary>
        public static string GetAbilityKey(string id) => SailingAbilities.Key(id);

        /// <summary>What it does, one or two sentences with the current settings' numbers.</summary>
        public static string GetAbilityDescription(string id) => SailingAbilities.Description(id);

        /// <summary>Seconds until the local player may use it again; 0 when ready.</summary>
        public static float GetAbilityCooldown(string id) => SailingAbilities.CooldownLeft(id);

        /// <summary>The whole cooldown in seconds, from the current settings.</summary>
        public static float GetAbilityCooldownLength(string id) => SailingAbilities.CooldownLength(id);
    }
}
