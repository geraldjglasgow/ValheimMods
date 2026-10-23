using System.Collections.Generic;
using EliteCreaturesReborn.Config;
using Translations;
using UnityEngine;

namespace EliteCreaturesReborn.Rendering
{
    /// <summary>
    /// One render at a Rendering Table, on the rendering player's machine with their own inventory (nothing of the table
    /// changes, so no owner is asked). E takes the first trophy in the inventory's grid order; a boss trophy is never
    /// taken by E, only when the player uses it on the table from the hotbar (it also unlocks a Forsaken power, so it is
    /// never spent by accident). Ores and ingredients: one trophy, one roll of <see cref="RenderTiers.LeastPerRoll"/>-
    /// <see cref="RenderTiers.MostPerRoll"/> of a random item of its tier, <see cref="RenderTiers.BossRolls"/> rolls for a
    /// boss trophy. Idols: only a boss trophy makes one, of its tier (a player, 2026-10-22: idols from every trophy were
    /// far too many, and gave no reason to fight the bosses), so in Idols mode E only says so.
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
            if (mode == RenderMode.Idols)
            {
                return Tell(user, RenderWords.NeedBoss, quiet);
            }
            Inventory inventory = user.GetInventory();
            ItemDrop.ItemData? trophy = FirstTrophy(inventory);
            return trophy == null ? Tell(user, RenderWords.None, quiet) : Render(user, inventory, trophy, mode, at);
        }

        /// <summary>A trophy used on the table from the hotbar; false for any other item (the game says it cannot be used).
        /// A refusal is the table's own message and counts as handled, so the game adds none of its own.</summary>
        public static bool One(Humanoid user, ItemDrop.ItemData item, RenderMode mode, Vector3 at)
        {
            if (item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Trophy)
            {
                return false;
            }
            string? refusal = Refusal(item, mode);
            if (refusal != null)
            {
                Tell(user, refusal, false);
                return true;
            }
            Render(user, user.GetInventory(), item, mode, at);
            return true;
        }

        private static string? Refusal(ItemDrop.ItemData item, RenderMode mode)
        {
            if (!TableSettings.TableEnabled)
            {
                return RenderWords.Off;
            }
            if (!RenderTiers.TryTier(item, out _, out bool boss))
            {
                return RenderWords.Unknown;
            }
            return mode == RenderMode.Idols && !boss ? RenderWords.NeedBoss : null;
        }

        private static bool Render(Humanoid user, Inventory inventory, ItemDrop.ItemData trophy, RenderMode mode, Vector3 at)
        {
            RenderTiers.TryTier(trophy, out int tier, out bool boss);
            bool made = mode == RenderMode.Idols
                ? Idol(user, inventory, trophy, tier)
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
            int rolls = boss ? RenderTiers.BossRolls : 1;
            for (int i = 0; i < rolls; i++)
            {
                GameObject prefab = outputs[Random.Range(0, outputs.Count)];
                RenderOutput.Give(user, prefab, Random.Range(RenderTiers.LeastPerRoll, RenderTiers.MostPerRoll + 1));
            }
            return true;
        }

        // Only a boss trophy gets here (Refusal turns the others away).
        private static bool Idol(Humanoid user, Inventory inventory, ItemDrop.ItemData trophy, int tier)
        {
            GameObject? idol = RenderOutput.Prefab(RenderTiers.Idol(tier));
            if (idol == null)
            {
                return Tell(user, RenderWords.NothingMade, false);
            }
            inventory.RemoveItem(trophy, 1);
            RenderOutput.Give(user, idol, 1);
            return true;
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
