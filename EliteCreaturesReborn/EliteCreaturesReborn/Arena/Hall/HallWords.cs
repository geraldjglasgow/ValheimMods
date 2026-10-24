namespace EliteCreaturesReborn.Arena.Hall
{
    /// <summary>
    /// The keys of the hall's words: Hel's name, E's action on her, and a death in her river. Their English is in
    /// <c>translations/English.yml</c>; the Translations library writes them into the game's table in the
    /// player's language.
    /// </summary>
    internal static class HallWords
    {
        /// <summary>Hel's name on her hover.</summary>
        public const string Hel = "ecr_arena_hel";

        /// <summary>E's action on Hel, "Begin the trial: $1" ($1 the trial brought from Helgrind).</summary>
        public const string Begin = "hud_ecr_arena_begin_trial";

        /// <summary>The centre message of a death in Hel's river, "Hel's river takes you."</summary>
        public const string River = "msg_ecr_arena_river";
    }
}
