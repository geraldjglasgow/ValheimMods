using HarmonyLib;

namespace OpenKeep
{
    /// <summary>
    /// One patch per hot game method that several features hook: the shared early exit is tested once, then each
    /// feature is asked in turn, in the order their separate patches used to run. Patches kept apart on purpose: the
    /// typing guard's first prefix on <c>InventoryGui.Update</c> and Batch's <c>UpdateRecipe</c> prefix (they must run
    /// before the game), Salvage's <c>UpdateRecipe</c> postfix (after Epic Loot's), and the shared-chest timeout,
    /// touch and ground pickup patches.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    public static class PlayerUpdatePatch
    {
        [HarmonyPostfix]
        public static void Postfix(Player __instance)
        {
            if (__instance != Player.m_localPlayer)
                return;
            BuildCamera.CameraToggle.Tick(__instance);
            Homestead.TorchKeyPatch.Tick(__instance);
            Reach.ReachKeys.Tick(__instance);
            Stow.DumpKeyPatch.Tick(__instance);
        }
    }

    /// <summary>After the inventory's update: the recipe search keys, the Salvage Key, the inventory hotkeys.</summary>
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Update))]
    public static class InventoryUpdatePatch
    {
        [HarmonyPostfix]
        public static void Postfix(InventoryGui __instance)
        {
            Recipes.RecipeInput.Poll(__instance);
            Salvage.SalvageHotkey.Tick(__instance);
            Stow.StowHotkeys.Tick(__instance);
        }
    }

    /// <summary>After the crafting panel's per-frame recipe update: the batch stepper, then the track and favourite buttons.</summary>
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipe))]
    public static class UpdateRecipePatch
    {
        [HarmonyPostfix]
        public static void Postfix(InventoryGui __instance, Player player)
        {
            Batch.BatchStepper.Refresh(__instance, player);
            Recipes.RecipeActions.Refresh(__instance);
        }
    }

    /// <summary>A container's once-a-second <c>CheckForChanges</c>, on every peer: its sign, then Auto Tidy's schedule.</summary>
    [HarmonyPatch(typeof(Container), nameof(Container.CheckForChanges))]
    public static class ContainerTickPatch
    {
        [HarmonyPrefix]
        public static void Prefix(Container __instance, out uint __state) => __state = __instance.m_lastRevision;

        [HarmonyPostfix]
        public static void Postfix(Container __instance, uint __state)
        {
            Signs.SignRefresh.Tick(__instance, false);
            Stow.TidySchedule.Tick(__instance, __state);
        }
    }
}
