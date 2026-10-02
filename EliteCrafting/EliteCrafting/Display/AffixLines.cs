using System.Text;
using EliteCrafting.Affixes;
using EliteCrafting.Config;
using EliteCrafting.Core;
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
            sb.Append(Text(Catalysed(state, roll, def), def));
            if (detail == TooltipDetail.Full)
            {
                AppendRange(sb, roll, def);
            }
            AppendMarkers(sb, state.IsBoundAt(index), roll.Tier, detail, dormant);
        }

        /// <summary>
        /// The sockets (sockets.md section 7): each gem as "Frost Gem: its affix line  T5", oldest first, greyed while its
        /// affix is unknown or disabled; then one "Empty socket" line per free socket; then the catalyst line.
        /// </summary>
        public static void AppendSockets(StringBuilder sb, ItemState state, TooltipDetail detail)
        {
            for (int i = 0; i < state.Gems.Count; i++)
            {
                AppendGem(sb, state, i, detail);
            }
            for (int i = 0; i < state.EmptySockets; i++)
            {
                sb.Append('\n').Append(RarityPalette.Grey).Append(Words.Localize("$ecf_ui_socket_empty")).Append(RarityPalette.Close);
            }
            if (state.CatalystFamily != null)
            {
                string family = DisplayWords.Name("$ecf_family_" + state.CatalystFamily, state.CatalystFamily);
                sb.Append('\n').Append(Words.Localize("$ecf_ui_catalyst", family, DisplayWords.Plain(state.CatalystQuality)));
            }
        }

        private static void AppendGem(StringBuilder sb, ItemState state, int index, TooltipDetail detail)
        {
            SocketGem gem = state.Gems[index];
            AffixDef? def = state.GemDefinitionAt(index);
            bool dormant = def == null || !def.Enabled;
            string gemName = DisplayWords.Name(ActiveRules.Current.Stone(gem.GemId)?.Name ?? "$ecf_stone_" + gem.GemId, gem.GemId);
            string text = Words.Localize("$ecf_ui_socket_gem", gemName, Text(Catalysed(state, gem.Roll, def), def));
            sb.Append('\n').Append(dormant ? RarityPalette.Grey : "").Append(text);
            AppendMarkers(sb, false, gem.Roll.Tier, detail, dormant);
        }

        // The value the effects use: a catalyst of the affix's family multiplies it (flags stay 1).
        private static AffixRoll Catalysed(ItemState state, AffixRoll roll, AffixDef? def)
        {
            if (def == null || def.Value == AffixValueType.Flag)
            {
                return roll;
            }
            float factor = state.CatalystFactor(roll.Id);
            return factor == 1f ? roll : roll.WithValue((float)System.Math.Round(roll.Value * factor, 2));
        }

        /// <summary>Tier (not at Compact), the bound marker in the game's orange, and the dormant word (closing the grey).</summary>
        private static void AppendMarkers(StringBuilder sb, bool bound, int tier, TooltipDetail detail, bool dormant)
        {
            if (detail != TooltipDetail.Compact)
            {
                sb.Append("  ").Append(Words.Localize("$ecf_ui_tier", AffixTierNumbers.Shown(tier).ToString()));
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
