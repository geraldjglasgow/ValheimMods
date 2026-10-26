using System.Text;
using EliteCreaturesReborn.Loot;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Scaling;
using EliteCreaturesReborn.Traits;
using Translations;
using UnityEngine;

namespace EliteCreaturesReborn.Display
{
    /// <summary>
    /// What an aspect does, in one line of plain text with the live numbers from the rules - for the altar before you
    /// summon, and for `elite inspect`. The numbers are read when the text is drawn, so the line always matches the fight
    /// the current rules would give. The words are in the player's language: an aspect's name is the key
    /// <c>ecr_aspect_&lt;aspect&gt;</c> and what it does <c>ecr_aspect_does_&lt;aspect&gt;</c>, its numbers as $1, $2 ...
    /// </summary>
    public static class AspectText
    {
        /// <summary>U+2605, the black star, written as an escape so the file stays plain ASCII.</summary>
        private const char StarGlyph = '\u2605';

        public static string Describe(Aspect aspect, AspectRules rules) => aspect switch
        {
            Aspect.None => Does(aspect),
            Aspect.Reflective => Does(aspect, N(rules, aspect, Fields.Reflect)),
            Aspect.Shielded => Does(aspect, N(rules, aspect, Fields.ArrowReduction)),
            Aspect.Mending => Does(aspect, N(rules, aspect, Fields.Regen)),
            Aspect.Summoner => Does(aspect, N(rules, aspect, Fields.Count), N(rules, aspect, Fields.Every)),
            Aspect.Elementalist => Does(aspect, N(rules, aspect, Fields.ElementalBonus)),
            Aspect.Enraged => Does(aspect, N(rules, aspect, Fields.PhysicalBonus)),
            Aspect.Twin => Does(aspect, N(rules, aspect, Fields.LessHealth), N(rules, aspect, Fields.LessDamage)),
            Aspect.Phantom => Does(aspect, Marks(rules), N(rules, aspect, Fields.PerPlayer),
                AspectMath.PhantomHealth(rules).ToString("0"), N(rules, aspect, Fields.LessDamage)),
            _ => DescribeLater(aspect, rules),
        };

        /// <summary>The aspects added in 3.9.0, split out only to keep <see cref="Describe"/> short.</summary>
        private static string DescribeLater(Aspect aspect, AspectRules rules) => aspect switch
        {
            Aspect.Adaptive => Does(aspect, N(rules, aspect, Fields.Resist), N(rules, aspect, Fields.Window)),
            Aspect.Fixated => Does(aspect, N(rules, aspect, Fields.MarkedBonus), N(rules, aspect, Fields.OthersLess),
                N(rules, aspect, Fields.Every)),
            Aspect.Stormbound => Does(aspect, N(rules, aspect, Fields.Every), N(rules, aspect, Fields.TellTime)),
            Aspect.Gravitic => Does(aspect, N(rules, aspect, Fields.Every), N(rules, aspect, Fields.Range),
                N(rules, aspect, Fields.SlamDamage), N(rules, aspect, Fields.SlamRadius)),
            Aspect.Colossal => Does(aspect, N(rules, aspect, Fields.Bigger), N(rules, aspect, Fields.MoreHealth),
                N(rules, aspect, Fields.Slower), N(rules, aspect, Fields.ShockwaveRadius)),
            _ => DescribeNewest(aspect, rules),
        };

        /// <summary>The aspects added in 3.15.0 and after, split out only to keep <see cref="DescribeLater"/> short.</summary>
        private static string DescribeNewest(Aspect aspect, AspectRules rules) => aspect switch
        {
            Aspect.Tethered => Does(aspect, N(rules, aspect, Fields.LessHealth), N(rules, aspect, Fields.AttackSpeed),
                N(rules, aspect, Fields.Armour)),
            Aspect.Bountiful => Does(aspect, N(rules, aspect, Fields.ExtraAspects)),
            Aspect.Portalbound => Does(aspect),
            Aspect.Nightfall => Does(aspect, N(rules, aspect, Fields.Range), N(rules, aspect, Fields.Every),
                N(rules, aspect, Fields.EveryMax), N(rules, aspect, Fields.Life), N(rules, aspect, Fields.Damage)),
            Aspect.Brutal => Does(aspect, N(rules, aspect, Fields.Launch)),
            Aspect.Echoing => Does(aspect, N(rules, aspect, Fields.Delay)),
            _ => "",
        };

        /// <summary>What the aspect does, in lower case and with no full stop, its numbers filled in.</summary>
        private static string Does(Aspect aspect, params object?[] values) =>
            Lang.Format("ecr_aspect_does_" + aspect.ToString().ToLowerInvariant(), values);

