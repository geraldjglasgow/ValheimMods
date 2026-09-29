using System.Linq;
using UnityEngine;

namespace EliteCreaturesPack.Crossbow
{
    /// <summary>
    /// One of the game's skeletons that can come as an archer, and the crossbowman that may come in its place: a copy of
    /// that skeleton (its health, resistances, looks, sounds and drops) whose crossbow hits as hard as that skeleton's
    /// bow, as blunt. The game's four: the Black Forest's Skeleton (40 health, a 20-damage bow), the Meadows' (30, 15),
    /// the Swamps' (60, 55) and the Mountains' (75, 60). The no-archer, poison, Hildir and summoned skeletons never
    /// carry a bow and are left alone.
    /// </summary>
    public sealed class XbowKind
    {
        public static readonly XbowKind[] All =
        {
            new XbowKind("Skeleton", "skeleton_bow", "ECP_SkeletonCrossbowman"),
            new XbowKind("Skeleton_Meadows", "skeleton_bow_meadows", "ECP_SkeletonCrossbowman_Meadows"),
            new XbowKind("Skeleton_Swamps", "skeleton_bow_swamps", "ECP_SkeletonCrossbowman_Swamps"),
            new XbowKind("Skeleton_Mountains", "skeleton_bow_mountains", "ECP_SkeletonCrossbowman_Mountains"),
        };

        /// <summary>The game's skeleton prefab.</summary>
        public readonly string Skeleton;

        /// <summary>Its archer's bow, one of its random weapons (not a network prefab).</summary>
        public readonly string Bow;

        /// <summary>The crossbowman's prefab, and its shot's.</summary>
        public readonly string Creature, Shot;

        /// <summary>The crossbowman, once built; null until the first ZNetScene wakes, or when the game lacks the skeleton.</summary>
        public GameObject? Prefab;

        /// <summary>The damage of the skeleton's own bow, all kinds added (the crossbow deals it as blunt, times `Damage Factor`).</summary>
        public float BowDamage;

        private XbowKind(string skeleton, string bow, string creature)
        {
            (Skeleton, Bow, Creature, Shot) = (skeleton, bow, creature, creature + "_shot");
        }

        /// <summary>The kind whose skeleton this prefab is, or null.</summary>
        public static XbowKind? OfSkeleton(GameObject? prefab) => prefab == null ? null : All.FirstOrDefault(k => k.Skeleton == prefab.name);

        /// <summary>The kind whose crossbowman this creature is (by its prefab name), or null.</summary>
        public static XbowKind? OfCreature(string prefabName) => All.FirstOrDefault(k => k.Creature == prefabName);
    }
}
