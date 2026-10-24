using System.Collections.Generic;
using System.Globalization;
using System.Text;
using EliteCreaturesReborn.Rendering;
using EliteCreaturesReborn.Util;
using UnityEngine;

namespace EliteCreaturesReborn.Raids
{
    /// <summary>
    /// Trophies as a stake (features/raids.md section 2; the user, 2026-10-23: "make it so you can pay trophies for the
    /// raiders chest too, so you can earn trophies"). The Raiders Chest takes every trophy the Rendering Table knows (its
    /// tier is its creature's biome, <see cref="RenderTiers"/>) beside coins. Each counts toward the raid's heat as coins:
    /// a tenth of its tier's fair stake (<see cref="RaidTable.TrophyWorth"/>), a boss's five times that. Sounding takes them
    /// out with the coins and keeps them in the host's ZDO (<see cref="RaidState.Trophies"/>, "TrophyBoar:3,TrophyNeck:2");
    /// a won raid puts each kind back into the chest times the band's coins back (x1 to x2, rounded, never fewer), and
    /// what does not fit falls beside it. Any other end loses them, as the coins are lost. The chest's owner only.
    /// </summary>
    internal static class ChestTrophies
    {
        private static readonly StringBuilder Text = new StringBuilder(128);
        private static readonly Dictionary<string, int> Kinds = new Dictionary<string, int>();

        /// <summary>True for a trophy the chest takes.</summary>
        public static bool IsTrophy(ItemDrop.ItemData? item) => item?.m_shared != null
            && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophy && RenderTiers.TryTier(item, out _, out _);

        /// <summary>The trophies in the inventory, counted, and what they are worth as coins together.</summary>
        public static int Count(Inventory inventory, out int worth)
        {
            int count = 0;
            worth = 0;
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                if (IsTrophy(item) && RenderTiers.TryTier(item, out int tier, out bool boss))
                {
                    count += item.m_stack;
                    worth += item.m_stack * RaidTable.TrophyWorth(tier, boss);
                }
            }
            return count;
        }

        /// <summary>Takes every trophy out, as the raid begins; what was taken, for the host's ZDO.</summary>
        public static string Take(Inventory inventory)
        {
            Kinds.Clear();
            foreach (ItemDrop.ItemData item in new List<ItemDrop.ItemData>(inventory.GetAllItems()))
            {
                if (IsTrophy(item) && item.m_dropPrefab != null)
                {
                    string name = item.m_dropPrefab.name;
                    Kinds[name] = (Kinds.TryGetValue(name, out int had) ? had : 0) + item.m_stack;
                    inventory.RemoveItem(item);
                }
            }
            return Write(Kinds);
        }

        /// <summary>A won raid: every kind staked back into the chest, times <paramref name="factor"/>; the rest beside it.
        /// The number given back.</summary>
        public static int Return(Container chest, string staked, float factor)
        {
            int back = 0;
            Inventory inventory = chest.GetInventory();
            foreach (KeyValuePair<string, int> kind in Read(staked))
            {
                GameObject? prefab = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(kind.Key) : null;
                int amount = Mathf.Max(kind.Value, Mathf.RoundToInt(kind.Value * factor));
                if (prefab != null)
                {
                    Put(inventory, prefab, amount, chest.transform.position);
                    back += amount;
                }
            }
            return back;
        }

        // Into the chest a stack at a time while it fits; what does not, dropped just above it.
        private static void Put(Inventory inventory, GameObject prefab, int amount, Vector3 at)
        {
            int stack = Mathf.Max(1, prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_maxStackSize);
            for (int left = amount; left > 0; left -= stack)
            {
                int n = Mathf.Min(left, stack);
                if (!inventory.CanAddItem(prefab, n) || !inventory.AddItem(prefab, n))
                {
                    GameObject made = Object.Instantiate(prefab, at + Vector3.up, Quaternion.identity);
                    made.GetComponent<ItemDrop>().SetStack(n);
                }
            }
        }

        private static string Write(Dictionary<string, int> kinds)
        {
            Text.Clear();
            foreach (KeyValuePair<string, int> kind in kinds)
            {
                Text.Append(Text.Length > 0 ? "," : "").Append(kind.Key).Append(':')
                    .Append(kind.Value.ToString(CultureInfo.InvariantCulture));
            }
            return Text.ToString();
        }

        private static Dictionary<string, int> Read(string staked)
        {
            Kinds.Clear();
            foreach (string part in (staked ?? "").Split(','))
            {
                int colon = part.LastIndexOf(':');
                if (colon > 0 && int.TryParse(part.Substring(colon + 1), NumberStyles.Integer, CultureInfo.InvariantCulture,
                    out int count) && count > 0)
                {
                    Kinds[part.Substring(0, colon)] = count;
                }
            }
            return Kinds;
        }
    }
}
