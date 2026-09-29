using HarmonyLib;

namespace EliteCreaturesPack.Mimic
{
    /// <summary>
    /// A dormant mimic is a chest to anyone looking: its hover text and name are the crypt chest's own ("[E] Open"),
    /// decided after every other mod's decoration (Elite Creatures Reborn's mutation words included, when it is installed), and no name plate or health bar
    /// shows over it. Awake, it is an ordinary creature again.
    /// </summary>
    [HarmonyPatch(typeof(Character))]
    public static class MimicDisguisePatch
    {
        [HarmonyPatch("GetHoverText"), HarmonyPostfix, HarmonyPriority(Priority.Last)]
        private static void HoverText(Character __instance, ref string __result)
        {
            if (Dormancy.Holds(__instance))
            {
                __result = ChestLook.HoverText();
            }
        }

        [HarmonyPatch("GetHoverName"), HarmonyPostfix, HarmonyPriority(Priority.Last)]
        private static void HoverName(Character __instance, ref string __result)
        {
            if (Dormancy.Holds(__instance))
            {
                __result = ChestLook.Name();
            }
        }
    }

    [HarmonyPatch(typeof(EnemyHud), "TestShow")]
    public static class MimicHudPatch
    {
        private static void Postfix(Character c, ref bool __result)
        {
            if (__result && Dormancy.Holds(c))
            {
                __result = false;
            }
        }
    }
}
