using System;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Hovering a piece that rides a launched ship (<see cref="KwShipRider"/>): its colliders are part of the ship's body,
    /// and the game's hover (<c>Player.FindHoverObject</c>) takes a hit on a collider of a body, unless that collider's own
    /// object can be hovered, as the body itself, so a chest built on a ship hovered as the ship and E opened nothing.
    /// When the game has picked a launched ship that way, the same ray is cast again: a first hit on a rider is that
    /// rider's collider, as the game takes a collider of a piece that has no body. Only for the local player, and only
    /// when the hover is a launched ship: one ray then, none otherwise.
    /// </summary>
    [HarmonyPatch(typeof(Player), "FindHoverObject")]
    public static class KwShipRiderHover
    {
        private static readonly RaycastHit[] hits = new RaycastHit[32];

        [HarmonyPostfix]
        private static void Postfix(Player __instance, ref GameObject hover)
        {
            if (hover == null || KwShipRiders.Loaded.Count == 0 || !hover.TryGetComponent(out KwShipRiders _))
                return;
            try
            {
                Collider? rider = First(__instance);
                if (rider != null && rider.GetComponentInParent<KwShipRider>() != null)
                    hover = rider.gameObject;
            }
            catch (Exception e)
            {
                Guard.Report(e, "Broadside ship rider hover");
            }
        }

        // The game's hover ray's first hit that is not the player's own body.
        private static Collider? First(Player player)
        {
            if (GameCamera.instance == null)
                return null;
            Transform eye = GameCamera.instance.transform;
            int count = Physics.RaycastNonAlloc(eye.position, eye.forward, hits, 50f, player.m_interactMask);
            Collider? best = null;
            float nearest = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                Collider collider = hits[i].collider;
                Rigidbody? body = collider != null ? collider.attachedRigidbody : null;
                if (collider == null || hits[i].distance >= nearest || body != null && body.gameObject == player.gameObject)
                    continue;
                (best, nearest) = (collider, hits[i].distance);
            }
            return best;
        }
    }
}
