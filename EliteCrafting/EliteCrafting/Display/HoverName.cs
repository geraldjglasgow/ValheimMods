using System;
using EliteCrafting.Affixes;
using EliteCrafting.Config;
using EliteCrafting.Text;

namespace EliteCrafting.Display
{
    /// <summary>
    /// Colors the item name at the start of an already localized hover text. The hover text is rebuilt by the game
    /// every frame while the player looks at the item, so the last input and output are remembered and a repeat costs
    /// one content compare and no allocation. Viewing client only.
    /// </summary>
    internal static class HoverName
    {
        private static string? _lastInput;
        private static string? _lastTag;
        private static string? _lastOutput;

        /// <summary>The name's color tag, or null when the name stays vanilla.</summary>
        public static string? Tag(ItemDrop.ItemData? item)
        {
            if (item == null || !ModSettings.ColoredItemNames.Value)
            {
                return null;
            }
            return RarityPalette.NameTag(ItemState.Read(item).Rarity);
        }

        /// <summary>Wraps the localized name at the start of <paramref name="text"/>; unchanged if it is not there.</summary>
        public static string Color(string text, string nameToken, string tag)
        {
            if (ReferenceEquals(tag, _lastTag) && string.Equals(text, _lastInput))
            {
                return _lastOutput!;
            }
            string name = Words.Localize(nameToken);
            string output = name.Length > 0 && text.StartsWith(name, StringComparison.Ordinal)
                ? tag + name + RarityPalette.Close + text.Substring(name.Length)
                : text;
            _lastInput = text;
            _lastTag = tag;
            _lastOutput = output;
            return output;
        }
    }
}
