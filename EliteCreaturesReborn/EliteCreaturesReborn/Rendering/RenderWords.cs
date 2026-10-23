using Translations;

namespace EliteCreaturesReborn.Rendering
{
    /// <summary>What a Rendering Table makes from trophies; kept in its ZDO, so everyone sees the same.</summary>
    internal enum RenderMode
    {
        Ores = 0,
        Ingredients = 1,
        Idols = 2,
    }

    /// <summary>
    /// The Rendering Table's words (English in <c>translations/English.yml</c>) and its hover: the name, what it makes, E
    /// to render (for idols, that a boss trophy is used on it from the hotbar instead) and Shift + E (the gamepad's own
    /// pair) to change what it makes, with the game's own key names. The hover
    /// is a whole localized string, rebuilt only when something in it changes (<see cref="RenderingTable"/>).
    /// </summary>
    internal static class RenderWords
    {
        /// <summary>The piece's word, with its <c>_description</c>.</summary>
        public const string Piece = "piece_ecr_renderingtable";

        public const string Off = "ecr_render_off";
        public const string None = "ecr_render_none";
        public const string NeedBoss = "ecr_render_need_boss";
        public const string Unknown = "ecr_render_unknown";
        public const string NothingMade = "ecr_render_nothing_made";

        private const string Makes = "ecr_render_makes";
        private const string One = "ecr_render_one";
        private const string IdolHint = "ecr_render_idol_hint";
        private const string Change = "ecr_render_change";

        private static readonly string[] ModeWords = { "ecr_render_ores", "ecr_render_ingredients", "ecr_render_idols" };

        private const string UseKey = "[<color=yellow><b>$KEY_Use</b></color>] ";
        private const string AltKeys = "[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] ";
        private const string PadKeys = "[<color=yellow><b>$KEY_AltKeys + $KEY_Use</b></color>] ";

        /// <summary>The hover, localized: the table's name, then what it makes and the keys, or that it is off.</summary>
        public static string Hover(RenderMode mode, bool enabled, bool gamepad)
        {
            string text = "$" + Piece + "\n";
            if (!enabled)
            {
                return Localization.instance.Localize(text + Lang.Get(Off));
            }
            text += Lang.Format(Makes, Lang.Get(ModeWords[(int)mode])) + "\n";
            text += (mode == RenderMode.Idols ? Lang.Get(IdolHint) : UseKey + Lang.Get(One)) + "\n";
            text += (gamepad ? PadKeys : AltKeys) + Lang.Get(Change);
            return Localization.instance.Localize(text);
        }
    }
}
