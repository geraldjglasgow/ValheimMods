using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// The tooltip title of a starred egg in an inventory or container grid gets its stars. InventoryGrid refreshes
    /// the hovered slot's tooltip every frame in CreateItemTooltip, setting the item's shared name as the title and
    /// GetTooltip as the text; UITooltip.Set does nothing while neither changes, and localizes both when shown. For a
    /// starred egg this makes that one call with the starred title in the game's place: patching the title after the
    /// game's call would change it twice per frame and redraw the tooltip every frame.
    /// </summary>
    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.CreateItemTooltip))]
    public static class TooltipTopic
    {
        [HarmonyPrefix]
        private static bool Prefix(InventoryGrid __instance, ItemDrop.ItemData item, UITooltip tooltip)
        {
            int stars = Stars.Get(item);
            if (stars <= 0 || tooltip == null)
                return true;
            tooltip.Set(item.m_shared.m_name + " " + StarText.Tier(stars), item.GetTooltip(), __instance.m_tooltipAnchor);
            return false;
        }
    }
}
