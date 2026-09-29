using HarmonyLib;
using PackPanel.Core;
using UnityEngine;

namespace PackPanel.Panels
{
    /// <summary>The same stacked carry-weight fraction in the inventory and on the HUD, including overload flashing.</summary>
    public static class WeightDisplay
    {
        public static string Format(Player player)
        {
            int weight = Mathf.CeilToInt(player.GetInventory().GetTotalWeight());
            int max = Mathf.CeilToInt(player.GetMaxCarryWeight());
            string current = weight > max && Mathf.Sin(Time.time * 10f) > 0f
                ? $"<color=red>{weight}</color>" : weight.ToString();
            return $"<line-height=60%><voffset=-0.12em>{current}</voffset>\n<size=75%>\u2014\u2014</size>\n{max}</line-height>";
        }

        // Verified against the game's UpdateInventoryWeight(Player): it rewrites m_weight every inventory update.
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateInventoryWeight))]
        public static class InventoryWeight
        {
            [HarmonyPostfix]
            public static void Postfix(InventoryGui __instance, Player player)
            {
                if (InventoryState.Active && player != null && __instance.m_weight != null)
                    __instance.m_weight.text = Format(player);
            }
        }
    }
}
