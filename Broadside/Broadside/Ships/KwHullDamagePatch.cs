using HarmonyLib;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The game's wear on a launched ship joined to its holes. Damage the ship's wear takes at a point (<c>WearNTear.ApplyDamage</c>,
    /// on the ship's owner, after the game's own health has fallen) also holes the hull part it strikes, by the hit's point
    /// and way (<see cref="KwHullHoles.Take"/>); the game's damage to the whole ship (upside down, the Ashlands sea, a hard
    /// landing: all at the ship's own place) and sinking (no hit) take only health. A shot that bursts (a projectile's area
    /// hit, <c>Projectile.DoAOE</c>) gives the ship the point of the box nearest it on that box's world bounds, which on a
    /// ship of many boxes is a deck's slab, the burst carried up or down to that deck's height (2026-10-26: a cannon's shots
    /// along the side made flat holes in the decks' edges); so on the shooter's game, before the hit is sent to the ship's
    /// owner (<c>WearNTear.Damage</c>), a launched ship's hit takes the burst's own place and the shot's way
    /// (<see cref="KwHullBurst"/>; an area hit's, <c>Aoe.CheckHits</c>, its middle). A heavy shot's hit is sent as a shot
    /// instead (<see cref="KwShipShot"/>), and while the owner routes it (<see cref="KwShipHitRouter.Routing"/>) the wear
    /// makes no holes of its own. The hammer's repair of the ship mends its holes one at a time (<see cref="KwShipRepair"/>).
    /// </summary>
    [HarmonyPatch]
    public static class KwHullDamagePatch
    {
        [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.ApplyDamage)), HarmonyPostfix]
        private static void Damaged(WearNTear __instance, float damage, HitData? hitData, bool __result)
        {
            if (!__result || hitData == null || damage <= 0f || KwShipHitRouter.Routing || !__instance.TryGetComponent(out KwHullHoles holes))
                return;
            if (hitData.m_hitType == HitData.HitType.AshlandsOcean || (hitData.m_point - __instance.transform.position).sqrMagnitude < 0.0025f)
                return;
            Take(holes, hitData.m_point, hitData.m_dir, damage, __instance.m_health);
        }

        // Apart from the patch, so only a ship's hit makes the closure; a throw is logged and the hit goes on.
        private static void Take(KwHullHoles holes, Vector3 point, Vector3 way, float damage, float health) =>
            KwShipBody.Step("Broadside hull holes", () => holes.Take(point, way, damage, health));

        // A heavy shot's hit on a launched ship goes to its owner as a shot (KwShipShot; the game's hit not sent), any other
        // burst's hit is given the burst's own place.
        [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Damage)), HarmonyPrefix]
        private static bool Sent(WearNTear __instance, HitData hit)
        {
            if (KwShipShot.Send(__instance, hit))
                return false;
            KwHullBurst.Mend(__instance, hit);
            return true;
        }

        [HarmonyPatch(typeof(Projectile), "DoAOE"), HarmonyPrefix]
        private static void Bursting(Projectile __instance, Vector3 hitPoint) => KwHullBurst.Begin(hitPoint, __instance.m_vel, __instance.transform.forward);

        [HarmonyPatch(typeof(Projectile), "DoAOE"), HarmonyFinalizer]
        private static void Burst() => KwHullBurst.End();

        // An area hit's burst (a bomb's, a cannonball's: Aoe) gives each thing the place of that thing's own collider; a
        // launched ship's hit takes the burst's middle instead, its way not known.
        [HarmonyPatch(typeof(Aoe), "CheckHits"), HarmonyPrefix]
        private static void AreaBursting(Aoe __instance) => KwHullBurst.Begin(__instance.transform.position, Vector3.zero, Vector3.zero);

        [HarmonyPatch(typeof(Aoe), "CheckHits"), HarmonyFinalizer]
        private static void AreaBurst() => KwHullBurst.End();
    }

    /// <summary>
    /// The burst a projectile is making (<c>Projectile.DoAOE</c>, on the shooter's game), while it makes it: where it burst
    /// and which way the shot flew. A launched ship it hits is told those (<see cref="Mend"/>) instead of the game's point
    /// on the nearest box's world bounds and its level way from the burst to it.
    /// </summary>
    public static class KwHullBurst
    {
        private static bool bursting;
        private static Vector3 centre, way;

        public static void Begin(Vector3 at, Vector3 velocity, Vector3 forward)
        {
            (bursting, centre) = (true, at);
            way = velocity.sqrMagnitude > 1e-4f ? velocity.normalized : forward;
        }

        public static void End() => bursting = false;

        /// <summary>A hit about to be sent to a launched ship, given the burst's place and the shot's way.</summary>
        public static void Mend(WearNTear wear, HitData? hit)
        {
            if (!bursting || hit == null || wear.GetComponent<KwHullHoles>() == null)
                return;
            (hit.m_point, hit.m_dir) = (centre, way);
        }
    }
}
