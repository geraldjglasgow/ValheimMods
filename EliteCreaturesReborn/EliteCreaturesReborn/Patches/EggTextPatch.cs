using System.Reflection;
using EliteCreaturesReborn.Display;
using HarmonyLib;
using PatchGuard;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>An egg on the ground: its hover text ends with what it will hatch.</summary>
    [HarmonyPatch(typeof(EggGrow), nameof(EggGrow.GetHoverText))]
    public static class EggHoverPatch
    {
        private static void Postfix(EggGrow __instance, ref string __result)
        {
            string current = __result;
            __result = Guard.Run("EggGrow.GetHoverText traits", () => Append(__instance, current));
        }

        private static string Append(EggGrow egg, string text)
        {
            string? line = egg.m_item != null ? EggText.For(egg.m_item.m_itemData) : null;
            return line != null ? text + "\n" + line : text;
        }
    }

    /// <summary>
    /// An egg in an inventory: its tooltip ends with what it will hatch. The game's tooltip builder is overloaded and
    /// its signature has changed between game versions, so the target is looked up by name, and a game whose builder no
    /// longer matches simply skips this patch rather than stopping the mod from loading.
    /// </summary>
    [HarmonyPatch]
    public static class EggTooltipPatch
    {
        private static bool Prepare() => Target() != null;

        private static MethodBase? TargetMethod() => Target();

        private static MethodBase? Target() => AccessTools.Method(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip),
            new[] { typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(bool) });

        private static void Postfix(ItemDrop.ItemData item, ref string __result)
        {
            string current = __result;
            __result = Guard.Run("ItemData.GetTooltip egg traits", () => Append(item, current));
        }

        private static string Append(ItemDrop.ItemData item, string text)
        {
            string? line = EggText.For(item);
            return line != null ? text + "\n\n" + line : text;
        }
    }
}
