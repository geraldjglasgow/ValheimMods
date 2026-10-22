using System;
using System.Collections.Generic;
using EarthWright.Core;
using Translations;

namespace EarthWright.Clearing
{
    /// <summary>
    /// Runs a planned clearing and tells the player how it went. With drops, the follow-up passes are scheduled before
    /// the first blow, since they must know which logs were lying there already.
    /// </summary>
    public static class ClearJob
    {
        public static int Execute(ClearPlan plan, Player player)
        {
            if (plan.Drops)
                SurvivalFollowUp.Schedule(plan);
            return ClearExecutor.Run(plan, player);
        }

        /// <summary>The first line goes to <paramref name="main"/> (a centre message or the console), the rest to <paramref name="extra"/>.</summary>
        public static void Report(ClearPlan plan, int cleared, Action<string> main, Action<string> extra)
        {
            List<string> lines = Lines(plan, cleared);
            main(lines[0]);
            for (int i = 1; i < lines.Count; i++)
                extra(lines[i]);
        }

        private static List<string> Lines(ClearPlan plan, int cleared)
        {
            List<string> lines = new List<string>();
            if (plan.Refusal != null)
            {
                lines.Add(TokenText.Localize(plan.Refusal));
                return lines;
            }
            if (cleared > 0)
                lines.Add(Lang.Format(ClearingWords.Done, cleared));
            if (plan.Warded > 0)
                lines.Add(Lang.Format(ClearingWords.Warded, plan.Warded));
            if (plan.NeedAxe > 0)
                lines.Add(Lang.Format(ClearingWords.NeedAxe, plan.NeedAxe));
            if (plan.NeedPickaxe > 0)
                lines.Add(Lang.Format(ClearingWords.NeedPickaxe, plan.NeedPickaxe));
            if (lines.Count == 0)
                lines.Add(Lang.Get(ClearingWords.Nothing));
            return lines;
        }
    }
}
