namespace Broadside
{
    /// <summary>
    /// Whether a tool takes a click at the aim, asked before the tool switch (<see cref="KwToolSwitch"/>) offers the part's
    /// own tool: each tool's own aim and site logic run without acting, so the two never disagree. A tool takes its own
    /// parts (and what one stands over, <see cref="KwEditKind.Over"/>: a railed roof or hull, a capped post's top), and what
    /// it builds from: the planking, deck and railing tools ribs and keel ends (<see cref="KwTools.Resolve"/>; the deck tool
    /// decks too), the railing tool any cabin (<see cref="KwRoofSite"/>); the keel tool keels and keel rests
    /// (<see cref="KwShapeSites.Keel"/>); the rib tool a keel's rib points; the sail tool decks, keels and cabin roofs
    /// (<see cref="KwShapeSites.Mast"/>); the bowsprit and Post cap tools a keel's post top within reach
    /// (<see cref="KwBowspritTool"/>, <see cref="KwCapTool.Hover"/>); the ladder tool a hatch or an open edge it finds
    /// (<see cref="KwLadderEdges"/>), even where the spot is then refused; the Cabin tool deck sections and roofs
    /// (<see cref="KwCabinPick"/>); the Doorway and Window tools planking and cabins, and any wall they find. While a tool's
    /// first click waits, its next click is its own whatever it is on (<see cref="Waits"/>). A tool this does not know takes
    /// every click. Reads only; local to the player aiming.
    /// </summary>
    public static class KwToolActs
    {
        /// <summary>Whether the tool takes a click at this aim (its own way, or as an edit of its own part).</summary>
        public static bool Acts(Player player, string tool, KwClick aim)
        {
            if (Edits(tool, aim))
                return true;
            if (tool == "planking" || tool == "deck")
                return KwTools.Resolve(aim, tool) != null;
            if (tool == KwBiomeCost.Rail)
                return KwTools.Resolve(aim, tool) != null || KwRoofSite.Of(aim.Piece) != null;
            if (tool == KwShapeTool.Keel)
                return KwKeel.IsKeel(aim.Piece) || KwShapeSites.Keel(aim) != null;
            if (tool == KwShapeTool.Rib)
                return KwShapeSites.Rib(aim) != null;
            return Fitting(player, tool, aim);
        }

        /// <summary>Whether a first click of the tool waits for its second (the planking, deck and railing tools', the Cabin tool's).</summary>
        public static bool Waits(string tool) => KwTools.Pending(tool) != null || (tool == KwCabinTool.Tool && KwCabinTool.Pending != null);

        // A click the tool's edit takes: on its own part, or on something one of its parts stands over.
        private static bool Edits(string tool, KwClick aim)
        {
            KwEditKind? kind = KwEditKinds.For(tool);
            return kind != null && (kind.Edits(Utils.GetPrefabName(aim.Piece.gameObject)) || kind.Over(aim) != null);
        }

        // The tools that take one click on what they stand on.
        private static bool Fitting(Player player, string tool, KwClick aim) => tool switch
        {
            KwSailTool.Tool => KwShapeSites.Mast(aim) != null,
            KwBowspritTool.Tool => KwBowspritTool.Site(aim) != null,
            KwCapTool.Tool => KwCapTool.Hover(player) != null,
            KwLadderTool.Tool => Ladder(player),
            KwCabinTool.Tool => KwCabinPick.Section(player) != null,
            KwDoorwayTool.Tool or KwWindowTool.Tool => KwOpeningHull.ToolOf(aim.Piece) != null || KwOpeningAimed.Of(player).Site != null,
            _ => true,
        };

        // The ladder tool: a hatch (hit, or seen through), or an open edge of the deck or cabin aimed at.
        private static bool Ladder(Player player)
        {
            KwLadderAim aim = KwLadderAim.Of(player);
            return aim.Hatch != null || (aim.Piece != null && KwLadderEdges.Find(aim.Piece, aim.Point, out _) != null);
        }
    }
}
