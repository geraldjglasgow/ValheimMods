using System.Runtime.CompilerServices;
using EliteCreaturesReborn.Breeding;
using EliteCreaturesReborn.Config;
using EliteCreaturesReborn.Traits;
using UnityEngine;

namespace EliteCreaturesReborn.Display
{
    /// <summary>
    /// The line an egg shows about what it will hatch - "Hatches with 2 stars, Leeching" - on its hover text on the
    /// ground and on its tooltip in an inventory. Without it an egg's inheritance would be invisible until it hatched.
    /// The mutation word takes its star colour, the same palette the nameplate uses. Both are asked for every frame, so
    /// the line is kept beside the egg and drawn again only when its stored traits or the palette change.
    /// </summary>
    internal static class EggText
    {
        private sealed class Memo
        {
            public string Raw = "";
            public int Palette = -1;
            public string? Line;
        }

        private static readonly ConditionalWeakTable<ItemDrop.ItemData, Memo> Kept = new ConditionalWeakTable<ItemDrop.ItemData, Memo>();

        /// <summary>The line for an egg carrying traits from this mod, or null for any other item.</summary>
        public static string? For(ItemDrop.ItemData item)
        {
            if (item == null || !item.m_customData.TryGetValue(TraitKeys.EggTraits, out string raw))
            {
                return null;
            }
            Memo memo = Kept.GetOrCreateValue(item);
            if (memo.Raw != raw || memo.Palette != PaletteSettings.Version)
            {
                memo.Raw = raw;
                memo.Palette = PaletteSettings.Version;
                memo.Line = Draw(item);
            }
            return memo.Line;
        }

        private static string? Draw(ItemDrop.ItemData item)
        {
            Lineage.Newborn? born = EggData.Read(item);
            if (born == null)
            {
                return null;
            }
            CreatureTraits traits = born.Traits;
            if (traits.Stars == 0 && !traits.Any)
            {
                return "Hatches plain";
            }
            string text = "Hatches with " + (traits.Stars == 1 ? "1 star" : $"{traits.Stars} stars");
            foreach (Mutation mutation in traits.Active())
            {
                text += $", <color=#{ColorUtility.ToHtmlStringRGB(PaletteSettings.Of(mutation))}>{MutationCatalog.Word(mutation)}</color>";
            }
            return text;
        }
    }
}
