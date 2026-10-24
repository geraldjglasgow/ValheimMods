namespace EliteCreaturesReborn.Arena.Gate
{
    /// <summary>
    /// The keys of Helgrind's words: the piece's name and description, its hover line and the message of a closed gate.
    /// Their English is in <c>translations/English.yml</c>; the Translations library writes them into the game's
    /// table in the player's language.
    /// </summary>
    internal static class GateWords
    {
        /// <summary>The piece's name, "Helgrind".</summary>
        public const string Piece = "piece_ecr_helgrind";

        public const string Description = Piece + "_description";

        /// <summary>The hover's E line: the trial the gate is set to ("Trial: $1").</summary>
        public const string Trial = "hud_ecr_helgrind_trial";

        /// <summary>The hover's line while the gate is open.</summary>
        public const string Open = "ecr_helgrind_open";

        /// <summary>The hover's line, and the centre message on walking in, while Arena is off or the hall not built.</summary>
        public const string Closed = "ecr_helgrind_closed";
    }
}
