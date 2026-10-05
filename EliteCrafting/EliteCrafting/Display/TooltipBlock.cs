using System.Text;
using EliteCrafting.Affixes;
using EliteCrafting.Config;
using EliteCrafting.Items;
using EliteCrafting.Rules;
using EliteCrafting.Text;

namespace EliteCrafting.Display
{
    /// <summary>
    /// Builds the tooltip affix block (display.md section 3), localized, top to bottom: rarity line, at Full the item's
    /// class and level (classes-and-tiers.md section 9), newer-format notice, affix lines, sealed. Lines that do not
    /// apply are left out; an item with nothing to show gets an empty block. Called once per item state and detail
    /// level by <see cref="DisplayCache"/>, never per frame. Runs on the viewing client only.
    /// </summary>
    internal static class TooltipBlock
    {
        // Our own builder: the game's shared tooltip StringBuilder is never touched.
        private static readonly StringBuilder Sb = new StringBuilder(512);

        public static string Build(ItemState state, ItemDrop.ItemData item, TooltipDetail detail, bool showDormant)
        {
            Sb.Clear();
            AppendRarity(Sb, state, item, detail);
            if (state.IsNewerFormat)
            {
                Sb.Append('\n').Append(RarityPalette.Grey).Append(Words.Localize("$ecf_ui_newer_format")).Append(RarityPalette.Close);
            }
            AffixLines.Append(Sb, state, detail, showDormant, ItemClasses.Classify(item).DamageScale);
            AppendSealed(Sb, state);
            // The block follows the vanilla tooltip after one blank line.
            return Sb.Length == 0 ? "" : "\n" + Sb.ToString();
        }

        private static void AppendRarity(StringBuilder sb, ItemState state, ItemDrop.ItemData item, TooltipDetail detail)
        {
            if (state.Rarity != null)
            {
                string name = DisplayWords.Name(state.Rarity.Name, state.Rarity.Id);
                sb.Append('\n').Append(RarityPalette.Tag(state.Rarity)).Append("<b>").Append(name).Append("</b>")
                    .Append(RarityPalette.Close);
            }
            else if (state.IsUnknownRarity)
            {
                sb.Append('\n').Append(RarityPalette.Grey).Append(DisplayWords.Verbatim(state.RarityId!)).Append(RarityPalette.Close);
            }
            else
            {
                return;
            }
            if (detail == TooltipDetail.Full)
            {
                sb.Append('\n').Append(RarityPalette.Grey).Append(ClassLine(item)).Append(RarityPalette.Close);
            }
        }

        /// <summary>"Swords · item level 4"; "item level 4" alone for an item without a class.</summary>
        private static string ClassLine(ItemDrop.ItemData item)
        {
            ItemClass? itemClass = ItemClasses.ClassOf(item);
            string level = ItemTier.Of(item).ToString();
            return itemClass == null
                ? Words.Localize("$ecf_ui_item_level", level)
                : Words.Localize("$ecf_ui_class_level", DisplayWords.Name(itemClass.Name, itemClass.Id), level);
        }

        /// <summary>"Sealed" in dark red, whatever sealed the item.</summary>
        private static void AppendSealed(StringBuilder sb, ItemState state)
        {
            if (!state.IsSealed)
            {
                return;
            }
            string text = Words.Localize("$ecf_ui_sealed_generic");
            sb.Append('\n').Append(RarityPalette.SealedRed).Append(text).Append(RarityPalette.Close);
        }
    }
}
