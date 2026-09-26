using System.Collections.Generic;
using EliteCreaturesReborn.Rules;
using UnityEngine;

namespace EliteCreaturesReborn.Traits
{
    /// <summary>
    /// Draws one aspect for a boss from the live rules: every outcome in that boss's rotation, weighted by its chance,
    /// `none` included. An altar's shift passes the aspect it shows as <c>exclude</c>, so a shift is always a visible
    /// change - unless nothing else is left to draw, when the current one simply stays.
    /// </summary>
    public static class AspectRoller
    {
        public static Aspect Roll(string bossPrefab, Aspect? exclude)
        {
            AspectRules rules = RuleState.Active.Boss.Aspects;
            if (!rules.Enabled)
            {
                return Aspect.None;
            }
            List<KeyValuePair<Aspect, float>> pool = Pool(rules, bossPrefab, exclude);
            return pool.Count == 0 ? exclude ?? Aspect.None : Pick(pool);
        }

        private static List<KeyValuePair<Aspect, float>> Pool(AspectRules rules, string bossPrefab, Aspect? exclude)
        {
            List<KeyValuePair<Aspect, float>> pool = new List<KeyValuePair<Aspect, float>>();
            foreach (Aspect aspect in AspectCatalog.Outcomes)
            {
                float weight = rules.ChanceOf(aspect);
                if (aspect != exclude && weight > 0f && rules.InRotation(bossPrefab, aspect))
                {
                    pool.Add(new KeyValuePair<Aspect, float>(aspect, weight));
                }
            }
            return pool;
        }

        private static Aspect Pick(List<KeyValuePair<Aspect, float>> pool)
        {
            float total = 0f;
            foreach (KeyValuePair<Aspect, float> entry in pool)
            {
                total += entry.Value;
            }
            float draw = Random.Range(0f, total);
            foreach (KeyValuePair<Aspect, float> entry in pool)
            {
                draw -= entry.Value;
                if (draw < 0f)
                {
                    return entry.Key;
                }
            }
            return pool[pool.Count - 1].Key;
        }
    }
}
