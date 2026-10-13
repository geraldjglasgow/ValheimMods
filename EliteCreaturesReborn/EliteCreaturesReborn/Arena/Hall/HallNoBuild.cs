using HarmonyLib;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Hall
{
    /// <summary>
    /// No building in the hall (features/arena.md section 1): while the hall stands, a piece whose ghost is within 40 m
    /// (across) of the hall's origin and above 4,900 m is refused as the game refuses a piece in a no-build location: the
    /// ghost turns red and placing it shows the game's own centre message (<c>$msg_nobuildzone</c>). The hoe's and the
    /// cultivator's ground work are pieces too. A postfix on the placement ghost's update, which runs every frame in
    /// build mode: it ends at a flag test while no hall stands, and costs a few comparisons while one does.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacementGhost))]
    internal static class HallNoBuild
    {
        private const float Radius = 40f;
        private const float Above = 4900f;

        [HarmonyPostfix]
        private static void Postfix(Player __instance)
        {
            if (!ArenaHall.Built)
            {
                return;
            }
            GameObject? ghost = __instance.m_placementGhost;
            if (ghost == null)
            {
                return;
            }
            Vector3 at = ghost.transform.position;
            float dx = at.x - ArenaHall.Origin.x;
            float dz = at.z - ArenaHall.Origin.z;
            if (at.y < Above || dx * dx + dz * dz > Radius * Radius)
            {
                return;
            }
            __instance.m_placementStatus = Player.PlacementStatus.NoBuildZone;
            __instance.SetPlacementGhostValid(false);
        }
    }
}
