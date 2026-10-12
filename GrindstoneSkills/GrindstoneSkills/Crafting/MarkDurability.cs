using System.Collections.Generic;
using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Maker's Mark on max durability, on every machine. ItemData.GetMaxDurability() calls GetMaxDurability(int quality)
    /// with the item's own quality, and the tooltip, repairs (which fill to the max), wear and the durability bars all end
    /// there, so one postfix on the int overload covers them. It multiplies the result the game and other mods' earlier
    /// postfixes gave (Elite Crafting's durability property patches the same overload), never replaces it. The current
    /// durability is an absolute number and is left alone, so a marked item reads as worn by the same amount everywhere.
    /// Hot: an item without custom data costs two field reads.
    /// </summary>
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetMaxDurability), typeof(int))]
    public static class MarkDurability
    {
        [HarmonyPostfix]
        private static void Postfix(ItemDrop.ItemData __instance, ref float __result)
        {
            Dictionary<string, string> data = __instance.m_customData;
            if (data != null && data.Count > 0)
                __result *= HookGuard.Run("Maker's Mark durability", static item => MarkReader.Factor(item), __instance, 1f);
        }
    }
}
