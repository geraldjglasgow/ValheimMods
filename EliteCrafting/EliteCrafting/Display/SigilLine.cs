using System.Text;
using EliteCrafting.Affixes;
using EliteCrafting.Config;
using EliteCrafting.Items;
using EliteCrafting.Rules;
using EliteCrafting.Text;

namespace EliteCrafting.Display
{
    /// <summary>
    /// The pending-sigil line (sigils.md section 1, display.md section 3): "Pending: Sigil of War" in the sigil's stone
    /// tint (<c>Items/StoneVisuals</c>; grey when it has none), and under it the sigil's description in grey, left out
    /// at Compact detail. A sigil id the running rules no longer define still shows, by its translated name or raw id.
    /// </summary>
    internal static class SigilLine
    {
        public static void Append(StringBuilder sb, ItemState state, TooltipDetail detail)
        {
            if (!state.HasSigil)
            {
                return;
            }
            string id = state.SigilId!;
            StoneDef? sigil = ActiveRules.Current.Stone(id);
            string name = DisplayWords.Name(sigil?.Name ?? "$ecf_stone_" + id, id);
            string tag = StoneVisuals.HasTint(id) ? RarityPalette.OpenTag(StoneVisuals.Tint(id)) : RarityPalette.Grey;
            sb.Append('\n').Append(tag).Append(Words.Localize("$ecf_ui_pending_sigil", name)).Append(RarityPalette.Close);
            string descKey = "ecf_stone_" + id + "_desc";
            if (detail != TooltipDetail.Compact && DisplayWords.Has(descKey))
            {
                sb.Append("\n  ").Append(RarityPalette.Grey).Append(Words.Localize("$" + descKey)).Append(RarityPalette.Close);
            }
        }
    }
}
