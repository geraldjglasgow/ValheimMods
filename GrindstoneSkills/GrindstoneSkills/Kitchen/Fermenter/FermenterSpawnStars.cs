using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Gives items a fermenter spawns the stars of its base. The game spawns them with Object.Instantiate and then
    /// ItemDrop.OnCreateNew (which only sets the world level and the cheated flag); the item saves m_itemData to its ZDO
    /// in its Start, a frame later. While a fermenter spawns (<see cref="Begin"/> to <see cref="End"/>), every kitchen
    /// item passing OnCreateNew gets the scope's stars in its quality, is rescaled the way ItemDrop.Awake and Load do,
    /// and is saved at once so the quality is in the ZDO before anything else reads it. Stars 0 leaves the prefab's own
    /// quality 1 untouched.
    /// </summary>
    internal static class FermenterSpawnStars
    {
        private static int? scope;

        /// <summary>Starts a spawn scope with these stars; hand the result to <see cref="End"/>.</summary>
        public static int? Begin(int stars)
        {
            int? previous = scope;
            scope = Mathf.Clamp(stars, 0, Stars.Max);
            return previous;
        }

        public static void End(int? previous) => scope = previous;

        /// <summary>A new item, from <see cref="ItemCreated"/> (ItemDrop.OnCreateNew).</summary>
        internal static void OnCreated(ItemDrop item)
        {
            if (scope == null || item == null || !Kitchen.IsKitchenItem(item.m_itemData))
                return;
            int quality = Stars.ToQuality(scope.Value);
            if (item.m_itemData.m_quality == quality)
                return;
            item.SetQuality(quality);
            item.Save();
        }
    }
}
