using HarmonyLib;
using OpenKeep.Core;

namespace OpenKeep.Carts
{
    /// <summary>Tells the player about Shift + Use on a cart that carries a station.</summary>
    [HarmonyPatch(typeof(Vagon), nameof(Vagon.GetHoverText))]
    public static class CartHoverPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Vagon __instance, ref string __result)
        {
            if (!CartsSettings.CartWorkbench.Value || __instance.GetComponent<CraftingStation>() == null)
                return;
            __result += Language.Localize("\n[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] $ok_cartcraft");
        }
    }
}
