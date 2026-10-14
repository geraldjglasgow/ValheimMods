using System.Collections;
using System.Collections.Generic;
using EliteCreaturesReborn.Arena.Trial;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Rewards
{
    /// <summary>
    /// A player's reward arriving (features/arena.md section 3, "Rewards"), on their own machine: every item into their
    /// inventory, what does not fit dropped at their feet (the stacks the game would make), each shown at the top left
    /// with its icon as the game shows an item picked up, the skill XP bonus (<see cref="SkillBonus"/>), and a moment
    /// later, after the trial's end message, "Hel rewards you: ..." with the names in the player's language, in the
    /// centre and in the chat window (a player's report 2026-10-14: the reward was hard to make out). A player who is dead when it arrives gets it as they respawn, where they respawn;
    /// one who is offline at the end gets nothing.
    /// </summary>
    internal static class RewardGrant
    {
        /// <summary>Seconds after the reward before its message, so the trial's end message is read first.</summary>
        private const float MessageDelay = 4f;

        private static RewardList? _pending;

        /// <summary>The reward from the core's owner.</summary>
        public static void Receive(ZPackage pkg)
        {
            RewardList? list = RewardList.Unpack(pkg);
            if (list == null)
            {
                return;
            }
            _pending = list;
            GrantPending();
        }

        /// <summary>Pays a reward waiting for the local player, when they are alive (on arrival, or at the respawn).</summary>
        public static void GrantPending()
        {
            Player player = Player.m_localPlayer;
            RewardList? list = _pending;
            if (list == null || player == null || player.IsDead())
            {
                return;
            }
            _pending = null;
            foreach (RewardList.Entry entry in list.Entries)
            {
                Give(player, entry.Prefab, entry.Count);
            }
            SkillBonus.Pay(player, list.Trial, list.XpFactor);
            Announce(RewardText.For(list));
        }

        private static void Give(Player player, string name, int count)
        {
            GameObject? prefab = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(name) : null;
            ItemDrop? item = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            if (item == null)
            {
                return; // this game has no such item: nothing to give
            }
            Inventory inventory = player.GetInventory();
            int stack = Mathf.Max(1, item.m_itemData.m_shared.m_maxStackSize);
            bool dropped = false;
            for (int left = count; left > 0; left -= stack)
            {
                int amount = Mathf.Min(left, stack);
                if (!inventory.CanAddItem(prefab, amount) || !inventory.AddItem(prefab, amount))
                {
                    Drop(prefab!, amount, player.transform.position);
                    dropped = true;
                }
            }
            Show(player, item.m_itemData, count, dropped);
        }

        // The item's own line at the top left with its icon, as the game shows an item picked up: added, or dropped at
        // the player's feet when it did not all fit.
        private static void Show(Player player, ItemDrop.ItemData item, int count, bool dropped)
        {
            string words = (dropped ? "$msg_dropped " : "$msg_added ") + item.m_shared.m_name;
            player.Message(MessageHud.MessageType.TopLeft, words, count, item.GetIcon());
        }

        // One stack at the player's feet, as the game drops an item: a networked item this machine owns until picked up.
        private static void Drop(GameObject prefab, int amount, Vector3 at)
        {
            Vector2 aside = Random.insideUnitCircle * 0.5f;
            GameObject dropped = Object.Instantiate(prefab, at + new Vector3(aside.x, 1f, aside.y), Quaternion.identity);
            ItemDrop drop = dropped.GetComponent<ItemDrop>();
            drop.m_itemData.m_worldLevel = (byte)Game.m_worldLevel;
            drop.SetStack(amount);
        }

        private static void Announce(string text)
        {
            if (text.Length > 0 && MessageHud.instance != null)
            {
                MessageHud.instance.StartCoroutine(Later(text));
            }
        }

        private static IEnumerator Later(string text)
        {
            yield return new WaitForSeconds(MessageDelay);
            if (MessageHud.instance != null)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, text);
            }
            ArenaWords.ToChat(text);
        }
    }
}
