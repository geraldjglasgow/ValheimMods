using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace OpenKeep.Blueprints
{
    /// <summary>BuildUi.Awake postfix (private): the build menu gets its Blueprints tab.</summary>
    [HarmonyPatch(typeof(BuildUi), nameof(BuildUi.Awake))]
    public static class BlueprintTabInstallPatch
    {
        [HarmonyPostfix]
        public static void Postfix(BuildUi __instance)
        {
            BlueprintSafe.Run("OpenKeep blueprints tab", () => BlueprintTab.Install(__instance));
        }
    }

    /// <summary>
    /// BuildUi.OpenBuildMenu prefix and postfix: the tab shows or hides before the menu picks its list (so a menu reopened
    /// after blueprints went off never lands on the hidden tab) and again once the menu knows its build tool.
    /// </summary>
    [HarmonyPatch(typeof(BuildUi), nameof(BuildUi.OpenBuildMenu))]
    public static class BlueprintTabOpenPatch
    {
        [HarmonyPrefix]
        public static void Prefix() => BlueprintSafe.Run("OpenKeep blueprints tab", BlueprintTab.Tick);

        [HarmonyPostfix]
        public static void Postfix()
        {
            BlueprintSafe.Run("OpenKeep blueprints tab", BlueprintTab.Opened);
            BlueprintSafe.Run("OpenKeep blueprints tab", BlueprintTab.Tick);
        }
    }

    /// <summary>
    /// BuildUi.UpdateTagButtons postfix (private): whenever the game redraws its tag column (another tab, a refill), the
    /// folder panel and the breadcrumb follow at once.
    /// </summary>
    [HarmonyPatch(typeof(BuildUi), nameof(BuildUi.UpdateTagButtons))]
    public static class BlueprintTagColumnPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            BlueprintSafe.Run("OpenKeep blueprint folders", Tab.FolderPanel.Sync);
            BlueprintSafe.Run("OpenKeep blueprint breadcrumb", Tab.Breadcrumb.Sync);
        }
    }

    /// <summary>
    /// TabHandler.Update prefix (private): while a name box is up over the build menu, its tab keys (Q / E) are typing,
    /// not tab changes. Only the build menu's own tab handler is held.
    /// </summary>
    [HarmonyPatch(typeof(TabHandler), nameof(TabHandler.Update))]
    public static class BlueprintTabKeysPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(TabHandler __instance)
        {
            BuildUi menu = BlueprintTab.Menu;
            return !(NamePrompt.Showing && menu != null && __instance == menu.m_tabHandler);
        }
    }

    /// <summary>
    /// BuildUi.OnSelectPiece prefix: Ctrl or Shift + click on a blueprint changes the tab's picks instead of selecting it,
    /// and the menu stays open (<see cref="Tab.TabClicks"/>); nothing while a name box is up.
    /// </summary>
    [HarmonyPatch(typeof(BuildUi), nameof(BuildUi.OnSelectPiece))]
    public static class BlueprintPickClickPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Piece piece)
        {
            if (NamePrompt.Showing)
                return false;
            return BlueprintSafe.Call("OpenKeep blueprint folder", () => Tab.TabClicks.BeforeSelect(piece), true);
        }
    }

    /// <summary>
    /// BuildUi.Update prefix (private): while a name box is up over the menu the menu does nothing (F would focus its
    /// search field, Esc and the Build Menu key would close it, gamepad buttons would act); the box shows the frame its
    /// Esc or Enter closes it too, so that key never reaches the menu. Otherwise a right click on a blueprint of
    /// the shown tab, or on a folder of the folder panel or the breadcrumb, renames it, and the rest of that frame of the menu is skipped.
    /// </summary>
    [HarmonyPatch(typeof(BuildUi), nameof(BuildUi.Update))]
    public static class BlueprintRightClickPatch
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            if (NamePrompt.Showing)
                return false;
            return BlueprintSafe.Call("OpenKeep blueprint right click", () => !Tab.TabClicks.RightClicked(), true);
        }
    }

    /// <summary>
    /// BuildUiPieceButton.Setup postfix: every time the menu sets a button up for a piece, OpenKeep's name band, pick mark
    /// and drag handle follow that piece (<see cref="Tab.TabButtons"/>).
    /// </summary>
    [HarmonyPatch(typeof(BuildUiPieceButton), nameof(BuildUiPieceButton.Setup))]
    public static class BlueprintButtonSetupPatch
    {
        [HarmonyPostfix]
        public static void Postfix(BuildUiPieceButton __instance)
        {
            BlueprintSafe.Run("OpenKeep blueprint button", () => Tab.TabButtons.Decorate(__instance));
        }
    }

    /// <summary>BuildUi.PressedFavoriteButton prefix: the Blueprints tab's entries are never made favourites (middle click).</summary>
    [HarmonyPatch(typeof(BuildUi), nameof(BuildUi.PressedFavoriteButton))]
    public static class BlueprintNoFavouritePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(BuildUiPieceButton btn)
        {
            return btn == null || !BlueprintMenu.IsOurs(btn.Piece);
        }
    }

    /// <summary>
    /// Postfix on the game's four piece lists (By Usage, By Material, Recent, Favorites): the Blueprints tab's entries
    /// show only in their own tab. Left alone when the tab could not be made, so the entries are still reachable.
    /// </summary>
    [HarmonyPatch]
    public static class BlueprintListFilterPatch
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(ByUsagePieceList), nameof(ByUsagePieceList.GetAvailablePiecesWithTag));
            yield return AccessTools.Method(typeof(ByMaterialPieceList), nameof(ByMaterialPieceList.GetAvailablePiecesWithTag));
            yield return AccessTools.Method(typeof(RecentPieceList), nameof(RecentPieceList.GetAvailablePiecesWithTag));
            yield return AccessTools.Method(typeof(FavoritePieceList), nameof(FavoritePieceList.GetAvailablePiecesWithTag));
        }

        [HarmonyPostfix]
        public static void Postfix(IList<Piece> resultOut)
        {
            if (!BlueprintTab.Installed)
                return;
            for (int i = resultOut.Count - 1; i >= 0; i--)
            {
                if (BlueprintMenu.IsOurs(resultOut[i]))
                    resultOut.RemoveAt(i);
            }
        }
    }
}
