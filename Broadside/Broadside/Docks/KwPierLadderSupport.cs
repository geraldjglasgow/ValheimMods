using HarmonyLib;
using PatchGuard;

namespace Broadside
{
    /// <summary>
    /// What a dock holds up (WearNTear.UpdateSupport prefix, on the piece's owner, where the game checks support; the one
    /// patch on that method). The game's own check measures the support a piece finds by the distance between two pieces'
    /// centres, and a dock section's is at its start, up to 12 m away: a piece far along a section found too little and
    /// broke. So: a dock ladder hanging from a dock section, or a dock lantern standing beside one, is held up by that section
    /// (<see cref="KwPierLadder.Held"/>, <see cref="KwDockLanternPiece.Held"/>, also while a section near it is still loading)
    /// at its material's full support, and with none it has none (the game then breaks it, dropping what it cost); and any
    /// other piece resting on a section's deck (<see cref="KwPierRest"/>: its boxes meet the deck's) has its material's full
    /// support, as on the ground, wherever along the section it stands. Written to the ZDO as the game writes it. A ladder or
    /// lantern beside a game floor, a dock section itself and every other piece keep the game's own check untouched.
    /// </summary>
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.UpdateSupport))]
    public static class KwPierLadderSupport
    {
        [HarmonyPrefix]
        private static bool Prefix(WearNTear __instance) =>
            Guard.Run<WearNTear, bool>("Broadside dock support", static w => !Hold(w), __instance);

        // Whether the dock decided this piece's support (false: the game's own check).
        private static bool Hold(WearNTear wear)
        {
            KwPierLadder? ladder = KwPierLadder.Of(wear);
            KwDockLanternPiece? lantern = ladder == null ? KwDockLanternPiece.Of(wear) : null;
            if (ladder != null && ladder.Spec != null && ladder.Spec.OnDock)
                Set(wear, ladder.Held());
            else if (lantern != null && lantern.Spec != null && lantern.Spec.OnDock)
                Set(wear, lantern.Held());
            else if (ladder == null && lantern == null && KwPierRest.On(wear))
                Set(wear, true);
            else
                return false;
            return true;
        }

        private static void Set(WearNTear wear, bool held)
        {
            float support = held ? wear.GetMaxSupport() : 0f;
            if (wear.m_support.Equals(support))
                return;
            wear.m_support = support;
            wear.m_nview.GetZDO()?.Set(ZDOVars.s_support, support);
        }
    }
}
