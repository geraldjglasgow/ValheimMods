namespace GrindstoneSkills
{
    /// <summary>
    /// The words of Read the rock, added under the game's hover name of an ore deposit (English, like the mod's other
    /// texts; the stars are gold ★ glyphs, <see cref="StarText"/>):
    /// <list type="bullet">
    /// <item>"Rich vein ★★" for a vein with stars, "Plain vein" for one without, so a player who has the milestone can
    /// tell a plain vein from a hover that shows nothing.</item>
    /// <item>"9 of 12 chunks left" under it for a multi-chunk rock (MineRock5 or MineRock), as this machine knows the
    /// chunks: a broken chunk reaches every client at once (the game's area health RPC).</item>
    /// </list>
    /// </summary>
    public static class VeinText
    {
        /// <summary>The lines for an ore deposit, without a leading line break.</summary>
        public static string Lines(Rock rock)
        {
            string vein = VeinLine(Veins.Stars(rock));
            return rock.HasChunks ? vein + "\n" + ChunksLine(rock) : vein;
        }

        public static string VeinLine(int stars) => stars > 0 ? "Rich vein " + StarText.Colored(stars) : "Plain vein";

        public static string ChunksLine(Rock rock) => $"{RockChunks.Left(rock)} of {RockChunks.Count(rock)} chunks left";
    }
}
