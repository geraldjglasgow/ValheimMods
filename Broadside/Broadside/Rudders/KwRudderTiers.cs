using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The seven rudders, one a biome, in the order a player reaches the biomes; and a rudder prefab's tier by name. All seven
    /// are registered, so a rudder built or saved in any of them loads, draws, steers and costs; only the Meadows and Black
    /// Forest rudders are in the hammer for now (<see cref="Offered"/>).
    /// </summary>
    public static class KwRudderTiers
    {
        public static IReadOnlyList<KwRudderTier> All { get; } = new[]
        {
            KwRudderTierMeadows.Tier, KwRudderTierBlackForest.Tier, KwRudderTierSwamp.Tier, KwRudderTierMountains.Tier,
            KwRudderTierPlains.Tier, KwRudderTierMistlands.Tier, KwRudderTierAshlands.Tier,
        };

        /// <summary>
        /// The rudders the hammer offers: Meadows and Black Forest (the user's words, 2026-10-26: "We should not have any of
        /// the higher tier stuff yet, only meadows and Black Forest compatible items"); the others stay registered, out of it.
        /// </summary>
        public static IReadOnlyList<KwRudderTier> Offered { get; } = new[] { KwRudderTierMeadows.Tier, KwRudderTierBlackForest.Tier };

        /// <summary>Whether a prefab is a rudder kept out of the hammer: registered for those built or saved, not offered.</summary>
        public static bool Held(string prefab)
        {
            KwRudderTier? tier = Of(prefab);
            foreach (KwRudderTier offered in Offered)
            {
                if (offered == tier)
                    return false;
            }
            return tier != null;
        }

        /// <summary>A rudder prefab's tier by its name (a placed rudder's through <c>Utils.GetPrefabName</c>); null for any other piece.</summary>
        public static KwRudderTier? Of(string prefab)
        {
            foreach (KwRudderTier tier in All)
            {
                if (tier.Prefab == prefab)
                    return tier;
            }
            return null;
        }

        /// <summary>
        /// A rudder's tier from the object itself: a placed rudder or its ghost by its prefab name; a copy of a rudder's parts
        /// under a name of its own (a launched ship's, <see cref="KwShipFittings"/>) by its model's mesh, which is named after the prefab.
        /// </summary>
        public static KwRudderTier? Of(GameObject piece)
        {
            KwRudderTier? named = Of(Utils.GetPrefabName(piece));
            if (named != null)
                return named;
            Transform? model = piece.transform.Find("model");
            MeshFilter? filter = model != null ? model.GetComponent<MeshFilter>() : null;
            string mesh = filter != null && filter.sharedMesh != null ? filter.sharedMesh.name : "";
            foreach (KwRudderTier tier in All)
            {
                if (mesh.StartsWith(tier.Prefab + "_", System.StringComparison.Ordinal))
                    return tier;
            }
            return null;
        }
    }
}
