using System.Linq;
using EliteCreaturesPack.Custom.Humans;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Export
{
    /// <summary>
    /// `human:` of an export: the ranges a human's look is rolled within, read from the prefab's
    /// <see cref="HumanAppearance"/>, where the human step lays them (the human base's own values, anything the game draws
    /// on players, with what a custom human's definitions set laid over them). Every key is written, so the block reads
    /// back as exactly these ranges.
    /// </summary>
    internal static class HumanLookExport
    {
        public static void Write(ExportWriter writer, ExportSource source)
        {
            HumanAppearance? look = source.Prefab.GetComponent<HumanAppearance>();
            if (look == null)
            {
                writer.Note("human: - only for a creature whose base is Human");
                return;
            }
            writer.Open("human");
            WriteGender(writer, look.Gender);
            writer.Note("hair and beard: the game's Hair... and Beard... items; [] is any of them, and none is bald or beardless");
            writer.Key("hair", ExportValues.Names(look.Hair ?? new string[0]));
            writer.Key("beard", ExportValues.Names(look.Beard ?? new string[0]));
            writer.Switch("beardless women", look.BeardlessWomen);
            writer.Note("hair colours: [] is the game's natural range; or a list such as [\"#d0b080\", [0.3, 0.2, 0.1]]");
            writer.Key("hair colours", "[" + string.Join(", ", (look.HairColours ?? new Color[0]).Select(ExportValues.Colour)) + "]");
            WriteSkinTone(writer, look.SkinTone);
            writer.Close();
        }

        private static void WriteGender(ExportWriter writer, string? gender)
        {
            string word = (gender ?? "").Trim().ToLowerInvariant();
            if (word == "male" || word == "female" || word == "random")
            {
                writer.Key("gender", word);
                return;
            }
            writer.Note($"gender: '{gender}' - not male, female or random, so it is left as it is");
        }

        private static void WriteSkinTone(ExportWriter writer, Vector2 tone)
        {
            if (ExportValues.Within(tone.x, 0f, 1f) && ExportValues.Within(tone.y, 0f, 1f) && tone.x <= tone.y)
            {
                writer.Note("skin tone: 0 lightest to 1 darkest, one number or [low, high]");
                writer.Key("skin tone", ExportValues.Range(tone.x, tone.y));
                return;
            }
            writer.Note($"skin tone: {ExportValues.Number(tone.x)} to {ExportValues.Number(tone.y)} - outside 0 (lightest) to 1 "
                + "(darkest), so it is left as it is");
        }
    }
}
