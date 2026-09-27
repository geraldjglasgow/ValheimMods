using EarthWright.Core;
using EarthWright.Terrain;
using HarmonyLib;
using UnityEngine;

namespace EarthWright.Actions
{
    /// <summary>
    /// Routes the local player's terrain clicks through EarthWright.
    /// <list type="number">
    /// <item><c>Player.TryPlacePiece</c> prefix: a special entry runs its handler instead (nothing placed, nothing charged).
    /// A brush entry builds its edit and runs the sender guards first, so a refused click costs nothing.</item>
    /// <item>The game then checks the placement and charges as usual, and instantiates the entry's TerrainOp prefab.</item>
    /// <item><c>TerrainOp.Awake</c> prefix: the pending edit is sent through the <see cref="Dispatcher"/> instead of
    /// the game's own terrain RPC; the prefab's placement effect still plays and the object is destroyed.</item>
    /// </list>
    /// TerrainOps not made by this player's click (pickaxe digging, other mods) are left to the game.
    /// </summary>
    public static class PlacementHook
    {
        internal static TerrainEdit Pending;
        internal static string PendingPiece;

        /// <summary>Set by modules that want the TerrainOp's effect skipped (the dust setting).</summary>
        public static System.Func<bool> SkipPlacedEffect = () => false;

        internal static bool BeforePlace(Player player, Piece piece, ref bool result)
        {
            Pending = null;
            if (player != Player.m_localPlayer || !GeneralSettings.Active || !LocalTool.IsToolName(LocalTool.RightItemName))
                return true;
            ToolAction action = ActionCatalog.For(piece);
            if (action == null)
                return true;
            if (Refused(Brush.BrushCaps.EntryRefusal(action.Id), ref result))
                return false;
            if (action.IsSpecial)
                return RunSpecial(player, action, ref result);
            TerrainEdit edit = EditFactory.Build(action);
            edit.Flags |= EditFlags.FromPlacement;
            EditEvents.RaiseBuilding(edit);
            string reason = EditGuards.CheckSender(edit);
            if (!string.IsNullOrEmpty(reason))
            {
                Messages.Center(reason);
                result = false;
                return false;
            }
            Pending = edit;
            PendingPiece = Utils.GetPrefabName(piece.gameObject);
            return true;
        }

        /// <summary>Shows a refusal (the held tool's level locks the entry) and cancels the click; false when there is none.</summary>
        private static bool Refused(string reason, ref bool result)
        {
            if (string.IsNullOrEmpty(reason))
                return false;
            Messages.Center(reason);
            result = false;
            return true;
        }

        private static bool RunSpecial(Player player, ToolAction action, ref bool result)
        {
            result = false;
            ISpecialAction handler = SpecialActions.For(action);
            GameObject ghost = player.m_placementGhost;
            if (handler != null)
                Safe.Run("EarthWright " + action.Special, () => handler.OnClick(player, action, ghost != null ? ghost.transform.position : player.transform.position));
            return false;
        }

        /// <summary>True when the TerrainOp was this player's click and has been handled here.</summary>
        internal static bool Intercept(TerrainOp op)
        {
            if (Pending == null || Utils.GetPrefabName(op.gameObject) != PendingPiece)
                return false;
            TerrainEdit edit = Pending;
            Pending = null;
            EditFactory.ClearOneShot();
            Dispatcher.SendChecked(edit);
            if (!SkipPlacedEffect())
                op.m_onPlacedEffect.Create(op.transform.position, Quaternion.identity);
            Object.Destroy(op.gameObject);
            return true;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
    public static class TryPlacePiecePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Player __instance, Piece piece, ref bool __result)
        {
            return PlacementHook.BeforePlace(__instance, piece, ref __result);
        }

        [HarmonyPostfix]
        public static void Postfix()
        {
            PlacementHook.Pending = null;
            EditFactory.ClearOneShot();
        }
    }

    [HarmonyPatch(typeof(TerrainOp), nameof(TerrainOp.Awake))]
    public static class TerrainOpAwakePatch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.High)]
        public static bool Prefix(TerrainOp __instance) => !PlacementHook.Intercept(__instance);
    }
}
