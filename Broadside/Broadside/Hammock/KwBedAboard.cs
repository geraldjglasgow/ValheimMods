using HarmonyLib;

namespace Broadside
{
    /// <summary>
    /// Sleeping in a bed that rides a launched ship (<see cref="KwShipRider"/>: a hammock, or with the .cfg's
    /// AllowBuildingOnShips any of the game's beds): the game's own checks run as on land, but the fire. A bed needs a fire
    /// near it (the game's heat area at the bed's place), and no fire can be had aboard (the game keeps fires off wooden
    /// floors and a ship's deck; by default only cannons, crates, piles and hammocks are built there), so aboard that
    /// check passes. Night only, no enemies near, not wet, and under a roof with enough cover all stand: the cover rays
    /// see the ship's hull, decks and cabins (the vehicle layer), so a bed in the hold or a cabin sleeps and one on the
    /// open deck is told it needs a roof. <c>Bed.CheckFire</c> prefix; local to the sleeping player, nothing sent.
    /// </summary>
    [HarmonyPatch(typeof(Bed), nameof(Bed.CheckFire))]
    public static class KwBedAboard
    {
        [HarmonyPrefix]
        private static bool Prefix(Bed __instance, ref bool __result)
        {
            if (!Aboard(__instance))
                return true;
            __result = true;
            return false;
        }

        /// <summary>Whether a bed rides a launched ship in this game's scene now.</summary>
        public static bool Aboard(Bed bed) => bed.TryGetComponent(out KwShipRider rider) && rider.On != null;
    }
}
