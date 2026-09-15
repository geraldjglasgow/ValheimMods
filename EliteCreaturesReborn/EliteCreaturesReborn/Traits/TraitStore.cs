namespace EliteCreaturesReborn.Traits
{
    /// <summary>
    /// Moves <see cref="CreatureTraits"/> to and from a creature's ZDO. This is the single seam between the resolved
    /// traits and their persisted form; nothing else touches the trait ZDO keys. Reads work on any client (the ZDO
    /// is shared); writes only take on the owner, which is where resolution happens.
    /// </summary>
    public static class TraitStore
    {
        public static bool IsResolved(ZDO zdo) => zdo != null && zdo.GetBool(TraitKeys.Resolved);

        public static CreatureTraits Load(ZDO zdo)
        {
            int stars = zdo.GetInt(TraitKeys.Stars);
            int mask = zdo.GetInt(TraitKeys.Mask);
            return new CreatureTraits(stars, mask);
        }

        public static void Save(ZDO zdo, CreatureTraits traits)
        {
            zdo.Set(TraitKeys.Stars, traits.Stars);
            zdo.Set(TraitKeys.Mask, traits.Mask);
            zdo.Set(TraitKeys.Resolved, true);
        }

        public static void SetBiome(ZDO zdo, Heightmap.Biome biome) => zdo.Set(TraitKeys.Biome, (int)biome);

        public static Heightmap.Biome GetBiome(ZDO zdo) => (Heightmap.Biome)zdo.GetInt(TraitKeys.Biome, (int)Heightmap.Biome.None);

        public static float GetDevouredHealth(ZDO zdo) => zdo.GetFloat(TraitKeys.DevouredHealth);

        public static float GetDevouredDamage(ZDO zdo) => zdo.GetFloat(TraitKeys.DevouredDamage);

        public static void AddDevoured(ZDO zdo, float health, float damage)
        {
            zdo.Set(TraitKeys.DevouredHealth, GetDevouredHealth(zdo) + health);
            zdo.Set(TraitKeys.DevouredDamage, GetDevouredDamage(zdo) + damage);
        }

        /// <summary>Which devourer instant-killed this prey; <see cref="ZDOID.None"/> when nothing devoured it. On the PREY's ZDO.</summary>
        public static ZDOID GetDevouredBy(ZDO zdo) => zdo.GetZDOID(TraitKeys.DevouredBy);

        /// <summary>Marks this prey as devoured by the given creature. Runs on the prey's owner (RPC_Damage), set at the bite
        /// so the death path feeds exactly that devourer even after the prey changes hands.</summary>
        public static void MarkDevouredBy(ZDO zdo, ZDOID devourer) => zdo.Set(TraitKeys.DevouredBy, devourer);

        /// <summary>The shared-clock instant a devourer may eat again, in whole milliseconds (0 when it never has). On the DEVOURER's ZDO.</summary>
        public static long GetDevourReadyAt(ZDO zdo) => zdo.GetLong(TraitKeys.DevourReadyAt, 0L);

        /// <summary>Owner-only write, at the moment a meal is banked: the cooldown starts here and every client reads it.</summary>
        public static void SetDevourReadyAt(ZDO zdo, long whenMs) => zdo.Set(TraitKeys.DevourReadyAt, whenMs);
    }
}
