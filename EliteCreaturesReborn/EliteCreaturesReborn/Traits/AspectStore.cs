namespace EliteCreaturesReborn.Traits
{
    /// <summary>
    /// The boss-aspect state that is not part of the rolled traits: an altar's current aspect (with a Bountiful one's
    /// extras) and shift time, a twin's or tethered boss's partner, a phantom copy's boss, a phantom boss's split count
    /// and a summoner's wave count. The single seam between that state and the ZDO, as <see cref="TraitStore"/> is for
    /// the traits. Reads work anywhere; every write is made by the object's owner.
    /// </summary>
    public static class AspectStore
    {
        private const int Unrolled = -1;

        public static bool AltarRolled(ZDO zdo) => zdo.GetInt(TraitKeys.AltarAspect, Unrolled) != Unrolled;

        public static Aspect GetAltarAspect(ZDO zdo)
        {
            int value = zdo.GetInt(TraitKeys.AltarAspect, Unrolled);
            return value == Unrolled ? Aspect.None : (Aspect)value;
        }

        /// <summary>Everything the altar offers: its aspect and, beside Bountiful, the extras the boss will carry.</summary>
        public static BossAspects GetAltarAspects(ZDO zdo) =>
            new BossAspects(GetAltarAspect(zdo), zdo.GetInt(TraitKeys.AltarExtra));

        /// <summary>The world time (whole milliseconds) the altar shifts next; long.MaxValue when it never will.</summary>
        public static long GetAltarShiftAt(ZDO zdo) => zdo.GetLong(TraitKeys.AltarShiftAt, 0L);

        public static void SetAltar(ZDO zdo, BossAspects aspects, long shiftAtMs)
        {
            zdo.Set(TraitKeys.AltarAspect, (int)aspects.Headline);
            if (zdo.GetInt(TraitKeys.AltarExtra) != aspects.Extras)
            {
                zdo.Set(TraitKeys.AltarExtra, aspects.Extras); // 0 clears it; an altar never Bountiful never gets the key
            }
            zdo.Set(TraitKeys.AltarShiftAt, shiftAtMs);
        }

        public static ZDOID GetTwin(ZDO zdo) => zdo.GetZDOID(TraitKeys.TwinPartner);

        public static void SetTwin(ZDO zdo, ZDOID partner) => zdo.Set(TraitKeys.TwinPartner, partner);

        public static ZDOID GetTether(ZDO zdo) => zdo.GetZDOID(TraitKeys.TetherPartner);

        public static void SetTether(ZDO zdo, ZDOID partner) => zdo.Set(TraitKeys.TetherPartner, partner);

        public static ZDOID GetPhantomOf(ZDO zdo) => zdo.GetZDOID(TraitKeys.PhantomOf);

        public static void SetPhantomOf(ZDO zdo, ZDOID boss) => zdo.Set(TraitKeys.PhantomOf, boss);

        public static int GetWaves(ZDO zdo) => zdo.GetInt(TraitKeys.SummonWaves);

        public static void SetWaves(ZDO zdo, int waves) => zdo.Set(TraitKeys.SummonWaves, waves);

        public static int GetSplits(ZDO zdo) => zdo.GetInt(TraitKeys.PhantomSplits);

        public static void SetSplits(ZDO zdo, int splits) => zdo.Set(TraitKeys.PhantomSplits, splits);
    }
}
