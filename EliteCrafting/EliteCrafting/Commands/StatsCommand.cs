using EliteCrafting.Core;
using EliteCrafting.Effects;
using EliteCrafting.Rules;

namespace EliteCrafting.Commands
{
    /// <summary>
    /// <c>ecraft stats</c>: the local player's own aggregated totals (console-commands.md section 3,
    /// effects-runtime.md section 5), read from the effects runtime's last rebuild (<see cref="EffectTotals.Snapshot"/>),
    /// so it shows what the game is actually given. Every non-zero channel with its raw sum, cap, applied value and the
    /// equipment positions feeding it; player-global channels first, then item-local ones (which apply per item); then the
    /// health-critical state and the Phase 2 runtime states in force (<see cref="EffectSnapshot.States"/>).
    /// Local player only; a dedicated server has none.
    /// </summary>
    internal static class StatsCommand
    {
        public static void Run(CommandCall call)
        {
            if (Player.m_localPlayer == null)
            {
                call.Reply("no local player here (main menu or dedicated server).");
                return;
            }
            EffectSnapshot snapshot = EffectTotals.Snapshot();
            string rebuilt = snapshot.SecondsSinceRebuild < 0f ? "not rebuilt yet" : $"rebuilt {Numbers.Format(snapshot.SecondsSinceRebuild)} s ago";
            call.Reply($"{snapshot.MagicItemsEquipped} magic items equipped, {snapshot.ActiveAffixes} active affixes, {rebuilt}");
            if (!snapshot.Enabled)
            {
                call.Detail("'Affix effects' is off: items keep their affixes, nothing applies.");
                return;
            }
            WriteChannels(call, snapshot, itemLocal: false);
            WriteChannels(call, snapshot, itemLocal: true);
            string critical = snapshot.HealthCritical ? "yes" : "no";
            call.Detail($"health critical: {critical} (threshold {Numbers.Format(snapshot.ThresholdPercent)}%)");
            if (snapshot.States.Count > 0)
            {
                call.Detail("active now: " + string.Join(", ", snapshot.States));
            }
        }

        private static void WriteChannels(CommandCall call, EffectSnapshot snapshot, bool itemLocal)
        {
            foreach (EffectChannelTotal total in snapshot.Channels)
            {
                if (total.ItemLocal == itemLocal)
                {
                    call.Detail(Line(total));
                }
            }
        }

        private static string Line(EffectChannelTotal total)
        {
            string cap = float.IsPositiveInfinity(total.Cap) ? "-" : Numbers.Format(total.Cap);
            string scope = total.ItemLocal ? " item-local" : "";
            string inactive = total.Active ? "" : " (inactive: not health critical)";
            string condition = total.Channel.Condition == AffixCondition.HealthCritical ? " when health critical" : "";
            return $"{total.Key,-26} sum {Numbers.Format(total.Sum),-6} cap {cap,-6} applied {Numbers.Format(total.Applied),-6} "
                + $"({total.Sources}){scope}{condition}{inactive}";
        }
    }
}
