using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// A wider map exploration radius at sea. Minimap.UpdateExplore uncovers the map around the local player every
    /// couple of seconds with m_exploreRadius (100 m in the game); for that one call the radius grows with the local
    /// player's Sailing level while they are aboard a ship (Ship.GetLocalShip, the ship whose deck volume they are in),
    /// then goes back. Exploration is each player's own, so nothing crosses the network.
    /// </summary>
    public static class SeaExploration
    {
        [HarmonyPatch(typeof(Minimap), nameof(Minimap.UpdateExplore))]
        private static class Explore
        {
            [HarmonyPrefix]
            private static void Prefix(Minimap __instance, Player player, out float __state)
            {
                __state = -1f;
                if (!SailingSkill.Active || player == null || player != Player.m_localPlayer || Ship.GetLocalShip() == null)
                    return;
                __state = __instance.m_exploreRadius;
                __instance.m_exploreRadius *= RadiusFactor();
            }

            [HarmonyFinalizer]
            private static void Finalizer(Minimap __instance, float __state)
            {
                if (__state >= 0f)
                    __instance.m_exploreRadius = __state;
            }
        }

        /// <summary>The factor on the local player's exploration radius while aboard: 2 doubles it; 1 with Sailing off.</summary>
        public static float RadiusFactor() =>
            SailingSkill.Active ? 1f + SailingSkill.Share(SailingSettings.ExploreRadius.Value, SailingSkill.Local()) : 1f;
    }
}
