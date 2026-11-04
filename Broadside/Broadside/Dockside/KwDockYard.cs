namespace Broadside
{
    /// <summary>
    /// The shipwright's yard among the dockside decorations (<see cref="KwDockside.Defs"/>, after the first eight), plain data:
    /// the work pieces (sawhorses, timber drying rack, steam box, scaffold, hull frames, lean-to, tool wall, wood shavings),
    /// then rope and sail (rope coils, rope spool, rope walk, spar rack, sail bolts, sail frame). Models from the workshop's
    /// <c>Assets/Ships/Keelwright/Dockside</c> (each one's HANDOFF.md); recipes of Meadows and Black Forest items only. The
    /// steam box burns a small fire under its kettle and steams at its pipe (<see cref="KwDockHearth"/>); the tool wall hangs
    /// on a wall (its placing box behind it, <see cref="KwDockModel"/>); the shavings get a flat box the hammer can aim at
    /// (<see cref="KwDockModel.FlatBox"/>). Each one's <see cref="KwDockDef.Decor"/> is DECORATE.md's entry, sized from its HANDOFF.
    /// </summary>
    public static class KwDockYard
    {
        private const KwDecorPlace Ground = KwDecorPlace.Ground, Deck = KwDecorPlace.Deck, Shore = KwDecorPlace.Shore, Wall = KwDecorPlace.Wall;

        /// <summary>The yard's decorations in the hammer's order.</summary>
        public static readonly KwDockDef[] Defs =
        {
            new KwDockDef("sawhorses", "Sawhorses", "Two sawhorses with a plank half ripped down its length, the saw left in the cut.", 0.4f, ("Wood", 8), ("Bronze", 1))
            {
                Decor = new KwDecorFit(2.4f, 0.8f, 0.95f, Ground | Deck) { Spacing = 0.8f },
            },
            new KwDockDef("dryingrack", "Timber drying rack", "Planks seasoning on a two-sided rack, sticks laid between the layers.", 0f, ("Wood", 14), ("RoundLog", 4))
            {
                Decor = new KwDecorFit(3f, 1.2f, 2.2f, Ground | Deck) { Spacing = 1f },
            },
            new KwDockDef("steambox", "Steam box", "A long box for steaming planks to bend, its kettle simmering on a small fire.", 0.4f, ("Wood", 12), ("Bronze", 2), ("Stone", 4))
            {
                NotOnWood = true, Fit = KwDockHearth.Fit(0.22f, 3f, 0.6f),
                Decor = new KwDecorFit(3.3f, 0.8f, 1.4f, Ground) { Spacing = 1.2f },
            },
            new KwDockDef("scaffold", "Scaffold", "A lashed pole scaffold with a plank platform and a ladder.", 0f, ("Wood", 14), ("RoundLog", 4), ("LeatherScraps", 4))
            {
                Decor = new KwDecorFit(4f, 1.2f, 2.2f, Ground) { Spacing = 1f },
            },
            new KwDockDef("hullribs", "Hull frames", "A ship's keel and ribs standing in the yard, a few strakes on.", 0f, ("Wood", 20), ("RoundLog", 12))
            {
                Decor = new KwDecorFit(3.6f, 10f, 3f, Ground) { Spacing = 1.5f },
            },
            new KwDockDef("leanto", "Lean-to shed", "An open timber shed under a sloping plank roof.", 0f, ("Wood", 20), ("RoundLog", 4))
            {
                Decor = new KwDecorFit(4.3f, 3.3f, 3.2f, Ground) { Spacing = 1.2f },
            },
            new KwDockDef("toolwall", "Tool wall", "A shipwright's tools on a board hung on a wall: broadaxe, adzes, augers, mallets and a drawknife.", 0.4f, ("Wood", 6), ("Bronze", 2))
            {
                Decor = new KwDecorFit(2f, 0.25f, 1.6f, Wall) { Spacing = 0.4f, Lift = 0.6f },
            },
            new KwDockDef("shavings", "Wood shavings", "Sawdust, curls and chips where the planks were worked.", 0f, ("Wood", 2))
            {
                Fit = KwDockModel.FlatBox,
                Decor = new KwDecorFit(2f, 2f, 0.05f, Ground | Deck) { Spacing = 0.2f, InPath = true },
            },
            new KwDockDef("ropecoils", "Rope coils", "Coils of hawser, line and cord laid down ready.", 0f, ("Wood", 2), ("LeatherScraps", 6))
            {
                Decor = new KwDecorFit(1.4f, 1.2f, 0.4f, Deck | Ground | Shore) { Spacing = 0.5f },
            },
            new KwDockDef("ropespool", "Rope spool", "A big reel of rope on its stand, the end trailing.", 0f, ("Wood", 8), ("LeatherScraps", 4))
            {
                Decor = new KwDecorFit(1.2f, 1.05f, 1.3f, Deck | Ground) { Spacing = 0.6f },
            },
            new KwDockDef("ropewalk", "Rope walk", "A rope-maker's jack and sledge, the strands laid out between them.", 0.4f, ("Wood", 10), ("LeatherScraps", 6), ("Stone", 2), ("Bronze", 1))
            {
                Decor = new KwDecorFit(4f, 1f, 1.6f, Ground) { Spacing = 1f },
            },
            new KwDockDef("sparrack", "Spar rack", "A mast, two yards and spare oars laid up on a rack.", 0f, ("Wood", 14), ("RoundLog", 6))
            {
                Decor = new KwDecorFit(4.5f, 1f, 2f, Ground | Deck) { Spacing = 1f },
            },
            new KwDockDef("sailbolts", "Sail bolts", "Bolts of sail cloth on a pallet, one sheet unrolled.", 0f, ("DeerHide", 6), ("Wood", 2), ("LeatherScraps", 2))
            {
                Decor = new KwDecorFit(1.6f, 1.1f, 0.7f, Deck | Ground) { Spacing = 0.6f },
            },
            new KwDockDef("sailframe", "Sail frame", "A sail laced into a frame for mending, the sailmaker's bench beside it.", 0f, ("Wood", 10), ("DeerHide", 4), ("LeatherScraps", 4))
            {
                Decor = new KwDecorFit(2.6f, 0.9f, 2.6f, Ground | Deck) { Spacing = 1f },
            },
        };
    }
}
