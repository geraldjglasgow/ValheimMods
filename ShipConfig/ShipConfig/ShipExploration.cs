using HarmonyLib;

namespace ShipConfig
{
    /// <summary>
    /// A wider map exploration radius aboard a ship (Multipliers, Explore Radius Multiplier). Minimap.UpdateExplore
    /// uncovers the map around the local player every couple of seconds with m_exploreRadius (100 m in the game); each
    /// time it does while the local player is aboard a ship (Ship.GetLocalShip, the ship whose deck volume they are in),
    /// a second Minimap.Explore uncovers that radius times the setting and GrindstoneSkills' Sailing factor, so the two
    /// multiply (GrindstoneSkills' own wider call falls inside this one). m_exploreRadius itself is never written: two mods
    /// raising it for that call and putting it back put back each other's raised value. Exploration is each player's own,
    /// so nothing crosses the network; the multiplier is the server's.
    /// </summary>
    public static class ShipExploration
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
                if (__instance.m_exploreTimer >= __state || player == null || player != Player.m_localPlayer)
                    return;
                float factor = ShipMultipliers.ExploreRadius;
                if (factor > 1f && Ship.GetLocalShip() != null)
                    __instance.Explore(player.transform.position, __instance.m_exploreRadius * factor * GrindstoneLink.ExploreFactor());
            }
        }
    }
}
