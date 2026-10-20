using UnityEngine;

namespace EliteCreaturesReborn.Rendering
{
    /// <summary>
    /// What a render hands over: the items go into the player's own inventory, and what does not fit drops at their feet
    /// as the game's own networked item drop (everyone sees it, anyone can pick it up). Each gift shows the game's
    /// pick-up line in the top left, with the item's icon and amount. Runs on the rendering player's machine only.
    /// </summary>
    internal static class RenderOutput
    {
        /// <summary>The item prefab by name; null (and the roll is skipped) when this game has none.</summary>
        public static GameObject? Prefab(string name)
        {
            GameObject? prefab = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(name) : null;
            return prefab != null && prefab.GetComponent<ItemDrop>() != null ? prefab : null;
        }

        /// <summary>Gives <paramref name="amount"/> of the item: into the inventory a stack at a time, the rest dropped.</summary>
        public static void Give(Humanoid user, GameObject prefab, int amount)
        {
            ItemDrop.ItemData item = prefab.GetComponent<ItemDrop>().m_itemData;
            int left = amount;
            while (left > 0)
            {
                int stack = Mathf.Min(left, Mathf.Max(1, item.m_shared.m_maxStackSize));
                left -= stack;
                Inventory inventory = user.GetInventory();
                if (inventory.CanAddItem(prefab, stack))
                {
                    inventory.AddItem(prefab, stack);
                }
                else
                {
                    Drop(user, prefab, stack);
                }
            }
            user.Message(MessageHud.MessageType.TopLeft, "$msg_added " + item.m_shared.m_name, amount, item.GetIcon());
        }

        private static void Drop(Humanoid user, GameObject prefab, int stack)
        {
            ItemDrop.ItemData item = prefab.GetComponent<ItemDrop>().m_itemData.Clone();
            item.m_dropPrefab = prefab;
            item.m_worldLevel = (byte)Game.m_worldLevel;
            Transform body = user.transform;
            ItemDrop.DropItem(item, stack, body.position + body.forward * 0.5f + Vector3.up, Quaternion.identity);
        }
    }
}
