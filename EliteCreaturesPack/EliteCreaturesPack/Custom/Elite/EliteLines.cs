using System.Collections.Generic;
using EliteCreaturesPack.Custom.Build;
using EliteCreaturesPack.Custom.Definitions;

namespace EliteCreaturesPack.Custom.Elite
{
    /// <summary>
    /// A creature's four elite lines (features/custom-creatures.md section 8) merged over its chain: each pass hands its
    /// definition's lists in, base-most first, and a list a later definition sets replaces the earlier one, the way every
    /// other value of a chain does. A list left out, or empty, keeps what the earlier definitions gave. Null: no definition
    /// of the chain asks for it (or, once checked, nothing of it is left to register).
    /// </summary>
    internal sealed class EliteLines
    {
        /// <summary>`elite: mutations`: ECR mutation names, in place of its random mutation roll.</summary>
        public EliteLine<string>? Mutations;

        /// <summary>`elite: aspect`: one ECR aspect, or a list it rolls from; only for a creature marked as a boss.</summary>
        public EliteLine<string>? Aspects;

        /// <summary>`elite: portal attacks`: the attack items Portalbound sends through its portals.</summary>
        public EliteLine<string>? PortalAttacks;

        /// <summary>`elite: summon`: what Summoner calls, with stars.</summary>
        public EliteLine<SummonEntry>? Summons;

        /// <summary>Whether any line is set.</summary>
        public bool Any => Mutations != null || Aspects != null || PortalAttacks != null || Summons != null;

        /// <summary>Takes the lists this pass's definition sets.</summary>
        public void Take(CreatureBuild build)
        {
            EliteBlock? elite = build.Definition.Elite;
            if (elite == null)
            {
                return;
            }
            Mutations = Pick(elite.Mutations, "elite.mutations", build.Report, Mutations);
            Aspects = Pick(elite.Aspects, "elite.aspect", build.Report, Aspects);
            PortalAttacks = Pick(elite.PortalAttacks, "elite.portal attacks", build.Report, PortalAttacks);
            Summons = Pick(elite.Summon, "elite.summon", build.Report, Summons);
        }

        private static EliteLine<T>? Pick<T>(List<T> values, string field, BuildReport report, EliteLine<T>? earlier) =>
            values.Count > 0 ? new EliteLine<T>(values, field, report) : earlier;
    }
}
