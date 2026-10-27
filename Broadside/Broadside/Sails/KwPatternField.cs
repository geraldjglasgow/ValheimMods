using System.Collections.Generic;

namespace Broadside
{
    /// <summary>
    /// The pattern rows on the sail and bowsprit windows (<see cref="KwForm"/>): the pattern (<see cref="KwSailPattern"/>),
    /// then only what it needs: its colour, how many stripes, which emblem (the game's banners by name and the player's
    /// pictures, <see cref="KwSailEmblems"/>, listed again each time the rows are made). A new pattern builds the window
    /// again (the caller's), so the rows follow it; the rows shown are remembered here so they are read as they are.
    /// </summary>
    internal static class KwPatternField
    {
        public const string Key = "pattern";
        private const string ColourKey = "patternColour", StripesKey = "patternStripes", EmblemKey = "patternEmblem";
        private static List<(string Id, string Label)> emblems = new List<(string Id, string Label)>();
        private static int shown;

        /// <summary>The rows for a pattern (an emblem pattern with none picked shows the first offered).</summary>
        public static List<KwField> Fields(KwSailPattern pattern, string label)
        {
            shown = pattern.Kind;
            List<KwField> fields = new List<KwField> { KwField.Choice(Key, label, KwSailPattern.Labels, pattern.Kind) };
            if (!pattern.IsPlain)
                fields.Add(KwField.Choice(ColourKey, "Pattern colour", KwSailDye.Labels, pattern.Colour));
            if (pattern.Striped)
                fields.Add(KwField.Number(StripesKey, "Stripes", pattern.Count, KwSailPattern.MinStripes, KwSailPattern.MaxStripes, 1f, ""));
            if (pattern.HasEmblem)
            {
                emblems = KwSailEmblems.Choices();
                if (pattern.Art.Length > 0 && !emblems.Exists(e => e.Id == pattern.Art))
                    emblems.Add((pattern.Art, "Missing picture"));
                string[] labels = emblems.Count > 0 ? emblems.ConvertAll(e => e.Label).ToArray() : new[] { "None" };
                fields.Add(KwField.Choice(EmblemKey, "Emblem", labels, emblems.FindIndex(e => e.Id == pattern.Art)));
            }
            return fields;
        }

        /// <summary>The pattern the rows hold; what they do not show is kept from the one before (was).</summary>
        public static KwSailPattern Read(KwFormValues values, KwSailPattern was)
        {
            int kind = values.ChoiceIndex(Key);
            int colour = shown != KwSailPattern.Plain ? values.ChoiceIndex(ColourKey) : was.Colour;
            int stripes = shown == KwSailPattern.Upright || shown == KwSailPattern.Across ? (int)values.Number(StripesKey) : was.Count;
            string art = shown == KwSailPattern.Emblem ? Emblem(values.ChoiceIndex(EmblemKey)) : was.Art;
            if (kind == KwSailPattern.Emblem && art.Length == 0)
                art = Emblem(0);
            return new KwSailPattern(kind, colour, stripes, art);
        }

        // The emblem at a place in the list last offered (the first when there is none there); empty when none is offered.
        private static string Emblem(int index)
        {
            if (emblems.Count == 0)
                emblems = KwSailEmblems.Choices();
            return emblems.Count == 0 ? "" : emblems[index >= 0 && index < emblems.Count ? index : 0].Id;
        }

        /// <summary>What the window's note line says about a pattern: where the player's own pictures come from and who sees them.</summary>
        public static string Note(KwSailPattern pattern) => pattern.HasEmblem
            ? "Pictures of your own: PNG files in BepInEx/config/Broadside.Sails. Other players see them only with the same file."
            : "";
    }
}
