using EliteCrafting.Affixes;
using HarmonyLib;

namespace EliteCrafting.Display
{
    /// <summary>
    /// Rarity-colored item names at the Phase 1 surfaces (display.md section 2). The shared <c>m_shared.m_name</c> is
    /// never touched; the color is wrapped around the name where it is drawn. Common, plain and unknown-rarity items,
    /// and every item while <c>Colored item names</c> is off, are left exactly to vanilla. All three run on the viewing
    /// client only; nothing is sent.
    /// </summary>
    internal static class NamePatches
    {
        /// <summary>
        /// Inventory and container grid tooltip title. Runs every frame for the hovered slot, so it takes over with a
        /// single <c>Set</c> (never a second Set in a postfix: the tooltip would rebuild every frame) using the cached
        /// colored topic. Plain items return true and vanilla runs untouched.
        /// </summary>
        [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.CreateItemTooltip))]
        private static class GridTooltipPatch
        {
            [HarmonyPrefix]
            private static bool Prefix(InventoryGrid __instance, ItemDrop.ItemData item, UITooltip tooltip)
            {
                if (item == null || tooltip == null)
                {
                    return true;
                }
                string? topic = DisplayCache.Topic(ItemState.Read(item), item);
                if (topic == null)
                {
                    return true;
                }
                tooltip.Set(topic, item.GetTooltip(), __instance.m_tooltipAnchor);
                return false;
            }
        }

        /// <summary>
        /// Pickup message at the top left ("Bronze sword x1"). The local player calls it when it picks an item up;
        /// the replacement shows the same message with the colored name.
        /// </summary>
        [HarmonyPatch(typeof(Character), nameof(Character.ShowPickupMessage))]
        private static class PickupMessagePatch
        {
            [HarmonyPrefix]
            private static bool Prefix(Character __instance, ItemDrop.ItemData item, int amount)
            {
                if (item == null)
                {
                    return true;
                }
                string? topic = DisplayCache.Topic(ItemState.Read(item), item);
                if (topic == null)
                {
                    return true;
                }
                __instance.Message(MessageHud.MessageType.TopLeft, "$msg_added " + topic, amount, item.GetIcon());
                return false;
            }
        }

        /// <summary>
        /// Dropped item hover text ("Bronze sword [2]\n[E] Pick up"): the first line's name is wrapped in the rarity
        /// color. Called every frame while hovered; <c>GetHoverText</c> has already refreshed the item data with
        /// <c>Load()</c>. See <see cref="HoverName"/>.
        /// </summary>
        [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.GetHoverText))]
        private static class HoverTextPatch
        {
            [HarmonyPostfix]
            private static void Postfix(ItemDrop __instance, ref string __result)
            {
                if (__result == null)
                {
                    return;
                }
                string? tag = HoverName.Tag(__instance.m_itemData);
                if (tag != null)
                {
                    __result = HoverName.Color(__result, __instance.m_itemData.m_shared.m_name, tag);
                }
            }
        }
    }
}
