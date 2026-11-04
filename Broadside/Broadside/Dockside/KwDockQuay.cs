namespace Broadside
{
    /// <summary>
    /// The quay among the dockside decorations (<see cref="KwDockside.Defs"/>, after the yard's), plain data: ship gear (tar
    /// pot, nail barrels, ship's anchor, fenders, pilings, gangplank, salvage heap, hanging net), then light and signal
    /// (lantern pole, signal bell, pennant line, notice board). Models from the workshop's
    /// <c>Assets/Ships/Keelwright/Dockside</c>; recipes of Meadows and Black Forest items only. The tar pot burns a small fire
    /// (<see cref="KwDockHearth"/>), the lantern pole lights from dusk to dawn (<see cref="KwDockLantern"/>) and on a dock is
    /// seated over a piling without its cairn (<see cref="KwDockMooring"/>, <see cref="KwDockTrim"/>), the bell rings
    /// (<see cref="KwDockBell"/>); the fenders and the hanging net hang on a wall. The anchor, fenders and pilings are made for
    /// the water (<see cref="KwDecorFit.Wet"/>: placed at the waterline and in water). The gangplank has no
    /// auto-decorate metadata: a ramp only means something set against a dock or a ship's side, so it is placed by hand.
    /// </summary>
    public static class KwDockQuay
    {
        private const KwDecorPlace Ground = KwDecorPlace.Ground, Deck = KwDecorPlace.Deck, Shore = KwDecorPlace.Shore, Wall = KwDecorPlace.Wall;
        private const KwDecorPlace Edge = KwDecorPlace.Edge, Shallows = KwDecorPlace.Shallows;

        /// <summary>The quay's decorations in the hammer's order.</summary>
        public static readonly KwDockDef[] Defs =
        {
            new KwDockDef("tarpot", "Tar pot", "A cauldron of pine tar kept warm over a small fire, a bucket and brush beside it.", 0.4f, ("Stone", 4), ("Bronze", 2), ("Resin", 4), ("Wood", 4))
            {
                NotOnWood = true, Fit = KwDockHearth.Fit(0.32f, 4f, 0f),
                Decor = new KwDecorFit(1.2f, 1.2f, 1.3f, Ground) { Spacing = 1f },
            },
            new KwDockDef("nailbarrels", "Nail barrels", "Barrels of ship's nails and spikes, one open, bundles of treenails beside them.", 0.4f, ("Wood", 8), ("BronzeNails", 10))
            {
                Decor = new KwDecorFit(1.5f, 1f, 1f, Ground | Deck) { Spacing = 0.6f },
            },
            new KwDockDef("ironanchor", "Ship's anchor", "A forged anchor on its wooden stock, its chain heaped beside it.", 0.4f, ("Bronze", 4), ("Wood", 2))
            {
                Decor = new KwDecorFit(2f, 1.4f, 0.6f, Shore | Ground) { Spacing = 0.8f, Wet = true },
            },
            new KwDockDef("fenders", "Fenders", "Woven rope fenders and a bundle of sticks on a rail, hung on a dock's side or a wall to spare a hull.", 0f, ("Wood", 6), ("LeatherScraps", 6))
            {
                Decor = new KwDecorFit(2f, 0.5f, 1.2f, Edge | Wall) { Spacing = 0.8f, Wet = true, Lift = 0.3f },
            },
            new KwDockDef("pilings", "Pilings", "Three tarred posts lashed together, crusted with weed and barnacles.", 0f, ("RoundLog", 6), ("Resin", 2), ("LeatherScraps", 2))
            {
                Decor = new KwDecorFit(1f, 1f, 3f, Shallows | Shore) { Spacing = 1f, Wet = true, MaxDepth = 2f },
            },
            new KwDockDef("gangplank", "Gangplank", "A cleated plank ramp on a trestle, to board from a dock.", 0f, ("Wood", 10), ("LeatherScraps", 2)),
            new KwDockDef("salvage", "Salvage heap", "A wrecked hull section, a broken mast and a barrel washed up together.", 0f, ("Wood", 12), ("LeatherScraps", 4), ("DeerHide", 2))
            {
                Decor = new KwDecorFit(4f, 2.8f, 1.9f, Ground | Shore) { Spacing = 1.2f },
            },
            new KwDockDef("hangnet", "Hanging net", "A fishing net on a board hung on a wall, cork floats along its head and stones at its foot.", 0f, ("Wood", 3), ("LeatherScraps", 5), ("Stone", 2))
            {
                Decor = new KwDecorFit(2.5f, 0.3f, 2.2f, Wall) { Spacing = 0.4f, Lift = 0.3f },
            },
            new KwDockDef("lanternpole", "Lantern pole", "A glass lantern hung from an iron arm on a hewn post, its flame lit from dusk to dawn.", 0.4f, ("Wood", 6), ("Stone", 4), ("Bronze", 2), ("Resin", 2))
            {
                // on a dock near a side it is seated on a piling, its cairn left off (KwDockMooring, KwDockTrim)
                Usage = Piece.UsageTagFlags.Lighting, Fit = KwDockLantern.Fit,
                Decor = new KwDecorFit(1f, 0.8f, 2.8f, Ground | Deck) { Spacing = 1f },
            },
            // on a post of the dock's own wood just off a dock's side, its arm in over the deck (KwDockLanternPiece); its cost
            // here is what the hammer shows at rest: aimed at a dock it is that dock's wood for the post's length and the lamp's
            new KwDockDef("lantern", "Dock lantern", "A glass lantern on an iron arm, on a post of the dock's own wood standing in the water beside it, its flame lit from dusk to dawn.", 0.4f, ("Wood", 2), ("Bronze", 2), ("Resin", 2))
            {
                // placed by hand only: the Auto-decorate tool seats lantern poles on the docks' own pilings (KwDecorLights)
                Bundle = "bs_dock_lanternhead", Usage = Piece.UsageTagFlags.Lighting, Fit = KwDockLanternPiece.Fit,
            },
            new KwDockDef("bell", "Signal bell", "A bronze bell hung between two posts; ring it to call the crew.", 0.45f, ("Bronze", 6), ("Wood", 8), ("LeatherScraps", 2))
            {
                Fit = KwDockBell.Fit,
                Decor = new KwDecorFit(1.4f, 0.8f, 2.6f, Ground | Deck | Shore) { Spacing = 1f },
            },
            new KwDockDef("pennants", "Pennant line", "A line of coloured pennants strung between two posts.", 0f, ("Wood", 6), ("LeatherScraps", 3), ("DeerHide", 2))
            {
                Decor = new KwDecorFit(4f, 0.5f, 3f, Ground | Deck) { Spacing = 1f },
            },
            new KwDockDef("noticeboard", "Notice board", "A roofed board with sail plans and notices pinned up.", 0f, ("Wood", 8), ("DeerHide", 1))
            {
                Decor = new KwDecorFit(1.6f, 0.4f, 2.2f, Ground | Deck) { Spacing = 1.5f },
            },
        };
    }
}
