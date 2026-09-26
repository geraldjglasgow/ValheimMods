using System.Collections.Generic;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Runtime;

namespace EliteCreaturesReborn.Commands
{
    /// <summary>
    /// <c>elite tier</c>: prints the world tier, what it does to the rolls right now, and every listed boss with whether
    /// the world has its defeat key - then any boss the game knows that the list leaves out, with the key to add. The
    /// only read-only sub-command open to everyone: it reads the global keys this machine already holds, changes
    /// nothing, and a player on a locked server is exactly who needs to know why the world got harder.
    /// </summary>
    public static class TierCommand
    {
        public static void Run(Terminal.ConsoleEventArgs args)
        {
            TierRules rules = RuleState.Active.Tiers;
            if (!rules.Enabled)
            {
                EliteCommands.Reply(args, "elite tier: world tiers are off; the world stays at tier 0.");
                return;
            }
            int tier = WorldTier.Current();
            EliteCommands.Reply(args, $"elite tier: world tier {tier} of {WorldTier.Ceiling()} - star boost x{rules.StarBoostAt(tier):0.##}, mutation boost x{rules.MutationBoostAt(tier):0.##}");
            Dictionary<string, string> known = WorldTier.KnownBosses();
            foreach (string key in rules.BossKeys)
            {
                string name = known.TryGetValue(key, out string found) ? found : "no known boss sets this";
                EliteCommands.Reply(args, $"  [{(WorldTier.IsDefeated(key) ? "x" : " ")}] {name} ({key})");
            }
            ReportUnlisted(args, rules, known);
        }

        private static void ReportUnlisted(Terminal.ConsoleEventArgs args, TierRules rules, Dictionary<string, string> known)
        {
            foreach (KeyValuePair<string, string> boss in known)
            {
                if (!rules.BossKeys.Contains(boss.Key))
                {
                    EliteCommands.Reply(args, $"  not counted: {boss.Value} ({boss.Key})");
                }
            }
        }
    }
}
