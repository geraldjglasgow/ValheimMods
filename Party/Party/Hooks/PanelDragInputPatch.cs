using HarmonyLib;
using Party.UI;

namespace Party.Hooks
{
    /// <summary>
    /// Freeing the cursor isn't enough to drag the panel: the camera controller keeps reading mouse movement as
    /// look input unless the game already treats a menu as open (<c>PlayerController.InInventoryEtc</c> - the same
    /// check that lets you move the mouse freely over the inventory without the camera spinning). Extending it for
    /// edit mode gives the panel the same free-mouse behavior as a vanilla menu.
    /// </summary>
    [HarmonyPatch(typeof(PlayerController), "InInventoryEtc")]
    public static class PanelDragInputPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref bool __result)
        {
            if (HealthPanel.EditMode)
                __result = true;
        }
    }
}
