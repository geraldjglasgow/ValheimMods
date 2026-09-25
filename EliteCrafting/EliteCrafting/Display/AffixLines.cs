using System.Text;
using EliteCrafting.Affixes;
using EliteCrafting.Config;
using EliteCrafting.Rules;
using EliteCrafting.Text;

namespace EliteCrafting.Display
{
    /// <summary>
    /// The affix lines of the tooltip block (display.md section 3): active affixes first in stored order, then the
    /// dormant ones greyed, then (Full only) unreadable segments. Built once per item state and detail level.
    /// </summary>
    internal static class AffixLines
    {
        public static void Append(StringBuilder sb, ItemState state, TooltipDetail detail, bool showDormant)
        {
            for (int i = 0; i < state.AffixCount; i++)
            {
                if (state.IsActiveAt(i))
                {
                    AppendLine(sb, state, i, detail, dormant: false);
                }
            }
            for (int i = 0; showDormant && i < state.AffixCount; i++)
            {
                if (!state.IsActiveAt(i))
                {
                    AppendLine(sb, state, i, detail, dormant: true);
                }
            }
            for (int i = 0; detail == TooltipDetail.Full && i < state.Unreadable.Count; i++)
            {
                sb.Append('\n').Append(RarityPalette.Grey)
                    .Append(Words.Localize("$ecf_ui_unreadable", DisplayWords.Verbatim(state.Unreadable[i])))
                    .Append(RarityPalette.Close);
            }
        }

        private static void AppendLine(StringBuilder sb, ItemState state, int index, TooltipDetail detail, bool dormant)
        {
            AffixRoll roll = state.Affixes[index];
            AffixDef? def = state.DefinitionAt(index);
            sb.Append('\n');
            if (dormant)
            {
                sb.Append(RarityPalette.Grey);
            }
            sb.Append(Text(roll, def));
            if (detail == TooltipDetail.Full)
            {
                AppendRange(sb, roll, def);
            }
            AppendMarkers(sb, state.IsBoundAt(index), roll.Tier, detail, dormant);
        }

        /// <summary>Tier (not at Compact), the bound marker in the game's orange, and the dormant word (closing the grey).</summary>
        private static void AppendMarkers(StringBuilder sb, bool bound, int tier, TooltipDetail detail, bool dormant)
        {
            if (detail != TooltipDetail.Compact)
            {
                sb.Append("  ").Append(Words.Localize("$ecf_ui_tier", tier.ToString()));
            }
            if (bound)
            {
                sb.Append("  ").Append(RarityPalette.Orange).Append(Words.Localize("$ecf_ui_bound")).Append(RarityPalette.Close);
            }
            if (dormant)
            {
                sb.Append("  ").Append(Words.Localize("$ecf_ui_dormant")).Append(RarityPalette.Close);
            }
        }

        /// <summary>
        /// The affix's own sentence (<c>$ecf_affix_&lt;id&gt;_line</c>) when a translation has one; otherwise the
        /// generic "value name" line. An orphaned affix (no definition) shows its bare stored value and its name or id.
        /// </summary>
        private static string Text(AffixRoll roll, AffixDef? def)
        {
            string lineKey = "ecf_affix_" + roll.Id + "_line";
            if (DisplayWords.Has(lineKey))
            {
                return Words.Localize("$" + lineKey, DisplayWords.Plain(roll.Value));
            }
            string name = def != null
                ? DisplayWords.Name(def.Name, roll.Id)
                : DisplayWords.Name("$ecf_affix_" + roll.Id, roll.Id);
            if (def != null && def.Value == AffixValueType.Flag)
            {
                return name;
            }
            string value = def != null ? DisplayWords.Signed(roll.Value, def) : DisplayWords.Plain(roll.Value);
            return Words.Localize("$ecf_ui_affix_line", value, name);
        }

        /// <summary>Full detail: the stored tier's roll range, when the definition still has that tier.</summary>
        private static void AppendRange(StringBuilder sb, AffixRoll roll, AffixDef? def)
        {
            AffixTierDef? row = def?.TierRow(roll.Tier);
            if (row == null || def!.Value == AffixValueType.Flag)
            {
                return;
            }
            sb.Append(' ').Append(Words.Localize("$ecf_ui_range", DisplayWords.Plain(row.Min), DisplayWords.Plain(row.Max)));
        }
    }
}
