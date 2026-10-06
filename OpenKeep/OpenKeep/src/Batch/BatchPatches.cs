using HarmonyLib;
using PatchGuard;

namespace OpenKeep.Batch
{
    /// <summary>
    /// The crafting panel hooks. Signatures verified against the decompiled InventoryGui: Awake(), UpdateRecipe(Player
    /// player, float dt), OnCraftPressed(), DoCrafting(Player player). UpdateRecipe runs every frame the panel is open:
    /// the prefix sets the craft durations (Craft Speed) and drives the game's multi-craft before the game reads them,
    /// the postfix lays the stepper out after the game has set the Craft button.
    /// </summary>
    [HarmonyPatch]
    public static class BatchPatches
    {
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
        [HarmonyPostfix]
        public static void AfterAwake(InventoryGui __instance)
        {
            BatchDrive.Remember(__instance);
            CraftSpeed.Remember(__instance);
            Guard.Run("batch stepper", () => BatchStepper.Create(__instance));
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipe))]
        [HarmonyPrefix]
        public static void BeforeUpdateRecipe(InventoryGui __instance, Player player)
        {
            CraftSpeed.Apply(__instance);
            if (!BatchAmount.Applies(__instance, player))
            {
                BatchDrive.Release(__instance);
                return;
            }
            BatchAmount.Follow(player, __instance.m_selectedRecipe.Recipe);
            BatchDrive.Drive(__instance, BatchAmount.Value);
        }

        /// <summary>Drives the amount once more (a click can land before this frame's UpdateRecipe) and remembers that it did.</summary>
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnCraftPressed))]
        [HarmonyPrefix]
        public static void BeforeCraftPressed(InventoryGui __instance, out int __state)
        {
            __state = 0;
            BatchDrive.Forget();
            if (__instance.m_craftTimer >= 0f || !BatchAmount.Applies(__instance, Player.m_localPlayer))
                return;
            BatchDrive.Drive(__instance, BatchAmount.Value);
            __state = BatchAmount.Value;
        }

        /// <summary>A started craft is one whose bar now runs (the game sets the timer to 0 last, after every refusal).</summary>
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnCraftPressed))]
        [HarmonyPostfix]
        public static void AfterCraftPressed(InventoryGui __instance, int __state)
        {
            if (__state > 0 && __instance.m_craftTimer >= 0f)
                BatchDrive.Started(__instance, __state);
        }

        /// <summary>First, so every other DoCrafting prefix (Reach's payment window, GrindstoneSkills' batch count) reads the started amount.</summary>
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        public static void BeforeDoCrafting(InventoryGui __instance) => BatchDrive.Restore(__instance);

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
        [HarmonyFinalizer]
        public static void AfterDoCrafting() => BatchDrive.Forget();
    }
}
