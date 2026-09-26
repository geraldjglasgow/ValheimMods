using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Traits;
using UnityEngine;

namespace EliteCreaturesReborn.Display
{
    /// <summary>
    /// What an aspect does, in one line of plain text with the live numbers from the rules - for the altar before you
    /// summon, and for `elite inspect`. The numbers are read when the text is drawn, so the line always matches the fight
    /// the current rules would give.
    /// </summary>
    public static class AspectText
    {
        public static string Describe(Aspect aspect, AspectRules rules) => aspect switch
        {
            Aspect.None => "the fight as the game ships it",
            Aspect.Reflective => $"{N(rules, aspect, Fields.Reflect)}% of each hit you land on it comes back to you",
            Aspect.Shielded => $"takes {N(rules, aspect, Fields.ArrowReduction)}% less damage from arrows and bolts",
            Aspect.Mending => $"regenerates {N(rules, aspect, Fields.Regen)}% of its health every second",
            Aspect.Summoner => $"calls {N(rules, aspect, Fields.Count)} creatures each time it loses "
                + $"{N(rules, aspect, Fields.Every)}% of its health",
            Aspect.Elementalist => $"deals {N(rules, aspect, Fields.ElementalBonus)}% more elemental damage",
            Aspect.Enraged => $"deals {N(rules, aspect, Fields.PhysicalBonus)}% more physical damage",
            Aspect.Twin => $"comes as two sharing one health pool, each with {N(rules, aspect, Fields.LessHealth)}% "
                + $"less health and {N(rules, aspect, Fields.LessDamage)}% less damage",
            Aspect.Phantom => $"brings {N(rules, aspect, Fields.Copies)} phantom copies: "
                + $"{N(rules, aspect, Fields.Health)} health, {N(rules, aspect, Fields.LessDamage)}% less damage",
            _ => "",
        };

        /// <summary>The lines under the bowl's own hover text: the aspect, what it pays, and when it shifts.</summary>
        public static string AltarLines(Aspect aspect, AspectRules rules, double secondsToShift)
        {
            string name = aspect == Aspect.None ? "none" : AspectCatalog.Word(aspect);
            string pays = Mathf.Approximately(rules.LootOf(aspect), 1f) ? "" : $" <color=#A0A0A0>(loot x{rules.LootOf(aspect):0.##})</color>";
            return $"\n<color=orange>Aspect: {name}</color>{pays}\n{Describe(aspect, rules)}{Shift(secondsToShift)}";
        }

        private static string Shift(double seconds)
        {
            if (seconds < 0)
            {
                return ""; // shifting is off: the aspect is fixed
            }
            int whole = Mathf.Max(0, Mathf.CeilToInt((float)seconds));
            return whole == 0 ? "\n<color=#A0A0A0>Shifting...</color>" : $"\n<color=#A0A0A0>Shifts in {whole / 60}:{whole % 60:00}</color>";
        }

        private static string N(AspectRules rules, Aspect aspect, string field) => rules.PowerOf(aspect, field).ToString("0.##");
    }
}
