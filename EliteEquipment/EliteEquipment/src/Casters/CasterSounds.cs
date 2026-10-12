namespace EliteEquipment.Casters
{
    /// <summary>
    /// The game sound played with each caster effect (the game's own recordings, by sound prefab name, picked 2026-10-09
    /// from the game's prefab list): the spirit caller's cast, the healing mead, the stamina mead, the frost orb's cast for
    /// a bolt landing, the frost orbs' shield for a full charge. A name the game lacks plays nothing.
    /// </summary>
    public static class CasterSounds
    {
        public static string For(string effect)
        {
            switch (effect)
            {
                case CasterFx.Cast: return "sfx_staffspiritcaller_cast";
                case CasterFx.Mend: return "sfx_Potion_health_Start";
                case CasterFx.Chant: return "sfx_Potion_stamina_Start";
                case CasterFx.BoltHit: return "sfx_stafffrostorbs_cast";
                // Full charge (user, 2026-10-11: the spell "making that noise when fully charged was cool").
                case CasterFx.ChargeReady: return "sfx_stafffrostorbs_shield_spawn";
                default: return "";
            }
        }
    }
}
