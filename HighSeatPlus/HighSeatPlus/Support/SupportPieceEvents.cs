using System;
using HarmonyLib;
using UnityEngine;

namespace HighSeatPlus
{
    /// <summary>
    /// WearNTear.Start (private, once after a piece is made, placed or loaded, before its first update): a new piece's
    /// colliders could join the cache of a ground-held piece beside it (<see cref="SupportSkip"/>), so the columns under
    /// them are raised and settle for <see cref="SettleSeconds"/>. Start rather than Awake: a piece moved right after it
    /// was made is counted where it ends up, and a record made in between (the piece is solid from Awake) is dropped by
    /// the raise. Only pieces the network knows (the build ghost's ZNetView is gone).
    /// </summary>
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Start))]
    internal static class SupportPieceAppears
    {
        public const float SettleSeconds = 0.5f;

        private static void Postfix(WearNTear __instance)
        {
            try
            {
                Raise(__instance);
            }
            catch (Exception e)
            {
                SupportSkip.Fail(e);
            }
        }

        public static void Raise(WearNTear piece)
        {
            if (piece.m_nview != null && piece.m_nview.GetZDO() != null && SupportBounds.Of(piece.gameObject, out Bounds bounds))
            {
                SupportCells.Changed(bounds, SettleSeconds);
            }
        }
    }

    /// <summary>
    /// WearNTear.RPC_HealthChanged (private, on every peer when a piece is damaged or repaired): the game clears the
    /// piece's own cache there (seen by <see cref="SupportCacheCleared"/>), and when the health crosses 75 % or 25 %
    /// SetHealthVisual switches its new, worn or broken look, which can bring in colliders of its own: then the columns
    /// under it are raised as for a new piece. A hit that stays within one look raises nothing.
    /// </summary>
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.RPC_HealthChanged))]
    internal static class SupportPieceLook
    {
        private static void Prefix(WearNTear __instance, out int __state) => __state = Look(__instance.m_healthPercentage);

        private static void Postfix(WearNTear __instance, int __state)
        {
            try
            {
                bool hasLooks = __instance.m_new != null || __instance.m_worn != null || __instance.m_broken != null;
                if (hasLooks && Look(__instance.m_healthPercentage) != __state)
                {
                    SupportPieceAppears.Raise(__instance);
                }
            }
            catch (Exception e)
            {
                SupportSkip.Fail(e);
            }
        }

        // The look SetHealthVisual shows for a share of health: new above 75 %, worn above 25 %, broken below.
        private static int Look(float health) => health > 0.75f ? 2 : health > 0.25f ? 1 : 0;
    }

    /// <summary>
    /// WearNTear.ClearCachedSupport (private): the game empties a piece's cache from inside its check, and from outside
    /// it when a neighbour is placed (directly or through RPC_ClearCachedSupport to the owner), on damage or repair
    /// (RPC_HealthChanged) and after a terrain edit (the heightmap's clear event). The next check must then be the
    /// game's, so the piece's record is dropped; from inside the check a new one is made right after.
    /// </summary>
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.ClearCachedSupport))]
    internal static class SupportCacheCleared
    {
        private static void Postfix(WearNTear __instance)
        {
            try
            {
                SupportSkip.Drop(__instance);
            }
            catch (Exception e)
            {
                SupportSkip.Fail(e);
            }
        }
    }

    /// <summary>WearNTear.OnDestroy (private): the piece's record goes with it, so no record outlives its piece.</summary>
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.OnDestroy))]
    internal static class SupportPieceGone
    {
        private static void Prefix(WearNTear __instance)
        {
            try
            {
                SupportSkip.Forget(__instance);
            }
            catch (Exception e)
            {
                SupportSkip.Fail(e);
            }
        }
    }
}
