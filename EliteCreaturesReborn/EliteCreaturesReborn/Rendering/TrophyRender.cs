using System.Collections.Generic;
using EliteCreaturesReborn.Config;
using Translations;
using UnityEngine;

namespace EliteCreaturesReborn.Rendering
{
    /// <summary>
    /// One render at a Rendering Table, on the rendering player's machine with their own inventory (nothing of the table
    /// changes, so no owner is asked). E takes the first trophy in the inventory's grid order, or for an idol the first
    /// whose biome has five; a boss trophy is never taken by E, only when the player uses it on the table from the hotbar
    /// (it also unlocks a Forsaken power, so it is never spent by accident). Ores and ingredients: one trophy, one roll of
    /// <see cref="RenderTiers.LeastPerRoll"/>-<see cref="RenderTiers.MostPerRoll"/> of a random item of its tier, five
    /// rolls for a boss trophy. Idols: five trophies of one tier, or one boss trophy, make one idol of that tier.
    /// </summary>
    internal static class TrophyRender
    {
        /// <summary>E, and its repeats while held (<paramref name="quiet"/>: no message when there is nothing left).</summary>
        public static bool Next(Humanoid user, RenderMode mode, Vector3 at, bool quiet)
        {
            if (!TableSettings.TableEnabled)
            {
                return Tell(user, RenderWords.Off, quiet);
            }
            Inventory inventory = user.GetInventory();
            ItemDrop.ItemData? trophy = mode == RenderMode.Idols ? FirstFullTier(inventory) : FirstTrophy(inventory);
            if (trophy == null)
            {
                return Tell(user, mode == RenderMode.Idols ? RenderWords.NeedFive : RenderWords.None, quiet);
            }
            return Render(user, inventory, trophy, mode, at);
        }

        /// <summary>A trophy used on the table from the hotbar; false for any other item (the game says it cannot be used).
        /// A refusal is the table's own message and counts as handled, so the game adds none of its own.</summary>
        public static bool One(Humanoid user, ItemDrop.ItemData item, RenderMode mode, Vector3 at)
        {
            if (item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Trophy)
            {
                return false;
            }
            string? refusal = Refusal(user.GetInventory(), item, mode);
            if (refusal != null)
            {
                Tell(user, refusal, false);
                return true;
            }
            Render(user, user.GetInventory(), item, mode, at);
            return true;
        }

        private static string? Refusal(Inventory inventory, ItemDrop.ItemData item, RenderMode mode)
        {
            if (!TableSettings.TableEnabled)
            {
                return RenderWords.Off;
            }
            if (!RenderTiers.TryTier(item, out int tier, out bool boss))
            {
                return RenderWords.Unknown;
            }
            bool tooFew = mode == RenderMode.Idols && !boss && Count(inventory, tier) < RenderTiers.PerIdol;
            return tooFew ? RenderWords.NeedFive : null;
        }

        private static bool Render(Humanoid user, Inventory inventory, ItemDrop.ItemData trophy, RenderMode mode, Vector3 at)
        {
            RenderTiers.TryTier(trophy, out int tier, out bool boss);
            bool made = mode == RenderMode.Idols
                ? Idol(user, inventory, trophy, tier, boss)
                : Rolls(user, inventory, trophy, mode, tier, boss);
            if (made)
            {
                RenderPrefab.PlayDone(at);
            }
            return made;
        }

        private static bool Rolls(Humanoid user, Inventory inventory, ItemDrop.ItemData trophy, RenderMode mode, int tier, bool boss)
        {
            List<GameObject> outputs = Existing(RenderTiers.Outputs(mode, tier));
            if (outputs.Count == 0)
            {
                return Tell(user, RenderWords.NothingMade, false);
            }
            inventory.RemoveItem(trophy, 1);
            int rolls = boss ? RenderTiers.PerIdol : 1;
            for (int i = 0; i < rolls; i++)
            {
                GameObject prefab = outputs[Random.Range(0, outputs.Count)];
                RenderOutput.Give(user, prefab, Random.Range(RenderTiers.LeastPerRoll, RenderTiers.MostPerRoll + 1));
            }
            return true;
        }

        private static bool Idol(Humanoid user, Inventory inventory, ItemDrop.ItemData trophy, int tier, bool boss)
        {
            GameObject? idol = RenderOutput.Prefab(RenderTiers.Idol(tier));
            if (idol == null)
            {
                return Tell(user, RenderWords.NothingMade, false);
            }
            if (boss)
            {
                inventory.RemoveItem(trophy, 1);
            }
            else
            {
                Take(inventory, trophy, tier, RenderTiers.PerIdol);
            }
            RenderOutput.Give(user, idol, 1);
            return true;
        }

        // The chosen trophy's stack first, then the tier's other trophies in grid order.
        private static void Take(Inventory inventory, ItemDrop.ItemData first, int tier, int count)
        {
            int taken = Mathf.Min(first.m_stack, count);
            inventory.RemoveItem(first, taken);
            foreach (ItemDrop.ItemData item in inventory.GetAllItemsInGridOrder())
            {
                if (taken >= count)
                {
                    return;
                }
                if (item != first && OfTier(item, tier))
                {
                    int part = Mathf.Min(item.m_stack, count - taken);
                    inventory.RemoveItem(item, part);
                    taken += part;
                }
            }
        }

        private static ItemDrop.ItemData? FirstTrophy(Inventory inventory)
        {
            foreach (ItemDrop.ItemData item in inventory.GetAllItemsInGridOrder())
            {
                if (RenderTiers.TryTier(item, out _, out bool boss) && !boss)
                {
                    return item;
                }
            }
            return null;
        }

        private static ItemDrop.ItemData? FirstFullTier(Inventory inventory)
        {
            foreach (ItemDrop.ItemData item in inventory.GetAllItemsInGridOrder())
            {
                if (RenderTiers.TryTier(item, out int tier, out bool boss) && !boss
                    && Count(inventory, tier) >= RenderTiers.PerIdol)
                {
                    return item;
                }
            }
            return null;
        }

        /// <summary>The trophies of one tier the inventory holds, boss trophies left out.</summary>
        private static int Count(Inventory inventory, int tier)
        {
            int count = 0;
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                count += OfTier(item, tier) ? item.m_stack : 0;
            }
            return count;
        }

        private static bool OfTier(ItemDrop.ItemData item, int tier) =>
            RenderTiers.TryTier(item, out int its, out bool boss) && !boss && its == tier;

        private static List<GameObject> Existing(string[] names)
        {
            List<GameObject> found = new List<GameObject>();
            foreach (string name in names)
            {
                GameObject? prefab = RenderOutput.Prefab(name);
                if (prefab != null)
                {
                    found.Add(prefab);
                }
            }
            return found;
        }

        // A refusal, in the middle of the screen; a held E that ran out says nothing and stops.
        private static bool Tell(Humanoid user, string key, bool quiet)
        {
            if (!quiet)
            {
                user.Message(MessageHud.MessageType.Center, Lang.Get(key));
            }
            return false;
        }
    }
}
