using System.Text;
using EliteCrafting.Affixes;
using EliteCrafting.Config;
using EliteCrafting.Items;
using EliteCrafting.Rules;
using EliteCrafting.Text;

namespace EliteCrafting.Display
{
    /// <summary>
    /// Builds the tooltip affix block (display.md section 3), localized, top to bottom: rarity line (with the tier
    /// ceiling at Full), newer-format notice, affix lines, honed/tempered, sealed, pending sigil. Lines that do not
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
            AffixLines.Append(Sb, state, detail, showDormant);
            AppendRefine(Sb, state, item);
            AppendSealed(Sb, state);
            SigilLine.Append(Sb, state, detail);
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
                string ceiling = Words.Localize("$ecf_ui_tier_ceiling", ItemTier.Of(item).ToString());
                sb.Append("  ").Append(RarityPalette.Grey).Append(ceiling).Append(RarityPalette.Close);
            }
        }

        /// <summary>Honed on weapons and tools (damage), tempered on armor and shields (quality.md section 6).</summary>
        private static void AppendRefine(StringBuilder sb, ItemState state, ItemDrop.ItemData item)
        {
            if (state.Refine <= 0f)
            {
                return;
            }
            string key = IsHoned(ItemSlots.SlotOf(item)) ? "$ecf_ui_honed" : "$ecf_ui_tempered";
            sb.Append('\n').Append(Words.Localize(key, DisplayWords.Plain(state.Refine)));
        }

        private static bool IsHoned(ItemSlot slot)
        {
            return slot == ItemSlot.MeleeWeapon || slot == ItemSlot.RangedWeapon || slot == ItemSlot.MagicWeapon
                || slot == ItemSlot.Tool;
        }

        /// <summary>"Sealed: Corrupted" in dark red; an unknown reason id shows the generic sealed text.</summary>
        private static void AppendSealed(StringBuilder sb, ItemState state)
        {
            if (!state.IsSealed)
            {
                return;
            }
            string reasonKey = "ecf_ui_sealed_" + state.SealedReason;
            string text = DisplayWords.Has(reasonKey)
                ? Words.Localize("$ecf_ui_sealed", Words.Localize("$" + reasonKey))
                : Words.Localize("$ecf_ui_sealed_generic");
            sb.Append('\n').Append(RarityPalette.SealedRed).Append(text).Append(RarityPalette.Close);
        }
    }
}
