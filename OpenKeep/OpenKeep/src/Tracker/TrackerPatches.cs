using HarmonyLib;

namespace OpenKeep.Tracker
{
    /// <summary>
    /// The tracker's hooks. Signatures verified against the decompiled game: Hud.Awake() (private), and
    /// InventoryGui.DoCrafting(Player player) (private, the end of the craft bar). The DoCrafting prefix runs late, after
    /// the Batch module has put the started craft's amount back.
    /// </summary>
    [HarmonyPatch]
    public static class TrackerPatches
    {
        [HarmonyPatch(typeof(Hud), nameof(Hud.Awake))]
        [HarmonyPostfix]
        public static void AfterHudAwake(Hud __instance)
        {
            if (__instance.GetComponent<TrackerHud>() == null)
                __instance.gameObject.AddComponent<TrackerHud>();
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Low)]
        public static void BeforeCrafting(InventoryGui __instance, Player player, out CraftWatch __state)
        {
            __state = CraftWatch.Start(__instance, player);
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
        [HarmonyPostfix]
        public static void AfterCrafting(Player player, CraftWatch __state) => __state?.Finish(player);
    }
}
