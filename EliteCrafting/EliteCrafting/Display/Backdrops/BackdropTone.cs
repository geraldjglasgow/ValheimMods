using System;
using System.Collections.Generic;
using EliteCrafting.Affixes;
using EliteCrafting.Rules;
using UnityEngine;

namespace EliteCrafting.Display.Backdrops
{
    /// <summary>
    /// The colour of a magic item's backdrop: its rarity's hue at full value and less saturation (the full colours,
    /// #1EFF00 and #0070DD, glow like neon on the wood; judged on the 2026-10-05 mockups), opaque, since the sprite
    /// carries the alpha. One tone per palette colour, worked out once. Normal, plain and unknown-rarity items have none.
    /// </summary>
    internal static class BackdropTone
    {
        private const float Saturation = 0.6f;

        private static readonly Dictionary<string, Color> Tones = new Dictionary<string, Color>(StringComparer.Ordinal);

        /// <summary>The item's backdrop tone; false when it shows none. Plain items return at the custom-data check.</summary>
        public static bool TryGet(ItemDrop.ItemData? item, out Color tone)
        {
            tone = default;
            if (item == null || item.m_customData == null || item.m_customData.Count == 0)
            {
                return false;
            }
            RarityDef? rarity = ItemState.Read(item).Rarity;
            if (rarity == null || rarity.IsBase)
            {
                return false;
            }
            if (!Tones.TryGetValue(rarity.Color, out tone))
            {
                tone = Tone(rarity.Color32);
                Tones[rarity.Color] = tone;
            }
            return true;
        }

        private static Color Tone(Color full)
        {
            Color.RGBToHSV(full, out float hue, out float saturation, out _);
            return Color.HSVToRGB(hue, saturation * Saturation, 1f);
        }
    }
}
