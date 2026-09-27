using EarthWright.Core;
using HarmonyLib;
using UnityEngine;

namespace EarthWright.Extras
{
    /// <summary>
    /// "Cultivate Anywhere": the game refuses its cultivator ground entries (cultivate, replant, and EarthWright's copies
    /// of them such as Till) with "needs dirt" where the ground's grass layer is too thin - bare rock, cleared or paved
    /// ground. With the setting on, that one refusal is lifted for those entries (ground pieces marked as needing
    /// vegetation ground). The game stops checking at "needs dirt", so dungeons, no-build locations and wards are checked
    /// again at the ghost's final point (where the brush put it). The check is the placing client's, so the synced
    /// setting is all it needs.
    /// </summary>
    public static class CultivateAnywhere
    {
        internal static void Apply(Player player, bool flash)
        {
            if (!ExtrasSettings.CultivateAnywhere.Value || !GeneralSettings.Active)
                return;
            if (player.m_placementStatus != Player.PlacementStatus.NeedDirt)
                return;
            Piece piece = LocalTool.SelectedPiece;
            if (piece == null || !piece.m_groundPiece || !piece.m_vegetationGroundOnly)
                return;
            Vector3 point = player.m_placementGhost.transform.position;
            GhostStatus.Set(player, GhostStatus.Checked(player, piece, point, flash));
        }
    }

    /// <summary>
    /// After the game (and the brush) placed and checked the local player's ghost - every frame, and again inside
    /// TryPlacePiece just before a click is judged: the seed grid moves seeds and saplings, and "Cultivate Anywhere"
    /// lifts the "needs dirt" refusal. Low priority, so the brush's own ghost placement runs first.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacementGhost))]
    public static class ExtrasGhostPatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Low)]
        public static void Postfix(Player __instance, bool flashGuardStone)
        {
            if (!ReferenceEquals(__instance, Player.m_localPlayer) || __instance.m_placementGhost == null)
                return;
            Safe.Run("EarthWright seed grid", () => SeedGrid.Apply(__instance, flashGuardStone));
            Safe.Run("EarthWright cultivate anywhere", () => CultivateAnywhere.Apply(__instance, flashGuardStone));
        }
    }
}
