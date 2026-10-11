using System;

namespace EliteCreaturesLink
{
    /// <summary>
    /// A creature prefab's Elite Creatures Reborn traits (api.md sections 2 and 3): the names ECR knows, and the fixed
    /// mutations, the aspects, the attacks Portalbound sends through its portals and the creatures Summoner calls, for one
    /// prefab. Each registration answers the problems ECR found, one sentence each naming the endpoint and the prefab;
    /// an empty list clears that part and a later call replaces an earlier one. Without Elite Creatures Reborn every call
    /// does nothing and answers an empty array.
    /// </summary>
    public static class EliteTraits
    {
        private static readonly Endpoint<Func<string[]?>> mutationNames = new Endpoint<Func<string[]?>>("GetMutationNames");
        private static readonly Endpoint<Func<string[]?>> aspectNames = new Endpoint<Func<string[]?>>("GetAspectNames");
        private static readonly Endpoint<Func<string, string[], string[]?>> setMutations =
            new Endpoint<Func<string, string[], string[]?>>("SetMutations");
        private static readonly Endpoint<Func<string, string[], string[]?>> setAspects =
            new Endpoint<Func<string, string[], string[]?>>("SetAspects");
        private static readonly Endpoint<Func<string, string[], string[]?>> setPortalAttacks =
            new Endpoint<Func<string, string[], string[]?>>("SetPortalAttacks");
        private static readonly Endpoint<Func<string, string[], int[], string[]?>> setSummons =
            new Endpoint<Func<string, string[], int[], string[]?>>("SetSummons");
        private static readonly Endpoint<Action<string>> clear = new Endpoint<Action<string>>("Clear");

        /// <summary>Every mutation by the name the rule file uses ("Mad", "Bloated" ...), in ECR's order; empty without ECR.</summary>
        public static string[] MutationNames => Safe.Call(mutationNames.Call, null) ?? Array.Empty<string>();

        /// <summary>Every aspect by the name the rule file uses ("Reflective" ...), in ECR's order, without `none`; empty without ECR.</summary>
        public static string[] AspectNames => Safe.Call(aspectNames.Call, null) ?? Array.Empty<string>();

        /// <summary>The prefab's fixed mutations, in place of its random mutation roll; ECR's own limits still refuse some.</summary>
        public static string[] SetMutations(string prefab, string[] names) =>
            Safe.Call(setMutations.Call, prefab, names, null) ?? Array.Empty<string>();

        /// <summary>The aspects the prefab rolls, boss or not: one is fixed, several are drawn by ECR's aspect weights.</summary>
        public static string[] SetAspects(string prefab, string[] names) =>
            Safe.Call(setAspects.Call, prefab, names, null) ?? Array.Empty<string>();

        /// <summary>The prefab's projectile attack items (like the Elder's <c>gd_king_shoot</c>) Portalbound sends through its portals.</summary>
        public static string[] SetPortalAttacks(string prefab, string[] attacks) =>
            Safe.Call(setPortalAttacks.Call, prefab, attacks, null) ?? Array.Empty<string>();

        /// <summary>What the prefab's Summoner calls: creature prefab names and stars for each (negative: the aspect's own).</summary>
        public static string[] SetSummons(string prefab, string[] creatures, int[] stars) =>
            Safe.Call(setSummons.Call, prefab, creatures, stars, null) ?? Array.Empty<string>();

        /// <summary>Forgets the prefab's mutations, aspects, portal attacks and summons.</summary>
        public static void Clear(string prefab) => Safe.Run(clear.Call, prefab);
    }
}
