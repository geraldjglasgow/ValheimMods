using System.Collections.Generic;
using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Stars for items a cooking station is about to create. SpawnItem (a dish taken off) and DropAllItems (the
    /// station breaking) both instantiate the item prefab and at once call ItemDrop.OnCreateNew on it, which only sets
    /// the world level and the cheated flag; the item saves m_itemData to its ZDO in its Start, a frame later. While a
    /// scope is open (<see cref="Begin"/> to <see cref="End"/>, on the owner, in one call), each new item takes the
    /// first queued entry with its prefab name: a kitchen item gets the entry's stars in its quality, is rescaled the
    /// way ItemDrop.Awake and Load do, and is saved at once. Entries are queued in the order the station creates its
    /// items, so identical dishes from several slots each take their own slot's stars.
    /// </summary>
    public static class StationSpawnStars
    {
        private struct Entry
        {
            public string Prefab;
            public int Stars;
        }

        private static readonly List<Entry> queue = new List<Entry>();
        private static bool open;

        public static void Begin()
        {
            queue.Clear();
            open = true;
        }

        /// <summary>Queues the stars for the next item created from this prefab.</summary>
        public static void Add(string prefab, int stars)
        {
            if (open && !string.IsNullOrEmpty(prefab))
                queue.Add(new Entry { Prefab = prefab, Stars = stars });
        }

        public static void End()
        {
            open = false;
            queue.Clear();
        }

        [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.OnCreateNew), typeof(ItemDrop), typeof(bool))]
        private static class Created
        {
            [HarmonyPostfix]
            private static void Postfix(ItemDrop item)
            {
                if (open && item != null && item.m_itemData?.m_dropPrefab != null)
                    Apply(item, Take(item.m_itemData.m_dropPrefab.name));
            }
        }

        /// <summary>The stars queued first for this prefab, removed from the queue; 0 when none are.</summary>
        private static int Take(string prefab)
        {
            for (int i = 0; i < queue.Count; i++)
            {
                if (queue[i].Prefab != prefab)
                    continue;
                int stars = queue[i].Stars;
                queue.RemoveAt(i);
                return stars;
            }
            return 0;
        }

        private static void Apply(ItemDrop item, int stars)
        {
            if (stars <= 0 || !Kitchen.IsKitchenItem(item.m_itemData))
                return;
            Stars.Set(item.m_itemData, stars);
            item.SetQuality(item.m_itemData.m_quality);
            item.Save();
        }
    }
}
