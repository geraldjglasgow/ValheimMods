namespace Hearthhold
{
    /// <summary>The names the source rolls store; all start with "hearthhold_" (see <see cref="Keys"/>).</summary>
    public static class SourceKeys
    {
        /// <summary>
        /// Ragdoll ZDO float: the Husbandry level of the player who killed the creature, written by the creature's owner
        /// as the ragdoll is set up and read by the ragdoll's owner when it spawns the loot (<see cref="MeatStars"/>).
        /// Absent (0) when no player made the kill.
        /// </summary>
        public const string KillerHusbandry = "hearthhold_killer_husbandry";
    }
}
