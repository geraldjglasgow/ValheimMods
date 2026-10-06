namespace GrindstoneSkills
{
    /// <summary>
    /// The angler behind a fishing float. The float is made on the caster's client when the cast lands (the thrown
    /// projectile's spawn on hit calls FishingFloat.Setup there), so the caster owns it and runs its FixedUpdate: the
    /// reel, the hook and the catch all happen on the angler's own client. Setup's postfix (<see cref="FloatSetup"/>)
    /// writes the angler's Fishing level and the bait's stars to the float's ZDO, which reaches every machine near it,
    /// so the owners of the fish around it can read them when they decide whether to bite (<see cref="BiteChance"/>).
    /// A float cast without GrindstoneSkills reads as level 0 with plain bait.
    /// </summary>
    public static class Angler
    {
        private static readonly int AnglerLevelHash = Keys.AnglerLevel.GetStableHashCode();
        private static readonly int BaitStarsHash = Keys.BaitStars.GetStableHashCode();

        public static void Stamp(FishingFloat fishingFloat, float level, int baitStars)
        {
            ZDO zdo = fishingFloat.m_nview.GetZDO();
            zdo.Set(Keys.AnglerLevel, level);
            zdo.Set(Keys.BaitStars, baitStars);
        }

        /// <summary>The Fishing level of the float's angler at the cast; 0 when unknown.</summary>
        public static float Level(FishingFloat fishingFloat)
        {
            ZDO zdo = Zdo(fishingFloat);
            return zdo == null ? 0f : UnityEngine.Mathf.Max(0f, zdo.GetFloat(AnglerLevelHash));
        }

        /// <summary>The stars of the bait on the float's hook; 0 when unknown.</summary>
        public static int BaitStars(FishingFloat fishingFloat)
        {
            ZDO zdo = Zdo(fishingFloat);
            return zdo == null ? 0 : UnityEngine.Mathf.Clamp(zdo.GetInt(BaitStarsHash), 0, Stars.Max);
        }

        /// <summary>
        /// The local player, when this client owns the float and the local player cast it; null anywhere else. The game
        /// finds a float's angler by the user ID stored at the cast (FishingFloat.GetOwner).
        /// </summary>
        public static Player LocalOf(FishingFloat fishingFloat)
        {
            if (fishingFloat == null || fishingFloat.m_nview == null || !fishingFloat.m_nview.IsValid() || !fishingFloat.m_nview.IsOwner())
                return null;
            Player local = Player.m_localPlayer;
            return local != null && fishingFloat.GetOwner() == local ? local : null;
        }

        private static ZDO Zdo(FishingFloat fishingFloat) =>
            fishingFloat != null && fishingFloat.m_nview != null && fishingFloat.m_nview.IsValid() ? fishingFloat.m_nview.GetZDO() : null;
    }
}
