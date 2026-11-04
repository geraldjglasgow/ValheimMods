using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The kit's ladder tool (<c>KW_tool_ladder</c>, <see cref="KwLadderTool"/>) on a dock (DOCKS.md "Dock ladders"): aimed
    /// at a dock section's edge, or a game floor's edge over the water, it hangs a dock ladder there
    /// (<see cref="KwPierLadderAim"/>), down to under the water. Hovering shows the ladder see-through where it would go, in
    /// its dock's material (<see cref="KwPreview"/>), and says what it costs or why not in the middle of the screen when that
    /// changes (<see cref="KwLadderTool.Told"/>); a click builds it through the game's own placement
    /// (<c>Player.PlacePiece</c>: the networked piece, its creator, the hammer's swing and place effect), writes its shape to
    /// its new ZDO and builds it in the same frame (<see cref="KwPierLadder.Take"/>), and takes its cost the game's way
    /// (<see cref="KwGamePay"/>: one of the material's item a metre; none with the world's no-build-cost key). The tool's
    /// workbench in range is the game's own check before the click. Any other aim is the kit's ladder tool's, as before
    /// (the dry dock's design ladders are unchanged). On the clicking player's game; the piece's ZDO carries the rest.
    /// </summary>
    public static class KwPierLadderTool
    {
        private static int seed = NewSeed();

        /// <summary>The click, when the aim is on a dock, a dock ladder or a game floor; false for any other (the kit's ladder tool takes it).</summary>
        public static bool Click(Player player)
        {
            KwPierLadderSite? site = KwPierLadderAim.Of(player, true);
            if (site == null)
                return false;
            KwLadderTool.Quiet();
            if (site.Why != null)
                Say(player, site.Why);
            else
                Build(player, site);
            return true;
        }

        /// <summary>The hover, when the aim is on a dock, a dock ladder or a game floor: the ladder shown there, or why not; false for any other.</summary>
        public static bool Preview(Player player)
        {
            KwPierLadderSite? site = KwPierLadderAim.Of(player, false);
            if (site == null)
                return false;
            if (site.Why != null)
            {
                KwPreview.Hide();
                KwLadderTool.Told(site.Why);
                return true;
            }
            string key = $"bs_pier_ladder|{site.Kind.Id}|{site.Length:F2}|{seed}|{site.Top:F2}|{site.Outward:F2}";
            KwPreview.Show(key, site.Top, site.Turn, _ => KwPierLadderShape.Mesh(site.Kind, site.Length, seed, "bs_pier_ladder_preview"), KwPierLooks.Of(site.Kind));
            KwLadderTool.Told($"Dock ladder, {Down(site)}: {site.Spec(seed).Cost} {KwPierWords.Item(site.Kind)}");
            return true;
        }

        // The cost checked as the game counts it, the ladder placed and built, the cost taken.
        private static void Build(Player player, KwPierLadderSite site)
        {
            KwPierLadderSpec spec = site.Spec(seed);
            bool free = player.NoCostCheat() || ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoBuildCost);
            string? missing = free ? null : KwBpCost.Missing(player, KwPierCost.Items(site.Kind, spec.Cost));
            if (missing != null)
            {
                Say(player, $"This ladder needs {missing}");
                return;
            }
            GameObject? placed = Place(player, site);
            if (placed == null)
            {
                Say(player, "The ladder could not be made (the log says why)");
                return;
            }
            KwPierLadder.Take(placed, spec);
            if (!free)
                KwGamePay.Take(player, KwPierCost.Requirements(site.Kind, spec.Cost, false));
            seed = NewSeed();
            Say(player, $"{site.Kind.Label} dock ladder built, {spec.Length:0.0} m {Down(site)}: {spec.Cost} {KwPierWords.Item(site.Kind)}");
        }

        // The ladder's piece placed the game's way at the site, found again among the pieces.
        private static GameObject? Place(Player player, KwPierLadderSite site)
        {
            string name = KwPierLadderPrefabs.Name(site.Kind);
            Piece? piece = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(name)?.GetComponent<Piece>() : null;
            if (piece == null)
            {
                Log.Warn($"Broadside docks: no {name} in the network scene; no ladder built.");
                return null;
            }
            player.PlacePiece(piece, site.Top, site.Turn, true, player.NoCostCheat());
            Piece? placed = KwPierBuild.Newest(name, player.GetPlayerID());
            if (placed == null)
                Log.Warn("Broadside docks: a dock ladder was not found after placing it.");
            return placed != null ? placed.gameObject : null;
        }

        private static string Down(KwPierLadderSite site) => site.Grounded ? "down to the sea floor" : "down into the water";

        private static int NewSeed() => (int)((uint)System.Environment.TickCount * 2654435761u >> 1) | 1;

        private static void Say(Player player, string text) => player.Message(MessageHud.MessageType.Center, text);
    }
}
