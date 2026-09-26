using System.Collections.Generic;
using EliteCreaturesReborn.Display;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;

namespace EliteCreaturesReborn.Commands
{
    /// <summary>
    /// The boss-aspect lines of `elite inspect`: the aspect, what it does with the live numbers, what it pays, and the
    /// state a player cannot see - a Twin's partner, a Phantom copy's boss, the Summoner waves already called. Read from
    /// the ZDO on whichever machine the admin is looking from, so it is the replicated truth, not a local guess.
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
            if (traits.Aspect == Aspect.Twin)
            {
                lines.Add($"twin of {AspectStore.GetTwin(zdo)}; the two share one health pool");
            }
            if (traits.Aspect == Aspect.Summoner)
            {
                lines.Add($"waves called: {AspectStore.GetWaves(zdo)}");
            }
            return lines;
        }
    }
}
