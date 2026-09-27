namespace GrindstoneSkills
{
    /// <summary>
    /// Twins, on the parent's owner, right after a birth whose twin roll was won (<see cref="BirthRolls"/>). The parent
    /// is made pregnant again through the game's own MakePregnant (Elite Creatures Reborn patches it to remember the
    /// partner beside the parent), then its pregnancy start (ZDO s_pregnant) is moved back past the game's own
    /// pregnancy duration, so it is already due, and it is marked a twin's (<see cref="Keys.Twin"/>). The game's next
    /// breeding check, half a minute later, gives the second birth (a second egg for a hen): a pregnant animal gives
    /// birth whether it is hungry, alerted or crowded. A twin's birth clears the mark and rolls no twins of its own.
    /// </summary>
    public static class TwinPregnancy
    {
        private static readonly int TwinHash = Keys.Twin.GetStableHashCode();

        /// <summary>
        /// Starts the twin's pregnancy, already due by <paramref name="pregnancyDuration"/> (the game's own, unpaced) plus
        /// a second. False when the parent is gone or already pregnant again.
        /// </summary>
        public static bool Conceive(Procreation parent, float pregnancyDuration)
        {
            ZNetView nview = parent.m_nview;
            if (nview == null || !nview.IsValid() || parent.IsPregnant())
                return false;
            parent.MakePregnant();
            ZDO zdo = nview.GetZDO();
            zdo.Set(ZDOVars.s_pregnant, Herd.TicksIn(-(pregnancyDuration + 1f)));
            zdo.Set(TwinHash, true);
            return true;
        }

        /// <summary>The current pregnancy is a twin's.</summary>
        public static bool IsTwin(ZDO zdo) => zdo != null && zdo.GetBool(TwinHash);

        public static void Clear(ZDO zdo) => zdo.Set(TwinHash, false);
    }
}
