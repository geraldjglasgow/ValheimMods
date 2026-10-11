using System;
using System.Collections.Generic;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Util;

namespace EliteCreaturesReborn.Traits
{
    /// <summary>
    /// ECR's own limits on the mutations another mod fixed for a creature (<see cref="Registrations"/>): its kind's bars
    /// (no Cloaked Deathsquito), its body's (no Gilded or Relentless on a large creature, <see cref="BodySize"/>) and the
    /// rule file's `mutations enabled`. A refused mutation is left out and the rest are kept; each refusal is logged once
    /// per prefab and mutation, with its reason, the way `elite spawn` words a skipped word. Checked at every roll, on the
    /// owner, because the rule file can change after the registration; the API asks the same question when the
    /// registration is made, to report it back (<see cref="Refusal"/>).
    /// </summary>
    public static class RegisteredMutations
    {
        private static readonly HashSet<string> Warned = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>The prefab's registered mutations this creature may carry, packed like a trait mask; 0 for none.</summary>
        public static int For(Character creature, string prefab)
        {
            int mask = Registrations.MutationsOf(prefab);
            return mask == 0 ? 0 : Allowed(mask, prefab, RuleState.Active, BodySize.Barred(creature), MutationBars.OfKind(prefab));
        }

        /// <summary>The mask with every refused mutation taken out, each refusal logged once.</summary>
        public static int Allowed(int mask, string prefab, RuleSet rules, int bodyBars, int kindBars)
        {
            foreach (Mutation mutation in MutationCatalog.InOrder)
            {
                string? why = (mask & Bit(mutation)) != 0 ? Refusal(mutation, rules, bodyBars, kindBars) : null;
                if (why != null)
                {
                    mask &= ~Bit(mutation);
                    WarnOnce(prefab, mutation, why!);
                }
            }
            return mask;
        }

        /// <summary>Why this mutation may not go on a creature with these bars under these rules; null when it may.</summary>
        public static string? Refusal(Mutation mutation, RuleSet rules, int bodyBars, int kindBars)
        {
            if ((kindBars & Bit(mutation)) != 0)
            {
                return "its kind never carries it";
            }
            if ((bodyBars & Bit(mutation)) != 0)
            {
                return "its body is large: no Gilded or Relentless on large creatures";
            }
            return rules.IsEnabled(mutation) ? null : "it is switched off in `mutations enabled`";
        }

        private static int Bit(Mutation mutation) => 1 << (int)mutation;

        private static void WarnOnce(string prefab, Mutation mutation, string why)
        {
            string word = MutationCatalog.Word(mutation);
            if (Warned.Add(prefab + "|" + word))
            {
                Log.Warn($"{prefab}: registered mutation '{word}' skipped - {why}");
            }
        }
    }
}
