namespace EliteCreaturesReborn.Traits
{
    /// <summary>
    /// The boss-aspect state that is not part of the rolled traits: an altar's current aspect and shift time, a twin's
    /// partner, a phantom copy's boss and a summoner's wave count. The single seam between that state and the ZDO, as
    /// <see cref="TraitStore"/> is for the traits. Reads work anywhere; every write is made by the object's owner.
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

        /// <summary>The world time (whole milliseconds) the altar shifts next; long.MaxValue when it never will.</summary>
        public static long GetAltarShiftAt(ZDO zdo) => zdo.GetLong(TraitKeys.AltarShiftAt, 0L);

        public static void SetAltar(ZDO zdo, Aspect aspect, long shiftAtMs)
        {
            zdo.Set(TraitKeys.AltarAspect, (int)aspect);
            zdo.Set(TraitKeys.AltarShiftAt, shiftAtMs);
        }

        public static ZDOID GetTwin(ZDO zdo) => zdo.GetZDOID(TraitKeys.TwinPartner);

        public static void SetTwin(ZDO zdo, ZDOID partner) => zdo.Set(TraitKeys.TwinPartner, partner);

        public static ZDOID GetPhantomOf(ZDO zdo) => zdo.GetZDOID(TraitKeys.PhantomOf);

        public static void SetPhantomOf(ZDO zdo, ZDOID boss) => zdo.Set(TraitKeys.PhantomOf, boss);

        public static int GetWaves(ZDO zdo) => zdo.GetInt(TraitKeys.SummonWaves);

        public static void SetWaves(ZDO zdo, int waves) => zdo.Set(TraitKeys.SummonWaves, waves);
    }
}
