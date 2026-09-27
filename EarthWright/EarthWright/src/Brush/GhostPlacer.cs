using EarthWright.Actions;
using EarthWright.Core;
using EarthWright.Terrain;
using HarmonyLib;
using UnityEngine;

namespace EarthWright.Brush
{
    /// <summary>
    /// After the game has placed the local player's ghost (every frame, and once more inside <c>TryPlacePiece</c> just
    /// before a click is checked), the brush takes over for terrain entries: it finds the aimed ground itself (the game
    /// has already moved a level entry's ghost to feet height), applies aim-at-edge and snapping, publishes
    /// <see cref="BrushState.Center"/> and the target height, and stands the ghost at the centre: at the target height
    /// for levelling entries (as the game does with the feet height), on the ground for the rest. The placed TerrainOp
    /// therefore sits where the brush says. Local player only.
    /// </summary>
    public static class GhostPlacer
    {
        public static void AfterGame(Player player, bool flashGuardStone)
        {
            if (player != Player.m_localPlayer || !BrushState.Active || player.m_placementGhost == null)
                return;
            ToolAction action = ActionCatalog.Current;
            if (action == null || action != BrushState.Action)
                return;
            BrushTick.SyncExternal();
            if (!GhostAim.TryAim(player, out Vector3 aim, out bool direct))
            {
                BrushState.HasAim = false;
                TargetHeight.Refresh(player);
                return;
            }
            BrushState.HasAim = true;
            BrushState.AimPoint = aim;
            Vector3 center = CenterPlacement.From(aim, action);
            TargetHeight.Refresh(player);
            center.y = TargetHeight.Used(action) ? BrushState.TargetHeight : TerrainRead.GroundHeight(center, aim.y);
            BrushState.Center = center;
            player.m_placementGhost.transform.position = center;
            GhostValidity.Apply(player, player.m_placementGhost, center, direct, flashGuardStone);
        }
    }

    /// <summary>Postfix on <c>Player.UpdatePlacementGhost</c>: the brush positions the ghost of terrain entries.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacementGhost))]
    public static class PlacementGhostPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Player __instance, bool flashGuardStone)
        {
            try
            {
                GhostPlacer.AfterGame(__instance, flashGuardStone);
            }
            catch (System.Exception e)
            {
                BrushLog.Error("brush ghost", e);
            }
        }
    }
}
