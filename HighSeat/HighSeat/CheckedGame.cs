namespace HighSeat
{
    /// <summary>
    /// The game version the patches that stand in for the game's own code (<see cref="SendChecks"/>,
    /// <see cref="ListChecks"/>) were checked against. Any other version leaves those features off and the game's own
    /// code runs: after a game update, read the methods they copy, and when the patches still do exactly what they do,
    /// put the new version here.
    /// </summary>
    internal static class CheckedGame
    {
        private static readonly GameVersion Checked = new GameVersion(1, 0, 21);

        /// <summary>Null when the running game is the checked version, else why not, for the log.</summary>
        public static string Verdict()
        {
            GameVersion running = global::Version.CurrentVersion;
            return running == Checked ? null : $"it was checked against Valheim {Checked}, this is {running}";
        }
    }
}
