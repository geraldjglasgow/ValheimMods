using System.Collections.Generic;
using EliteCreaturesReborn.Aspects;
using EliteCreaturesReborn.Display;
using EliteCreaturesReborn.Loot;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;
using Translations;
using UnityEngine;

namespace EliteCreaturesReborn.Altars
{
    /// <summary>
    /// The altar window's words for the fight a player is choosing, never for what the altar happens to show: the chosen
    /// aspect's heading and what it does, and what the chosen fight pays. A part left on Random is described as rolling;
    /// the pay is then the least that fight can pay, marked "(at least)", and while the stars roll the boss's past defeats
    /// show, since each leans its stars up. With every part on Random the plain boss is named as one of the rolls.
    /// </summary>
    internal static class FightText
    {
        private const string Grey = "#A0A0A0";

        public static string AspectTitle(int aspect, bool any)
        {
            if (!any)
            {
                return Orange(Lang.Get("ecr_altar_aspect_off"));
            }
            string name = aspect == AltarPicks.Random ? Lang.Get("ecr_altar_random") : Capital(AspectText.Name((Aspect)aspect));
            return Orange(Lang.Format("ecr_altar_aspect_line", name));
        }

        public static string Description(int aspect, bool any)
        {
            if (!any)
            {
                return "";
            }
            if (aspect == AltarPicks.Random)
            {
                return Lang.Get("ecr_altar_random_text");
            }
            AspectRules rules = RuleState.Active.Boss.Aspects;
            return AspectText.Sentence((Aspect)aspect, rules)
                + (aspect == (int)Aspect.Bountiful ? " " + Lang.Get("ecr_altar_bountiful_text") : "")
                + Loot(AspectLoot.FactorOf(new BossAspects((Aspect)aspect, 0), rules));
        }

        /// <summary>The pay, then grey notes for what still rolls; one line each.</summary>
        public static string Fight(string boss, int stars, int aspect, List<Aspect> aspects)
        {
            bool starsOn = RuleState.Active.Boss.Enabled;
            bool starsRoll = starsOn && stars == AltarPicks.Random;
            bool aspectRolls = aspects.Count > 0 && aspect == AltarPicks.Random;
            List<string> lines = new List<string>();
            string reward = BossRewards.AltarLine(boss, starsOn && !starsRoll ? stars : 0, LeastLoot(aspect, aspects));
            if (reward.Length > 0)
            {
                lines.Add(reward.TrimStart('\n') + (starsRoll || aspectRolls ? " " + Note(Lang.Get("ecr_altar_at_least")) : ""));
            }
            if (starsRoll)
            {
                StarNotes(lines, boss);
            }
            if ((starsRoll || !starsOn) && (aspectRolls || aspects.Count == 0))
            {
                lines.Add(Note(Lang.Get("ecr_altar_plain_text")));
            }
            return string.Join("\n", lines);
        }

        private static void StarNotes(List<string> lines, string boss)
        {
            lines.Add(Note(Lang.Get("ecr_altar_stars_roll")));
            string defeated = AltarAspect.Defeated(BossKills.Count(boss));
            if (defeated.Length > 0)
            {
                lines.Add(Note(defeated));
            }
        }

        // The chosen aspect's loot multiplier, or with the aspect on Random the smallest any roll can bring.
        private static float LeastLoot(int aspect, List<Aspect> aspects)
        {
            AspectRules rules = RuleState.Active.Boss.Aspects;
            if (aspects.Count == 0)
            {
                return 1f;
            }
            if (aspect != AltarPicks.Random)
            {
                return AspectLoot.FactorOf(new BossAspects((Aspect)aspect, 0), rules);
            }
            float least = float.MaxValue;
            foreach (Aspect choice in aspects)
            {
                least = Mathf.Min(least, AspectLoot.FactorOf(new BossAspects(choice, 0), rules));
            }
            return least;
        }

        private static string Loot(float factor) => Mathf.Approximately(factor, 1f) ? ""
            : "\n" + Note(Lang.Format("ecr_altar_loot", factor.ToString("0.##")));

        private static string Orange(string text) => "<color=orange>" + text + "</color>";

        private static string Note(string text) => "<color=" + Grey + ">" + text + "</color>";

        public static string Capital(string text) => text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text.Substring(1);
    }
}
