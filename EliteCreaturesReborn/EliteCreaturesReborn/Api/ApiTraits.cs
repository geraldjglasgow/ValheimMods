using System;
using System.Collections.Generic;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Traits;

namespace EliteCreaturesReborn.Api
{
    /// <summary>
    /// The trait endpoints of <see cref="EliteCreaturesApi"/> (<c>features/api.md</c>): the mutation and aspect names, and
    /// a prefab's fixed mutations and aspects, checked and handed to <see cref="Registrations"/>. A name ECR does not know
    /// is left out and reported. A known mutation ECR's limits refuse - its kind's bars, the rule file's `mutations
    /// enabled` as loaded now - is reported but kept, because the creature's roll checks again with the rules of that
    /// moment (a server's synced file may differ) and with its body, which is measured only then, once the prefab is
    /// final. Each call replaces what the prefab had; an empty list clears it.
    /// </summary>
    internal static class ApiTraits
    {
        /// <summary>The mutations by the names the rule file uses, in catalog order.</summary>
        public static string[] MutationNames() => Array.ConvertAll(MutationCatalog.InOrder, MutationCatalog.Word);

        /// <summary>The aspects by the names the rule file uses, in catalog order, without `none`.</summary>
        public static string[] AspectNames() => Array.ConvertAll(AspectCatalog.InOrder, AspectCatalog.Key);

        public static string[] SetMutations(string prefab, string[]? names)
        {
            ApiProblems problems = new ApiProblems(nameof(EliteCreaturesApi.SetMutations), prefab);
            int mask = 0;
            foreach (string? name in names ?? Array.Empty<string>())
            {
                Mutation? mutation = MutationCatalog.FromName(name ?? "");
                if (mutation == null)
                {
                    problems.Add($"'{name}' is not a mutation, left out");
                    continue;
                }
                mask |= 1 << (int)mutation.Value;
            }
            Registrations.SetMutations(prefab, mask);
            ReportRefusals(prefab, mask, problems);
            return problems.ToArray();
        }

        public static string[] SetAspects(string prefab, string[]? names)
        {
            ApiProblems problems = new ApiProblems(nameof(EliteCreaturesApi.SetAspects), prefab);
            List<Aspect> aspects = new List<Aspect>();
            foreach (string? name in names ?? Array.Empty<string>())
            {
                Aspect? aspect = AspectCatalog.FromName(name ?? "");
                if (aspect == null)
                {
                    problems.Add($"'{name}' is not an aspect, left out");
                }
                else if (!aspects.Contains(aspect.Value))
                {
                    aspects.Add(aspect.Value);
                }
            }
            Registrations.SetAspects(prefab, aspects.ToArray());
            return problems.ToArray();
        }

        public static void Clear(string prefab) => Registrations.Clear(prefab);

        // What can be told now: the kind's bars and the switches of the rule file loaded now. Kept either way.
        private static void ReportRefusals(string prefab, int mask, ApiProblems problems)
        {
            int kindBars = MutationBars.OfKind(prefab);
            foreach (Mutation mutation in MutationCatalog.InOrder)
            {
                string? why = (mask & (1 << (int)mutation)) != 0
                    ? RegisteredMutations.Refusal(mutation, RuleState.Active, 0, kindBars) : null;
                if (why != null)
                {
                    problems.Add($"'{MutationCatalog.Word(mutation)}' is refused for now - {why}");
                }
            }
        }
    }
}
