using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Translations;
using UnityEngine;

namespace EliteCreaturesReborn.Recap.Window
{
    /// <summary>
    /// The recap's words: times, the killer line, the summary line, a death's list entry and a hit's row, built in the
    /// language shown each time they are drawn (keys <c>ecr_recap_*</c> in <c>translations/English.yml</c>). A
    /// recap keeps causes and damage types as ids (<see cref="HitDescribe"/>) and dealers as their parts
    /// (<see cref="AttackerName"/>), so nothing it keeps is in one language.
    /// </summary>
    internal static class RecapText
    {
        private const string TypePrefix = "ecr_recap_type_";

        /// <summary>Seconds into the clip as "0:07.4".</summary>
        public static string Clip(float seconds)
        {
            seconds = Mathf.Max(0f, seconds);
            int minutes = (int)(seconds / 60f);
            return minutes.ToString(CultureInfo.InvariantCulture) + ":"
                + (seconds - minutes * 60f).ToString("00.0", CultureInfo.InvariantCulture);
        }

        /// <summary>"Killed by Mad Greydwarf 2★ (ranged)", "Died: Fall".</summary>
        public static string Killer(DeathRecap recap)
        {
            string who = "<b><noparse>" + recap.Killer + "</noparse></b>";
            if (!recap.Dealt)
            {
                return Lang.Format("ecr_recap_died", who);
            }
            if (recap.Cause.Length == 0 || recap.Cause == "hit")
            {
                return Lang.Format("ecr_recap_killed_by", who);
            }
            return Lang.Format("ecr_recap_killed_by_how", who, "<color=#B0B0B0>(" + Word(recap.Cause) + ")</color>");
        }

        /// <summary>"Day 96, 14:32 · 312 damage in 8.4 s: slash 62%, frost 30%".</summary>
        public static string Summary(DeathRecap recap)
        {
            if (recap.Hits.Count == 0)
            {
                return When(recap) + " · " + Lang.Get("ecr_recap_no_hits");
            }
            Dictionary<string, float> byType = new Dictionary<string, float>();
            int total = Mathf.RoundToInt(Totals(recap.Hits, byType));
            float span = recap.Hits[recap.Hits.Count - 1].Time - recap.Hits[0].Time;
            string seconds = span.ToString("0.0", CultureInfo.InvariantCulture);
            string shares = Shares(byType);
            string damage = shares.Length > 0
                ? Lang.Format("ecr_recap_damage_shares", total, seconds, shares)
                : Lang.Format("ecr_recap_damage", total, seconds);
            return When(recap) + " · " + damage;
        }

        /// <summary>"Day 96, 14:32".</summary>
        public static string When(DeathRecap recap) =>
            Lang.Format("ecr_recap_when", recap.Day, recap.Clock.ToString("HH:mm", CultureInfo.InvariantCulture));

        /// <summary>A death in the list: the killer, then the day, clock and clip length on a smaller line.</summary>
        public static string Entry(DeathRecap recap) =>
            "<b><noparse>" + recap.Killer + "</noparse></b>\n<size=13><color=#B8B8B8>" + When(recap) + " · "
            + Clip(recap.Duration) + "</color></size>";

        /// <summary>"0:07.4   Mad Greydwarf 2★   42   slash 34 · frost 8   HP 51/158".</summary>
        public static string Hit(HitRecord hit, float at)
        {
            StringBuilder text = new StringBuilder();
            text.Append("<pos=0%>").Append(Clip(at));
            string attacker = AttackerName.TextOf(hit.Attacker);
            string who = attacker.Length > 0 ? "<noparse>" + attacker + "</noparse>" : Capital(Word(hit.Source));
            text.Append("<pos=9%>").Append(who);
            text.Append("<pos=46%><b>").Append(Mathf.RoundToInt(hit.Damage)).Append("</b>");
            text.Append("<pos=53%>").Append(Parts(hit.Parts));
            string health = Lang.Format("ecr_recap_hp", Mathf.CeilToInt(hit.Health), Mathf.RoundToInt(hit.MaxHealth));
            text.Append("<pos=84%><color=#B8B8B8>").Append(health).Append("</color>");
            return text.ToString();
        }

        /// <summary>A cause's or damage type's word in the language shown ("edge of the world"); an id the words lack (a
        /// newer game's cause) as it is.</summary>
        public static string Word(string id)
        {
            string key = TypePrefix + id;
            return Lang.Has(key) ? Lang.Get(key) : id;
        }

        public static string Capital(string text) => text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text.Substring(1);

        private static string Parts(List<KeyValuePair<string, float>> parts)
        {
            StringBuilder text = new StringBuilder();
            foreach (KeyValuePair<string, float> part in parts)
            {
                if (text.Length > 0)
                {
                    text.Append(" · ");
                }
                string words = Lang.Format("ecr_recap_part", Word(part.Key), Mathf.Max(1, Mathf.RoundToInt(part.Value)));
                text.Append("<color=").Append(Colour(part.Key)).Append('>').Append(words).Append("</color>");
            }
            return text.ToString();
        }

        // Every hit's damage added up, and by type into byType.
        private static float Totals(List<HitRecord> hits, Dictionary<string, float> byType)
        {
            float total = 0f;
            foreach (HitRecord hit in hits)
            {
                total += hit.Damage;
                foreach (KeyValuePair<string, float> part in hit.Parts)
                {
                    byType.TryGetValue(part.Key, out float sum);
                    byType[part.Key] = sum + part.Value;
                }
            }
            return total;
        }

        // The four biggest damage types as shares of the whole, coloured like the hit rows; empty when there are none.
        private static string Shares(Dictionary<string, float> byType)
        {
            List<KeyValuePair<string, float>> sorted = new List<KeyValuePair<string, float>>(byType);
            sorted.Sort((a, b) => b.Value.CompareTo(a.Value));
            float sum = 0f;
            sorted.ForEach(p => sum += p.Value);
            StringBuilder text = new StringBuilder();
            for (int i = 0; i < sorted.Count && i < 4 && sum > 0f; i++)
            {
                string share = Lang.Format("ecr_recap_share", Word(sorted[i].Key), Mathf.RoundToInt(100f * sorted[i].Value / sum));
                text.Append(i == 0 ? "" : ", ").Append("<color=").Append(Colour(sorted[i].Key)).Append('>').Append(share)
                    .Append("</color>");
            }
            return text.ToString();
        }

        private static string Colour(string type) => type switch
        {
            "fire" or "burning" or "lava" => "#FF9A4D",
            "frost" or "freezing" => "#9FDBFF",
            "lightning" => "#FFE36B",
            "poison" => "#9AD86A",
            "spirit" => "#E4DCFF",
            _ => "#E6E6E6",
        };
    }
}
