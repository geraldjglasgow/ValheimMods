using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// A wider map exploration radius at sea. Minimap.UpdateExplore uncovers the map around the local player every
    /// couple of seconds with m_exploreRadius (100 m in the game); each time it does while the local player is aboard a
    /// ship (Ship.GetLocalShip, the ship whose deck volume they are in), a second Minimap.Explore uncovers the radius
    /// grown with their Sailing level. m_exploreRadius itself is never written: other mods raise it for that call and put
    /// it back, and two mods doing so put back each other's raised value, so the radius grew without end. Exploration is
    /// each player's own, so nothing crosses the network.
    /// </summary>
    public static class SeaExploration
    {
        [HarmonyPatch(typeof(Minimap), nameof(Minimap.UpdateExplore))]
        private static class Explore
        {
            [HarmonyPrefix]
            private static void Prefix(Minimap __instance, out float __state) => __state = __instance.m_exploreTimer;

            // The game sets its timer back to 0 when it explores, so a timer lower than before means it just did.
            [HarmonyPostfix]
            private static void Postfix(Minimap __instance, Player player, float __state)
            {
                if (__instance.m_exploreTimer >= __state || !SailingSkill.Active || player == null)
                    return;
                if (player != Player.m_localPlayer || Ship.GetLocalShip() == null)
                    return;
                float factor = RadiusFactor();
                if (factor > 1f)
                    __instance.Explore(player.transform.position, __instance.m_exploreRadius * factor);
            }
        }

        /// <summary>The factor on the local player's exploration radius while aboard: 2 doubles it; 1 with Sailing off.</summary>
        public static float RadiusFactor() =>
            SailingSkill.Active ? 1f + SailingSkill.Share(SailingSettings.ExploreRadius.Value, SailingSkill.Local()) : 1f;
    }
}
