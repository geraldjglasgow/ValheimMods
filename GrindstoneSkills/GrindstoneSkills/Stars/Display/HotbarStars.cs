using System.Collections.Generic;
using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Stars on the hotbar's icons. HotkeyBar.UpdateIcons runs every frame: it lists the items in the inventory's first
    /// row (m_items), gives each the slot at its column and marks that slot m_used. When the player is missing or dead
    /// it destroys every slot and returns before refreshing m_items, so the column is checked against the slots that
    /// exist. The Stars On Icons setting is read every time.
    /// </summary>
    [HarmonyPatch(typeof(HotkeyBar), nameof(HotkeyBar.UpdateIcons))]
    public static class HotbarStars
    {
        [HarmonyPostfix]
        private static void Postfix(HotkeyBar __instance)
        {
            List<HotkeyBar.ElementData> slots = __instance.m_elements;
            foreach (HotkeyBar.ElementData slot in slots)
                if (!slot.m_used)
                    StarBadge.Apply(slot.m_go, 0);
            bool on = DisplaySettings.StarsOnIcons.Value;
            foreach (ItemDrop.ItemData item in __instance.m_items)
            {
                int column = item.m_gridPos.x;
                if (column >= 0 && column < slots.Count)
                    StarBadge.Apply(slots[column].m_go, on ? Stars.Get(item) : 0);
            }
        }
    }
}
