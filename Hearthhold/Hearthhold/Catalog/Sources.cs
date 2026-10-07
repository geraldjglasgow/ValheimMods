using System.Collections.Generic;

namespace Hearthhold
{
    /// <summary>
    /// Which pickables and creature drops roll stars where they spawn. Forage: a pickable that grows from no plant, whose
    /// item can be eaten (berries, mushrooms...) or is a cooking herb (<see cref="ForageExtras"/>), plus honey from hives
    /// and sap from sap collectors. Crops: a pickable some plant grows (carrots, turnips, onions,
    /// barley, Jotun puffs, magecap...), wild or planted, whose item a kitchen uses or that can be eaten; flax is neither.
    /// Meat: what a creature drops that can be eaten or that a cooking station cooks (raw meats), plus entrails and blood
    /// bags (<see cref="MeatExtras"/>). Creature parts with other uses (feathers, eyes, coal) stay plain. Eggs are GrindstoneSkills' own
    /// (their stars are the hen's level) and never roll. Filled by <see cref="Discovery"/>, keyed by prefab name.
    /// </summary>
    public static class Sources
    {
        /// <summary>Wild picks that are not eaten as they are but are cooking herbs: they roll stars too.</summary>
        public static readonly HashSet<string> ForageExtras = new HashSet<string>
        {
            "Dandelion", "Thistle", "Fiddleheadfern", "MushroomSmokePuff", "RoyalJelly", "Sap",
        };

        /// <summary>Creature parts that are not cooked on a station but are cooking ingredients: they roll stars too.</summary>
        public static readonly HashSet<string> MeatExtras = new HashSet<string> { "Entrails", "Bloodbag" };

        private static readonly HashSet<string> cropPickables = new HashSet<string>();
        private static readonly HashSet<string> forageItems = new HashSet<string>();
        private static readonly HashSet<string> cropItems = new HashSet<string>();
        private static readonly HashSet<string> meatItems = new HashSet<string>();

        /// <summary>Whether this pickable prefab grows from a plant.</summary>
        public static bool IsCropPickable(string pickablePrefab) => pickablePrefab != null && cropPickables.Contains(pickablePrefab);

        /// <summary>Whether a pick of this pickable rolls stars, and as which source.</summary>
        public static bool TryPickSource(Pickable pickable, out StarSource source)
        {
            source = StarSource.Forage;
            string item = pickable != null && pickable.m_itemPrefab != null ? pickable.m_itemPrefab.name : null;
            if (item == null)
                return false;
            if (IsCropPickable(Utils.GetPrefabName(pickable.gameObject)))
            {
                source = StarSource.Crop;
                return cropItems.Contains(item);
            }
            return forageItems.Contains(item);
        }

        public static bool IsForageItem(string itemPrefab) => itemPrefab != null && forageItems.Contains(itemPrefab);

        public static bool IsCropItem(string itemPrefab) => itemPrefab != null && cropItems.Contains(itemPrefab);

        public static bool IsMeatItem(string itemPrefab) => itemPrefab != null && meatItems.Contains(itemPrefab);

        internal static void AddCropPickable(string pickablePrefab) => cropPickables.Add(pickablePrefab);

        internal static void AddForage(string itemPrefab) => forageItems.Add(itemPrefab);

        internal static void AddCrop(string itemPrefab) => cropItems.Add(itemPrefab);

        internal static void AddMeat(string itemPrefab) => meatItems.Add(itemPrefab);

        internal static IEnumerable<string> AllItems()
        {
            foreach (string name in forageItems)
                yield return name;
            foreach (string name in cropItems)
                yield return name;
            foreach (string name in meatItems)
                yield return name;
        }
    }
}
