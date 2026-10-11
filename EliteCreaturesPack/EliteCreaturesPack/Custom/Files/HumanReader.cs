using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EliteCreaturesPack.Custom.Definitions;
using EliteCreaturesPack.Custom.Humans;
using UnityEngine;
using YamlConfig;

namespace EliteCreaturesPack.Custom.Files
{
    /// <summary>
    /// Reads `human:` into the <see cref="HumanLook"/> contract. A key left out stays null, so it keeps what the base gave
    /// (for the human base, anything the game draws on players). Whether the creature really is a human is known only
    /// once bases are resolved, and whether a hair or beard is one of the game's only once the world's ObjectDB is up, so
    /// the human step checks both.
    /// </summary>
    internal static class HumanReader
    {
        private static readonly string[] Genders = { "male", "female", "random" };

        public static HumanLook? Read(YamlNode entry, FieldReader fields)
        {
            YamlNode? block = fields.Block(entry, "human");
            if (block == null)
            {
                return null;
            }
            HumanLook look = new HumanLook
            {
                Gender = ReadGender(fields.At(block, "gender"), fields),
                Hair = fields.Names(block, "hair"),
                Beard = fields.Names(block, "beard"),
                BeardlessWomen = fields.Switch(block, "beardless women"),
                HairColours = ReadColours(fields.At(block, "hair colours"), fields),
            };
            NumberRange? skin = fields.Range(block, "skin tone", 0f, 1f);
            look.SkinTone = skin == null ? (Vector2?)null : new Vector2(skin.Value.Min, skin.Value.Max);
            return look;
        }

        private static string? ReadGender(YamlNode node, FieldReader fields)
        {
            string? text = fields.TextOf(node);
            if (text == null)
            {
                return null;
            }
            string? gender = Array.Find(Genders, word => string.Equals(word, text, StringComparison.OrdinalIgnoreCase));
            if (gender == null)
            {
                node.Error($"'{text}' is not a gender: male, female or random");
            }
            return gender;
        }

        /// <summary>
        /// One colour or a list of colours, each "#rrggbb" or [r, g, b(, a)]. A list of numbers alone is one colour in
        /// numbers (no colour is a bare number), so <c>[0.4, 0.3, 0.2]</c> is one colour and <c>[[0.4, 0.3, 0.2],
        /// "#d0b080"]</c> two. Null when the key is not there; an empty list is set (the game's natural range).
        /// </summary>
        private static List<Color>? ReadColours(YamlNode node, FieldReader fields)
        {
            if (!FieldReader.Has(node))
            {
                return null;
            }
            List<Color> colours = new List<Color>();
            if (node.Kind == YamlNodeKind.Scalar || IsNumbers(node))
            {
                AddColour(colours, fields.ColourOf(node));
                return colours;
            }
            foreach (YamlNode item in node.Items)
            {
                AddColour(colours, fields.ColourOf(item));
            }
            return colours;
        }

        private static bool IsNumbers(YamlNode node) =>
            node.Kind == YamlNodeKind.List && node.Count > 0 && node.Items.All(item => item.Kind == YamlNodeKind.Scalar
                && float.TryParse(item.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out _));

        private static void AddColour(List<Color> colours, Color? colour)
        {
            if (colour != null)
            {
                colours.Add(colour.Value);
            }
        }
    }
}