        /// <summary>Phantom's split marks as "66% and 33%".</summary>
        private static string Marks(AspectRules rules)
        {
            float[] marks = rules.ListOf(Aspect.Phantom, Fields.SplitAt);
            if (marks.Length == 0)
            {
                return Lang.Get("ecr_aspect_marks_none");
            }
            string[] words = new string[marks.Length];
            for (int i = 0; i < marks.Length; i++)
            {
                words[i] = marks[i].ToString("0.##") + "%";
            }
            return words.Length == 1 ? words[0]
                : Lang.Format("ecr_book_list_and",
                    string.Join(Lang.Get("ecr_book_list_comma"), words, 0, words.Length - 1), words[words.Length - 1]);
        }

        /// <summary>The lines under the bowl's own hover text: the boss's stars, the aspect, a Bountiful one's extras each
        /// with what it does, what the whole fight pays, and when it shifts. A part that is off (null) is left out; a part
        /// a player chose at the altar says so, and so does each chosen extra (<paramref name="extrasChosen"/>, one bit
        /// per aspect).</summary>
        public static string AltarLines(int? stars, BossAspects? aspects, AspectRules rules, double secondsToShift,
            bool starsChosen = false, bool aspectChosen = false, int extrasChosen = 0)
        {
            string starLine = stars is int count ? StarLine(count) + (starsChosen ? ChosenMark() : "") : "";
            string aspectLines = aspects is BossAspects drawn ? AspectLines(drawn, rules, aspectChosen, extrasChosen) : "";
            return starLine + aspectLines + Shift(secondsToShift);
        }

        private static string ChosenMark() => " <color=#A0A0A0>" + Lang.Get("ecr_altar_chosen") + "</color>";

        /// <summary>"Stars: " and one star glyph per star, or "Stars: none". The game's fonts draw the glyph from their
        /// Noto JP fallback, as they draw every Japanese character.</summary>
        private static string StarLine(int stars) =>
            "\n<color=orange>" + Lang.Format("ecr_altar_stars_line", Stars(stars)) + "</color>";

        /// <summary>One star glyph per star, or "none".</summary>
        public static string Stars(int stars) => stars > 0 ? new string(StarGlyph, stars) : Lang.Get("ecr_altar_stars_none");

        /// <summary>The aspect's name (<c>ecr_aspect_&lt;aspect&gt;</c>); "none" for the fight as the game ships it.</summary>
        public static string Name(Aspect aspect) => Lang.Get("ecr_aspect_" + aspect.ToString().ToLowerInvariant());

        /// <summary>What the aspect does as a sentence of its own, for the compendium and the altar window: the altar's
        /// line starts in lower case after the aspect's name, so here it takes a capital and its full stop.</summary>
        public static string Sentence(Aspect aspect, AspectRules rules)
        {
            string line = Describe(aspect, rules);
            string capital = line.Length == 0 ? line : char.ToUpperInvariant(line[0]) + line.Substring(1);
            return Lang.Format("ecr_aspect_sentence", capital);
        }

        private static string AspectLines(BossAspects aspects, AspectRules rules, bool chosen, int extrasChosen)
        {
            Aspect aspect = aspects.Headline;
            float loot = AspectLoot.FactorOf(aspects, rules);
            string pays = Mathf.Approximately(loot, 1f) ? ""
                : " <color=#A0A0A0>" + Lang.Format("ecr_altar_loot_paren", loot.ToString("0.##")) + "</color>";
            return "\n<color=orange>" + Lang.Format("ecr_altar_aspect_line", Name(aspect)) + "</color>"
                + (chosen ? ChosenMark() : "") + pays + "\n" + Describe(aspect, rules) + ExtraLines(aspects, rules, extrasChosen);
        }

        /// <summary>One line per extra aspect a Bountiful altar will add: "+ Enraged: deals 20% more physical damage", each
        /// one in <paramref name="chosen"/> (one bit per aspect) marked "(chosen)". The altar window's too.</summary>
        public static string ExtraLines(BossAspects aspects, AspectRules rules, int chosen = 0)
        {
            StringBuilder lines = new StringBuilder();
            foreach (Aspect extra in aspects.ExtrasInOrder())
            {
                string mark = (chosen & (1 << (int)extra)) != 0 ? ChosenMark() : "";
                string name = "<color=orange>+ " + Name(extra) + "</color>" + mark;
                lines.Append('\n').Append(Lang.Format("ecr_altar_extra_line", name, Describe(extra, rules)));
            }
            return lines.ToString();
        }

        private static string Shift(double seconds)
        {
            if (seconds < 0)
            {
                return ""; // shifting is off: the altar is fixed
            }
            int whole = Mathf.Max(0, Mathf.CeilToInt((float)seconds));
            string words = whole == 0 ? Lang.Get("ecr_altar_shifting")
                : Lang.Format("ecr_altar_shifts_in", $"{whole / 60}:{whole % 60:00}");
            return "\n<color=#A0A0A0>" + words + "</color>";
        }

        private static string N(AspectRules rules, Aspect aspect, string field) => rules.PowerOf(aspect, field).ToString("0.##");
    }
}
