using System;
using System.Collections.Generic;
using EliteCraftingLink;
using UnityEngine;

namespace OpenKeep.Salvage
{
    /// <summary>
    /// EliteCrafting's Magic and Rare items, salvaged knowingly. While EliteCrafting is installed its item data (keys
    /// starting with <c>ecf_</c>) never counts as another mod's data, and the rune EliteCrafting says the item may give
    /// back joins the returns with its chance (an Awakening Rune from Magic, an Ascension Rune from Rare). The chance is
    /// rolled once, when the stack is salvaged. Asked through EliteCraftingLink, so nothing of EliteCrafting is
    /// referenced; without it nothing changes.
    /// </summary>
    public static class EliteCraftingSalvage
    {
        private const string KeyPrefix = "ecf_";

        /// <summary>An item data key EliteCrafting keeps, while EliteCrafting is installed.</summary>
        public static bool Owns(string key) => key.StartsWith(KeyPrefix, StringComparison.Ordinal) && CraftingLink.Present;

        /// <summary>Adds the rune the item may give back, one per stack with its chance, to the returns.</summary>
        public static void AddRune(ItemDrop.ItemData item, List<SalvageReturn> returns)
        {
            if (!CraftingLink.Present || ObjectDB.instance == null)
                return;
            string prefab = CraftingItems.GetSalvageRune(item);
            if (string.IsNullOrEmpty(prefab))
                return;
            float chance = Mathf.Min(CraftingItems.GetSalvageRuneChance(item), 1f);
            GameObject rune = chance > 0f ? ObjectDB.instance.GetItemPrefab(prefab) : null;
            ItemDrop drop = rune != null ? rune.GetComponent<ItemDrop>() : null;
            if (drop != null)
                returns.Add(new SalvageReturn(drop, 1, chance));
        }

        /// <summary>The returns as they come out: each chance rolled, a won one kept as certain, a lost one left out.</summary>
        public static List<SalvageReturn> Roll(List<SalvageReturn> returns)
        {
            List<SalvageReturn> rolled = new List<SalvageReturn>(returns.Count);
            foreach (SalvageReturn entry in returns)
            {
                if (entry.IsCertain)
                    rolled.Add(entry);
                else if (UnityEngine.Random.value < entry.Chance)
                    rolled.Add(new SalvageReturn(entry.Item, entry.Amount));
            }
            return rolled;
        }
    }
}
