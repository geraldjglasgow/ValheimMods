using System.Collections.Generic;

namespace Broadside
{
    /// <summary>
    /// The hull tools on a launched ship (the user's request, 2026-10-26: the railing, doorway, window, ladder, sail,
    /// bowsprit and post cap tools work on a ship where it floats, with the same windows as on the dock). A tool's click
    /// (from <see cref="KwTools"/>) and its preview each frame (from <see cref="KwPreview"/>) aimed at a launched ship
    /// (<see cref="KwSeaAim"/>) go to the kinds that serve that tool (<see cref="KwSeaKind.Serves"/>), in
    /// <see cref="KwSeaKinds.All"/>'s order, until one takes it; the dock's own click and preview are then skipped. A tool
    /// no kind serves says it works on the dock only. Not aiming at a launched ship, the tools work as on the dock.
    /// </summary>
    public static class KwSeaTools
    {
        private static readonly HashSet<KwSeaKind> hovering = new HashSet<KwSeaKind>();

        /// <summary>A tool's click: true when it aimed at a launched ship (taken here, whatever came of it).</summary>
        public static bool Click(Player player, string tool)
        {
            KwSeaAim? aim = KwSeaAim.Of(player);
            if (aim == null)
                return false;
            foreach (KwSeaKind kind in KwSeaKinds.All)
            {
                if (kind.Serves(tool) && kind.Click(player, tool, aim))
                    return true;
            }
            player.Message(MessageHud.MessageType.Center, Served(tool) ? KwSeaWords.Nothing : KwSeaWords.DockOnly(tool));
            return true;
        }

        /// <summary>A tool's preview this frame: true when it aims at a launched ship (or a sea window is open), the dock's preview skipped.</summary>
        public static bool Hover(Player player, string? tool)
        {
            if (KwSeaWindow.IsOpen)
                return true;
            KwSeaAim? aim = tool != null && !KwForm.IsOpen ? KwSeaAim.Of(player) : null;
            KwSeaKind? shown = null;
            if (aim != null)
            {
                foreach (KwSeaKind kind in KwSeaKinds.All)
                {
                    if (shown == null && kind.Serves(tool!) && KwShipBodyStep(() => kind.Hover(player, tool!, aim)))
                        shown = kind;
                }
            }
            Unhover(shown);
            if (shown != null)
                hovering.Add(shown);
            if (aim != null && shown == null)
                KwSeaGhost.Clear();
            return aim != null;
        }

        /// <summary>Every kind's hover but one cleared (all, for null).</summary>
        public static void Unhover(KwSeaKind? keep = null)
        {
            hovering.RemoveWhere(kind =>
            {
                if (kind == keep)
                    return false;
                KwShipBody.Step("Broadside sea unhover", kind.Unhover);
                return true;
            });
        }

        // Whether any kind serves a tool at sea.
        private static bool Served(string tool)
        {
            foreach (KwSeaKind kind in KwSeaKinds.All)
            {
                if (kind.Serves(tool))
                    return true;
            }
            return false;
        }

        // A kind's hover run so a throw is reported and counts as nothing shown.
        private static bool KwShipBodyStep(System.Func<bool> hover)
        {
            bool shown = false;
            KwShipBody.Step("Broadside sea hover", () => shown = hover());
            return shown;
        }
    }
}
