using System.Collections.Generic;
using UnityEngine;

namespace EliteCreaturesPack.Arsenal
{
    /// <summary>
    /// One of the game's skeletons whose spawns the arsenal skeletons may take: the Black Forest's Skeleton (40 health,
    /// a 25-slash sword), the Meadows' (30, 15), the Swamps' (60, 48 slash and 10 chop) and the Mountains' (75, 60 and
    /// 15). Each arsenal skeleton is a copy of the kind's archer skeleton (its health, resistances, faction, looks,
    /// sounds, senses, AI and drops), and the kind's no-archer skeleton's spawns are taken too, by every weapon but the
    /// bow. The poison, Hildir, Deep North and summoned skeletons are left alone.
    /// </summary>
    public sealed class ArsenalKind
    {
        public static readonly ArsenalKind[] All =
        {
            new ArsenalKind("Skeleton", "Skeleton_NoArcher", "skeleton_sword", "skeleton_bow", ""),
            new ArsenalKind("Skeleton_Meadows", "Skeleton_Meadows_noarcher", "skeleton_sword_meadows", "skeleton_bow_meadows", "_Meadows"),
            new ArsenalKind("Skeleton_Swamps", "Skeleton_Swamps_noarcher", "skeleton_sword_swamps", "skeleton_bow_swamps", "_Swamps"),
            new ArsenalKind("Skeleton_Mountains", "Skeleton_Mountains_noarcher", "skeleton_sword_mountains", "skeleton_bow_mountains", "_Mountains"),
        };

        private static readonly Dictionary<string, (ArsenalKind, ArsenalWeapon)> byName = new Dictionary<string, (ArsenalKind, ArsenalWeapon)>();

        /// <summary>The game's skeletons (with and without archers), and their sword and bow (random weapons, not network prefabs).</summary>
        public readonly string Skeleton, NoArcher, Sword, Bow;

        /// <summary>Added to every name made from this kind ("_Swamps"); the Black Forest's has none.</summary>
        public readonly string Suffix;

        /// <summary>The arsenal skeletons built for this kind, by weapon; a weapon whose build failed is missing.</summary>
        public readonly Dictionary<ArsenalWeapon, GameObject> Creatures = new Dictionary<ArsenalWeapon, GameObject>();

        private ArsenalKind(string skeleton, string noArcher, string sword, string bow, string suffix)
        {
            (Skeleton, NoArcher, Sword, Bow, Suffix) = (skeleton, noArcher, sword, bow, suffix);
        }

        /// <summary>The creature's prefab name ("ECP_SkeletonCutthroat_Swamps"), hashed into worlds: never renamed.</summary>
        public string Creature(ArsenalWeapon weapon) => "ECP_Skeleton" + weapon.Title + Suffix;

        /// <summary>The creature's weapon, an item only it carries.</summary>
        public string Attack(ArsenalWeapon weapon) => Creature(weapon) + "_attack";

        public void Add(ArsenalWeapon weapon, GameObject creature)
        {
            Creatures[weapon] = creature;
            byName[creature.name] = (this, weapon);
        }

        /// <summary>The kind whose skeleton this is, and whether that skeleton comes as an archer, or null.</summary>
        public static ArsenalKind? OfSource(GameObject? prefab, out bool archer)
        {
            archer = false;
            foreach (ArsenalKind kind in All)
            {
                if (prefab != null && (prefab.name == kind.Skeleton || prefab.name == kind.NoArcher))
                {
                    archer = prefab.name == kind.Skeleton;
                    return kind;
                }
            }
            return null;
        }

        /// <summary>The kind and weapon of an arsenal skeleton, by its prefab name.</summary>
        public static bool OfCreature(string prefabName, out ArsenalKind kind, out ArsenalWeapon weapon)
        {
            bool found = byName.TryGetValue(prefabName, out var pair);
            (kind, weapon) = pair;
            return found;
        }
    }
}
