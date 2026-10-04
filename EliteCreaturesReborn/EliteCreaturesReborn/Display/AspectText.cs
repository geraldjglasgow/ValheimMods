using System.Text;
using EliteCreaturesReborn.Loot;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Scaling;
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
            Aspect.Phantom => $"at {Marks(rules)} health splits off phantom copies, "
                + $"{N(rules, aspect, Fields.PerPlayer)} per player online, with {AspectMath.PhantomHealth(rules):0} "
                + $"health and {N(rules, aspect, Fields.LessDamage)}% less damage",
            _ => DescribeLater(aspect, rules),
        };

        /// <summary>The aspects added in 3.9.0, split out only to keep <see cref="Describe"/> short.</summary>
        private static string DescribeLater(Aspect aspect, AspectRules rules) => aspect switch
        {
            Aspect.Adaptive => $"takes {N(rules, aspect, Fields.Resist)}% less of whichever damage type hit it most in "
                + $"the last {N(rules, aspect, Fields.Window)} seconds",
            Aspect.Fixated => $"marks one player and hits them {N(rules, aspect, Fields.MarkedBonus)}% harder, everyone "
                + $"else {N(rules, aspect, Fields.OthersLess)}% softer; the mark moves every "
                + $"{N(rules, aspect, Fields.Every)} seconds to whoever hurt it most",
            Aspect.Stormbound => $"every {N(rules, aspect, Fields.Every)} seconds calls lightning down on a circle under "
                + $"each player, {N(rules, aspect, Fields.TellTime)} seconds after it appears",
            Aspect.Gravitic => $"every {N(rules, aspect, Fields.Every)} seconds pulls every player within "
                + $"{N(rules, aspect, Fields.Range)} m toward it, then slams: {N(rules, aspect, Fields.SlamDamage)}% of "
                + $"max health to players within {N(rules, aspect, Fields.SlamRadius)} m of it",
            Aspect.Colossal => $"{N(rules, aspect, Fields.Bigger)}% bigger, {N(rules, aspect, Fields.MoreHealth)}% more "
                + $"health, {N(rules, aspect, Fields.Slower)}% slower; its heavy attacks send a "
                + $"{N(rules, aspect, Fields.ShockwaveRadius)} m shockwave that knocks players down (roll through it or "
                + "jump it)",
            Aspect.Tethered => $"comes as two bound by a tether, each with {N(rules, aspect, Fields.LessHealth)}% less health; "
                + $"the further apart their health, the faster both attack (up to {N(rules, aspect, Fields.AttackSpeed)}%) "
                + $"and the less damage the weaker one takes (up to {N(rules, aspect, Fields.Armour)}%)",
            Aspect.Bountiful => $"carries {N(rules, aspect, Fields.ExtraAspects)} more aspects at once",
            Aspect.Portalbound => "throws its vines through portals: one opens at its hand, the other somewhere above you",
            _ => "",
        };

        /// <summary>Phantom's split marks as "66% and 33%".</summary>
        private static string Marks(AspectRules rules)
        {
            float[] marks = rules.ListOf(Aspect.Phantom, Fields.SplitAt);
            if (marks.Length == 0)
            {
                return "no";
            }
            string[] words = new string[marks.Length];
            for (int i = 0; i < marks.Length; i++)
            {
                words[i] = marks[i].ToString("0.##") + "%";
            }
            return words.Length == 1 ? words[0]
                : string.Join(", ", words, 0, words.Length - 1) + " and " + words[words.Length - 1];
        }

        /// <summary>The lines under the bowl's own hover text: the aspect, a Bountiful one's extras each with what it does,
        /// what the whole fight pays, and when it shifts.</summary>
        public static string AltarLines(BossAspects aspects, AspectRules rules, double secondsToShift)
        {
            Aspect aspect = aspects.Headline;
            string name = aspect == Aspect.None ? "none" : AspectCatalog.Word(aspect);
            float loot = AspectLoot.FactorOf(aspects, rules);
            string pays = Mathf.Approximately(loot, 1f) ? "" : $" <color=#A0A0A0>(loot x{loot:0.##})</color>";
            return $"\n<color=orange>Aspect: {name}</color>{pays}\n{Describe(aspect, rules)}"
                + ExtraLines(aspects, rules) + Shift(secondsToShift);
        }

        /// <summary>One line per extra aspect a Bountiful altar will add: "+ Enraged: deals 20% more physical damage".</summary>
        private static string ExtraLines(BossAspects aspects, AspectRules rules)
        {
            StringBuilder lines = new StringBuilder();
            foreach (Aspect extra in aspects.ExtrasInOrder())
            {
                lines.Append($"\n<color=orange>+ {AspectCatalog.Word(extra)}</color>: {Describe(extra, rules)}");
            }
            return lines.ToString();
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
