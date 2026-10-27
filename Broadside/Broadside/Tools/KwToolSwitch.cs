using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The tool switch (the user's request, 2026-10-25: "If somebody is holding the wrong tool in their hand and clicks a
    /// piece of the ship, it should just switch tools and let them edit that piece right there ... when you click something,
    /// you are editing it, and it switches to the correct tool"). With any Broadside tool in hand, a click on a part the
    /// tool does not take (<see cref="KwToolActs"/>) selects that part's own tool in the hammer as the game's own piece
    /// pick does (<see cref="Select"/>: the build menu's tab and selection, the placement ghost, the hammer's panel) and
    /// opens that part's edit (<see cref="KwEdit.Open"/>; Shift: every one of its kind on the dock), whatever its tool would
    /// take near it its own way. A doorway or window on the part under the crosshair is the Doorway or Window tool's: a click
    /// on it with any other tool switches to that tool, whose own click then changes it (Shift: its row), even where the
    /// held tool would change the wall around it. Fittings (hatches, rudders, the wheel, the windlass), the dock and
    /// anything else no tool edits are left to the held tool, and so is every click while its first click waits. The hover
    /// says it first (<see cref="KwToolSwitchHint"/>). Local to the player clicking: nothing is sent.
    /// </summary>
    public static class KwToolSwitch
    {
        /// <summary>A click switched and taken; false for every other click (the held tool takes it as before).</summary>
        public static bool Click(Player player, string tool)
        {
            KwSwitchTarget? target = KwForm.IsOpen ? null : Target(player, tool);
            if (target == null || !Select(player, target.Tool))
                return false;
            KwToolSwitchHint.Forget();
            if (target.Kind != null && target.Part != null)
                KwEdit.Open(player, target.Kind, target.Part);
            else
                KwHouseTools.Click(player, target.Tool);
            return true;
        }

        /// <summary>What a click at the aim would switch to with this tool in hand; null for a click the tool keeps.</summary>
        public static KwSwitchTarget? Target(Player player, string tool)
        {
            KwClick? aim = KwTools.Aim(player);
            bool waits = KwToolActs.Waits(tool);
            KwSwitchTarget? opening = waits ? null : Opening(player, tool, aim);
            KwSwitchTarget? own = opening == null && aim != null ? Own(aim.Value.Piece) : null;
            string? to = KwSwitchRule.Choose(tool, waits, opening?.Tool, own?.Tool, () => KwToolActs.Acts(player, tool, aim!.Value));
            KwSwitchTarget? target = to != null ? opening ?? own : null;
            return target != null && ToolPiece(player, target.Tool) != null ? target : null;
        }

        /// <summary>The Broadside tool in the hammer ("sail"), or null.</summary>
        public static string? Held(Player player)
        {
            Piece? piece = player.InPlaceMode() ? player.GetSelectedPiece() : null;
            string name = piece != null ? piece.name : "";
            return name.StartsWith(KwToolPieces.ToolPrefix, System.StringComparison.Ordinal) ? name.Substring(KwToolPieces.ToolPrefix.Length) : null;
        }

        /// <summary>
        /// The tool picked in the hammer as the game's build menu picks a piece (<c>BuildUI.OnSelectPiece</c>: its tab, its
        /// place in the grid, the placement ghost made again, the button's sound); the hammer's panel shows it from the next
        /// frame. False when the hammer lacks it.
        /// </summary>
        public static bool Select(Player player, string tool)
        {
            Piece? piece = ToolPiece(player, tool);
            if (piece == null || !player.SetSelectedPiece(piece) || player.GetSelectedPiece() != piece)
                return false;
            player.PlayButtonSound();
            return true;
        }

        // The tool's piece in the hammer in hand, when the player can pick it there.
        private static Piece? ToolPiece(Player player, string tool)
        {
            PieceTable? table = player.m_buildPieces;
            string name = KwToolPieces.ToolPrefix + tool;
            GameObject? prefab = table != null ? table.m_pieces.Find(p => p != null && p.name == name) : null;
            Piece? piece = prefab != null ? prefab.GetComponent<Piece>() : null;
            return piece != null && table!.m_availablePieces.Contains(piece) ? piece : null;
        }

        // An opening under the crosshair as the Doorway and Window tools see it: with one of them in hand, wherever they see
        // it; with another tool, only on the part the crosshair is on (not one seen through to something beyond it).
        private static KwSwitchTarget? Opening(Player player, string tool, KwClick? aim)
        {
            bool own = KwSwitchRule.Opens(tool);
            if (!own && (aim == null || KwOpeningHull.ToolOf(aim.Value.Piece) == null))
                return null;
            KwOpeningSight sight = KwOpeningAimed.Of(player);
            Piece? on = sight.Site?.Piece;
            if (sight.Opening == null || on == null || (!own && aim!.Value.Piece != on))
                return null;
            bool doorway = sight.Opening.IsDoorway;
            return new KwSwitchTarget(KwSwitchRule.OpeningTool(doorway), null, null, doorway ? "doorway" : "window", on.GetInstanceID() + "/" + sight.Opening.Id);
        }

        // A part some tool edits, with that tool; null for a piece none does.
        private static KwSwitchTarget? Own(Piece piece)
        {
            KwEditKind? kind = KwSwitchRule.KindOf(Utils.GetPrefabName(piece.gameObject));
            return kind != null ? new KwSwitchTarget(kind.Tool, kind, piece, kind.One, piece.GetInstanceID().ToString()) : null;
        }
    }

    /// <summary>
    /// What a click switches to: the tool, the part's kind and the part (none for an opening, which its tool's own click
    /// changes), the part as the player reads it, and a key that is the same while the same thing is aimed at.
    /// </summary>
    public sealed class KwSwitchTarget
    {
        public KwSwitchTarget(string tool, KwEditKind? kind, Piece? part, string one, string key) => (Tool, Kind, Part, One, Key) = (tool, kind, part, one, key);

        public string Tool { get; }
        public KwEditKind? Kind { get; }
        public Piece? Part { get; }
        public string One { get; }
        public string Key { get; }

        /// <summary>"Click: change this mast (switches to the Sail tool)".</summary>
        public string Hint => KwSwitchRule.Hint(One, Tool);
    }
}
