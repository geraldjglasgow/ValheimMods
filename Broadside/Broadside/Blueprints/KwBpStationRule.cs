using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The blueprint table stands only by a dry dock: while its placement ghost is more than
    /// <see cref="KwBlueprintStation.Range"/> from every dock piece's footprint (a keel block guide's, or a keel block's),
    /// the ghost is invalid (red) and a click says why (<see cref="KwBpStationClick"/>, which looks again at the moment of
    /// the click). Runs after the game updates its ghost; the dock pieces are looked for at most ten times a second. Local
    /// to the placing player.
    /// </summary>
    [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
    public static class KwBpStationRule
    {
        private const float Every = 0.1f;
        private static float nextScan;
        private static bool near;

        [HarmonyPostfix]
        private static void Postfix(Player __instance) => Guard.Run("Broadside blueprint table placement", Check, __instance);

        private static void Check(Player player)
        {
            GameObject? ghost = player.m_placementGhost;
            if (player != Player.m_localPlayer || ghost == null || ghost.name != KwBlueprintStation.Prefab
                || player.m_placementStatus != Player.PlacementStatus.Valid || DockNear(ghost.transform.position))
                return;
            player.m_placementStatus = Player.PlacementStatus.Invalid;
            player.SetPlacementGhostValid(false);
        }

        private static bool DockNear(Vector3 at)
        {
            if (Time.time < nextScan)
                return near;
            nextScan = Time.time + Every;
            near = KwBpDock.NearestPiece(at, KwBlueprintStation.Range) != null;
            return near;
        }
    }

    /// <summary>
    /// A click with the blueprint table selected: the game places it as usual, unless no dry dock is in range; then the
    /// click says so instead of the game's "invalid placement".
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
    public static class KwBpStationClick
    {
        [HarmonyPrefix]
        private static bool Prefix(Player __instance, Piece piece, ref bool __result)
        {
            if (piece == null || piece.name != KwBlueprintStation.Prefab)
                return true;
            bool place = Guard.Run<Player, bool>("Broadside blueprint table place", DockInRange, __instance);
            if (!place)
                __result = false;
            return place;
        }

        // True: the game places the table.
        private static bool DockInRange(Player player)
        {
            GameObject? ghost = player.m_placementGhost;
            if (ghost == null || KwBpDock.NearestPiece(ghost.transform.position, KwBlueprintStation.Range) != null)
                return true;
            player.Message(MessageHud.MessageType.Center,
                $"The blueprint table must stand within {KwBlueprintStation.Range:0} m of a dry dock (a keel block guide or a row of keel blocks)");
            return false;
        }
    }
}
