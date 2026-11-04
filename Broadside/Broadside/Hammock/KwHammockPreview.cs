using HarmonyLib;
using PatchGuard;

namespace Broadside
{
    /// <summary>
    /// Every frame the hammock is in the local player's hammer, after the game places its ghost: the game's ghost and marker
    /// hidden, and what a click would do shown see-through (<see cref="KwHammockView"/>): with no first end, the end the aim
    /// would set (a post, or a hook on the surface aimed at); with one, the whole hammock from it to the aim, red with the
    /// reason while it may not be hung (the plan made again only when either end moves a centimetre), or the first end alone
    /// while the aim gives no second. The build panel's description says the same (<see cref="KwHammockWords.Live"/>); the
    /// recipe and workbench are the game's own lines. The hammock put away drops a first end, hides the preview and gives
    /// the hammer its description back. Local to this player.
    /// </summary>
    [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
    public static class KwHammockPreview
    {
        private static string asked = "";
        private static KwHammockPlan? planned;
        private static Piece? shown;

        [HarmonyPostfix]
        private static void Postfix(Player __instance, bool flashGuardStone)
        {
            if (__instance == Player.m_localPlayer && !flashGuardStone)
                Guard.Run("Broadside hammock preview", Refresh, __instance);
        }

        private static void Refresh(Player player)
        {
            Piece? hammock = KwHammockTool.Held(player);
            if (hammock == null)
            {
                if (shown != null)
                    Away();
                return;
            }
            shown = hammock;
            Unghost(player);
            if (KwHammockTool.Pending != null && KwHammockTool.Pending.Lost)
                KwHammockTool.Cancel(KwHammockWords.ShipGone);
            KwHammockClick? first = KwHammockTool.Pending, aim = KwHammockAim.Click(player);
            KwHammockPlan? plan = first != null && aim != null && aim.Why == null ? Plan(first, aim) : null;
            Show(first, aim, plan);
            string text = KwHammockWords.Live(first, aim, plan);
            if (hammock.m_description != text)
                hammock.m_description = text;
        }

        // The hammock out of hand: no first end, no preview, its description as the hammer lists it.
        private static void Away()
        {
            if (KwHammockTool.Pending != null)
                KwHammockTool.Cancel(null);
            KwHammockView.Hide();
            (asked, planned) = ("", null);
            if (shown != null)
                shown.m_description = KwHammockWords.Description;
            shown = null;
        }

        // The game's ghost of the hammock and its marker hidden: the preview stands for them.
        private static void Unghost(Player player)
        {
            if (player.m_placementGhost != null && player.m_placementGhost.activeSelf)
                player.m_placementGhost.SetActive(false);
            if (player.m_placementMarkerInstance != null && player.m_placementMarkerInstance.activeSelf)
                player.m_placementMarkerInstance.SetActive(false);
        }

        // The hammock the next click would hang, made again only when either end changed.
        private static KwHammockPlan Plan(KwHammockClick first, KwHammockClick aim)
        {
            string key = first.Key + "#" + aim.Key;
            if (key != asked || planned == null)
                (asked, planned) = (key, KwHammockPlan.Make(first, aim, false));
            return planned;
        }

        // The whole hammock planned (red while refused), else the first end alone, else the end the aim would set.
        private static void Show(KwHammockClick? first, KwHammockClick? aim, KwHammockPlan? plan)
        {
            if (plan != null && plan.Spec != null)
                KwHammockView.Whole(plan);
            else if (first != null)
                KwHammockView.End(first);
            else if (aim != null && aim.Why == null)
                KwHammockView.End(aim);
            else
                KwHammockView.Hide();
        }
    }
}
