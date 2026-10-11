using System;

namespace EliteCreaturesReborn.Api
{
    /// <summary>
    /// Elite Creatures Reborn's public API for other mods (<c>features/api.md</c>): another mod's creatures given ECR's
    /// mutations and aspects. Bind to it by reflection, never by reference: the <c>ValheimModLibs/EliteCreaturesLink</c>
    /// library does that and does nothing when ECR is absent or older.
    /// <para>
    /// Rules every endpoint keeps: only BCL types cross; it never throws (an internal failure is logged once and answers
    /// false or an empty array); a registration answers the problems it found, one sentence each, never null.
    /// Registrations are code, not data: every peer's mod makes the same ones, nothing is synced, and a creature's owner
    /// reads them at its roll, which goes into the ZDO as every roll does - a creature already rolled keeps what it has.
    /// Registered mutations replace the random mutation roll of the prefab (its stars still roll), within ECR's limits;
    /// registered aspects are rolled for the prefab whether or not the game marks it a boss. Main thread only. An
    /// endpoint is added, never changed; a removed one stays as a no-op.
    /// </para>
    /// </summary>
    public static class EliteCreaturesApi
    {
        /// <summary>This API's version; a caller needing newer endpoints checks <see cref="GetApiVersion"/> or <see cref="HasEndpoint"/>.</summary>
        public const int ApiVersion = 1;

        // ---- 1. shape

        public static int GetApiVersion() => ApiVersion;

        public static string GetPluginVersion() => PluginInfo.PluginVersion;

        /// <summary>Whether a public endpoint of this name exists.</summary>
        public static bool HasEndpoint(string name) => ApiGuard.Run("HasEndpoint", ApiEndpoints.Has, name, false);

        /// <summary>Every endpoint name, sorted.</summary>
        public static string[] GetEndpointNames() => ApiGuard.Run("GetEndpointNames", ApiEndpoints.All, Array.Empty<string>());

        // ---- 2. names

        /// <summary>The mutations by the names the rule file uses ("Mad", "Bloated" ...), in catalog order.</summary>
        public static string[] GetMutationNames() => ApiGuard.Run("GetMutationNames", ApiTraits.MutationNames, Array.Empty<string>());

        /// <summary>The aspects by the names the rule file uses ("Reflective" ... "Echoing"), in catalog order, without `none`.</summary>
        public static string[] GetAspectNames() => ApiGuard.Run("GetAspectNames", ApiTraits.AspectNames, Array.Empty<string>());

        // ---- 3. a prefab's traits

        /// <summary>
        /// The prefab's fixed mutations, in place of its random mutation roll from every spawn source; several may go on
        /// one creature. A name ECR does not know is left out; one ECR's limits refuse (its body, its kind, `mutations
        /// enabled`) is skipped at the roll, logged once. Answers the problems; an empty array clears; a later call replaces.
        /// </summary>
        public static string[] SetMutations(string prefab, string[] mutations) =>
            Named(prefab, "SetMutations") ?? ApiGuard.Run("SetMutations", ApiTraits.SetMutations, prefab, mutations, Array.Empty<string>());

        /// <summary>
        /// The aspects the prefab rolls, boss or not: one name is always that one, several are drawn by the rule file's
        /// aspect weights (Bountiful's extras from the same list). Answers the problems; an empty array clears.
        /// </summary>
        public static string[] SetAspects(string prefab, string[] aspects) =>
            Named(prefab, "SetAspects") ?? ApiGuard.Run("SetAspects", ApiTraits.SetAspects, prefab, aspects, Array.Empty<string>());

        /// <summary>
        /// The attack item prefab names (the creature's own projectile attacks, like the Elder's <c>gd_king_shoot</c>)
        /// Portalbound may send through its portals; without them the aspect never fires. Answers the problems; empty clears.
        /// </summary>
        public static string[] SetPortalAttacks(string prefab, string[] attacks) =>
            Named(prefab, "SetPortalAttacks") ?? ApiGuard.Run("SetPortalAttacks", ApiAbilities.SetPortalAttacks, prefab, attacks, Array.Empty<string>());

        /// <summary>
        /// What the prefab's Summoner calls, in place of the rule file's list: creature prefab names and the stars of each
        /// (the same length; a negative star means the aspect's own `stars`). Answers the problems; empty clears.
        /// </summary>
        public static string[] SetSummons(string prefab, string[] creatures, int[] stars) =>
            Named(prefab, "SetSummons") ?? ApiGuard.Run("SetSummons", ApiAbilities.SetSummons, prefab, creatures, stars, Array.Empty<string>());

        /// <summary>Forgets the prefab's mutations, aspects, portal attacks and summons.</summary>
        public static void Clear(string prefab)
        {
            if (!string.IsNullOrEmpty(prefab))
            {
                ApiGuard.Run("Clear", ApiTraits.Clear, prefab);
            }
        }

        // A call naming no prefab registers nothing and says so; null when there is a name.
        private static string[]? Named(string prefab, string endpoint) =>
            string.IsNullOrEmpty(prefab) ? ApiProblems.NoPrefab(endpoint) : null;
    }
}
