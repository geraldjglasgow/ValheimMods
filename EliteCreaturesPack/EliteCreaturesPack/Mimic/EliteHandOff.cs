namespace EliteCreaturesPack.Mimic
{
    /// <summary>
    /// What a mimic shares with Elite Creatures Reborn when that mod is installed. Only key names cross between the two
    /// mods: neither references the other, and without it the keys are simply never read, or never written.
    /// <list type="bullet">
    /// <item><see cref="DisguisedKey"/>: this mod sets it on every mimic's ZDO. While a creature carrying it sleeps,
    /// Elite Creatures Reborn holds back everything it would show on it (its size, star look, mutation effects and
    /// decorated name) and puts them on when it wakes, so a dormant mimic looks exactly like the chest.</item>
    /// <item><see cref="GenerationKey"/>: Elite Creatures Reborn sets it on a copy its Splintering mutation splits off;
    /// this mod reads it so a split-off mimic is born awake, since its parent was already fighting.</item>
    /// </list>
    /// </summary>
    internal static class EliteHandOff
    {
        public const string DisguisedKey = "ecp_disguised";
        public const string GenerationKey = "ecr_gen";

        /// <summary>OWNER, as a mimic wakes into the scene: marked disguised for good (the mark lives in its ZDO).</summary>
        public static void MarkDisguised(ZNetView nview)
        {
            ZDO zdo = nview.GetZDO();
            if (!zdo.GetBool(DisguisedKey))
            {
                zdo.Set(DisguisedKey, true);
            }
        }

        /// <summary>Whether Elite Creatures Reborn's Splintering split this creature off another.</summary>
        public static bool IsSplitOff(ZNetView nview) => nview.GetZDO().GetInt(GenerationKey) > 0;
    }
}
