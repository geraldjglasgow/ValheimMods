using HarmonyLib;

namespace OpenKeep.Carts
{
    /// <summary>
    /// Plain Use on a cart attaches it as in the game; the game ignores the alt flag for carts, so Shift + Use
    /// (the game's alt use) opens the cart's station instead, the way Use opens a workbench.
    /// </summary>
    [HarmonyPatch(typeof(Vagon), nameof(Vagon.Interact))]
    public static class CartInteractPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Vagon __instance, Humanoid character, bool hold, bool alt, ref bool __result)
        {
            if (hold || !alt || !CartsSettings.CartWorkbench.Value)
                return true;
            CraftingStation station = __instance.GetComponent<CraftingStation>();
            if (station == null)
                return true;
            __result = station.Interact(character, false, false);
            return false;
        }
    }
}
