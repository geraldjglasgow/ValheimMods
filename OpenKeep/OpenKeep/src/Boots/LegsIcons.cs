using System.Collections.Generic;
using ItemCopies;
using UnityEngine;

using SharedData = ItemDrop.ItemData.SharedData;

namespace OpenKeep.Boots
{
    /// <summary>
    /// While Separate Boots is on, the 20 leggings show the workshop's trousers icon (<see cref="BootsIcons.Pants"/>), since
    /// the feet are no longer theirs; off, their own icons again, remembered at first sight. Written into the prefabs and
    /// every live copy (ItemCopies); a leggings with several style variants gets the trousers icon for each. Display only:
    /// every client applies it from the synced switch.
    /// </summary>
    public static class LegsIcons
    {
        private static readonly Dictionary<string, Sprite[]> originals = new Dictionary<string, Sprite[]>();

        /// <summary>Remembers a leggings' own icons the first time it is seen.</summary>
        public static void Remember(GameObject legsPrefab)
        {
            if (!originals.ContainsKey(legsPrefab.name))
                originals[legsPrefab.name] = legsPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_icons;
        }

        public static void Apply(bool on)
        {
            var icons = new Dictionary<string, Sprite[]>();
            foreach (KeyValuePair<string, Sprite[]> entry in originals)
                icons[entry.Key] = on ? Trousers(entry.Key, entry.Value) : entry.Value;
            Copies.Apply(icons.Keys, (name, shared) => Write(shared, icons[name]));
        }

        private static void Write(SharedData shared, Sprite[] icons)
        {
            if (icons != null)
                shared.m_icons = icons;
        }

        /// <summary>The trousers icon as often as the leggings have variants; their own when it is missing.</summary>
        private static Sprite[] Trousers(string legs, Sprite[] own)
        {
            BootSet set = BootSets.ByLegs(legs);
            Sprite pants = set != null ? BootsIcons.Pants(set) : null;
            if (pants == null)
                return own;
            var icons = new Sprite[Mathf.Max(1, own != null ? own.Length : 0)];
            for (int i = 0; i < icons.Length; i++)
                icons[i] = pants;
            return icons;
        }
    }
}
