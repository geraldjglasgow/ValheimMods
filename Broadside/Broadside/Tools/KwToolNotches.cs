using System.Collections.Generic;
using HarmonyLib;
using PatchGuard;

namespace Broadside
{
    /// <summary>
    /// The mouse wheel's notches for a tool that steps through choices with it instead of turning (the Post cap tool's
    /// styles, with Shift its length and with Ctrl its lean, <see cref="KwCapTool"/>; the Doorway and Window tools' types, and with Shift held a row's count,
    /// <see cref="KwOpeningHover"/>). Such a tool's piece may turn (<c>Piece.m_canRotate</c>), so the game itself counts
    /// the wheel's notches (and the gamepad's turn buttons) into its placement rotation, in the table's build camera too,
    /// which leaves the wheel to the game while no window is open, and the view does not zoom with the wheel. Before the
    /// placement ghost is placed each frame, the notches since the last frame are taken back off that rotation and kept for
    /// the tool (<see cref="Take"/>), so its ghost never turns and the turning tools find their rotation as they left it.
    /// A tool that steps is one more name in <see cref="Steppers"/>. Local to this player.
    /// </summary>
    [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
    public static class KwToolNotches
    {
        /// <summary>The tools (their names after "KW_tool_") whose wheel steps choices.</summary>
        public static readonly HashSet<string> Steppers = new HashSet<string> { KwCapSpec.Tool, KwDoorwayTool.Tool, KwWindowTool.Tool };

        private static string held = "";
        private static int rest, notches;

        [HarmonyPrefix]
        private static void Prefix(Player __instance) => Guard.Run("Broadside tool wheel", Count, __instance);

        // A stepping tool taken in hand starts from the rotation as it is; while it stays in hand, every notch the game
        // counts is moved from the rotation to the tool.
        private static void Count(Player player)
        {
            if (player != Player.m_localPlayer)
                return;
            string tool = Stepper(player);
            if (tool != held)
            {
                (held, rest, notches) = (tool, player.m_placeRotation, 0);
                return;
            }
            if (tool.Length == 0)
                return;
            notches += player.m_placeRotation - rest;
            player.m_placeRotation = rest;
        }

        /// <summary>The notches turned for this tool since it was last asked (up positive), taken; 0 while it is not in hand.</summary>
        public static int Take(string tool)
        {
            if (tool != held || tool.Length == 0)
                return 0;
            int taken = notches;
            notches = 0;
            return taken;
        }

        // The stepping tool in hand; empty for none.
        private static string Stepper(Player player)
        {
            Piece? piece = player.InPlaceMode() ? player.GetSelectedPiece() : null;
            string name = piece != null ? piece.name : "";
            if (!name.StartsWith(KwToolPieces.ToolPrefix, System.StringComparison.Ordinal))
                return "";
            string tool = name.Substring(KwToolPieces.ToolPrefix.Length);
            return Steppers.Contains(tool) ? tool : "";
        }
    }
}
