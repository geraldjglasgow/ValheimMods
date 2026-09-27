using System.Collections.Generic;
using EliteCreaturesReborn.Display;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;

namespace EliteCreaturesReborn.Commands
{
    /// <summary>
    /// The boss-aspect lines of `elite inspect`: the aspect, what it does with the live numbers, what it pays, and the
    /// state a player cannot see - a Twin's partner, a Phantom copy's boss, the Summoner waves already called, the
    /// Phantom splits already made, the type an Adaptive boss resists, a Fixated boss's mark. Read from the ZDO on
    /// whichever machine the admin is looking from, so it is the replicated truth, not a local guess.
    /// </summary>
    internal static class AspectReport
    {
        public static List<string> Lines(EliteController controller)
        {
            CreatureTraits traits = controller.Traits;
            AspectRules rules = RuleState.Active.Boss.Aspects;
            ZDO zdo = controller.View.GetZDO();
            List<string> lines = new List<string>();
            if (traits.PhantomCopy)
            {
                lines.Add($"phantom copy of {AspectStore.GetPhantomOf(zdo)}: no drops, no body, vanishes with its boss");
                return lines;
            }
            lines.Add($"aspect {AspectCatalog.Key(traits.Aspect)} (loot x{rules.LootOf(traits.Aspect):0.##}): "
                + AspectText.Describe(traits.Aspect, rules));
            string? state = StateLine(traits.Aspect, zdo);
            if (state != null)
            {
                lines.Add(state);
            }
            return lines;
        }

        /// <summary>The replicated state an aspect keeps that a player cannot see; null for an aspect that keeps none.</summary>
        private static string? StateLine(Aspect aspect, ZDO zdo) => aspect switch
        {
            Aspect.Twin => $"twin of {AspectStore.GetTwin(zdo)}; the two share one health pool",
            Aspect.Summoner => $"waves called: {AspectStore.GetWaves(zdo)}",
            Aspect.Phantom => $"splits made: {AspectStore.GetSplits(zdo)}",
            Aspect.Adaptive => $"resisting now: {Aspects.AdaptiveTypes.Describe(Aspects.AdaptiveTypes.Read(zdo))}",
            Aspect.Fixated => Aspects.FixatedMark.Report(zdo),
            _ => null,
        };
    }
}
