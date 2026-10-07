using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Saved ingredients that do not fit the crafter's inventory go to the ground in front of them, the way the
    /// game drops an item (Humanoid.DropItem): ItemDrop.DropItem spawns a networked copy of the item's prefab that
    /// keeps its quality, stack and crafter, so everyone sees it and anyone can pick it up. Amounts above the stack
    /// size are dropped as several stacks.
    /// </summary>
    public static class CraftDrop
    {
        public static void AtFeet(Player player, ItemDrop.ItemData item, int amount)
        {
            if (player == null || item == null || item.m_dropPrefab == null)
                return;
            Transform at = player.transform;
            Vector3 position = at.position + at.forward + at.up;
            int perStack = Mathf.Max(1, item.m_shared.m_maxStackSize);
            for (int left = amount; left > 0; left -= perStack)
                ItemDrop.DropItem(item, Mathf.Min(left, perStack), position, at.rotation);
        }
    }
}
