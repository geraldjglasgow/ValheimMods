using EarthWright.Brush;
using EarthWright.Extras;
using HarmonyLib;

namespace EarthWright.Patches
{
    /// <summary>
    /// The one postfix on <c>Player.UpdatePlacementGhost</c> (every frame in build mode, and once more inside
    /// <c>TryPlacePiece</c> just before a click is judged): for the local player's ghost, the brush places terrain
    /// entries first, then the seed grid and "Cultivate Anywhere" run on where the brush left the ghost.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacementGhost))]
    public static class PlacementGhostPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Player __instance, bool flashGuardStone)
        {
            if (!ReferenceEquals(__instance, Player.m_localPlayer) || __instance.m_placementGhost == null)
                return;
            GhostPlacer.Run(__instance, flashGuardStone);
            ExtrasGhost.Run(__instance, flashGuardStone);
        }
    }
}
