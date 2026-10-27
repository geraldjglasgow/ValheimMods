using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The boarding ladder's climb made safe on a launched Broadside ship. The game's ladder (<c>Ladder.Interact</c>) puts
    /// the player on its target at once, and the target is planned from the blueprint alone (<see cref="KwShipLadderFind"/>,
    /// for a body thinner than the game's player), so the hull's collider boxes, which stand inside the drawn planking on a
    /// full-bodied hull, or a piece built aboard since (a cannon, a chest), could fill the spot; the game then pushed the
    /// player back out over the side (the user, 2026-10-26: "I keep getting teleported back down"). After the climb the
    /// player's own capsule is tried there against the ship's solid colliders and, when it overlaps one, moved inboard
    /// (toward the ship's middle line) and along in small steps to the nearest clear place standing on the ship, and put
    /// down there as every climb puts the player (<see cref="KwClimbRoom.Place"/>). Local to the climbing player.
    /// </summary>
    [HarmonyPatch(typeof(Ladder), nameof(Ladder.Interact))]
    public static class KwShipLadderLanding
    {
        // How far inboard and how far along a landing is looked for, in steps of Step; how far down for the floor.
        private const float Inward = 2.4f, Step = 0.15f, Floor = 0.9f;
        private static readonly float[] Along = { 0f, 0.3f, -0.3f, 0.6f, -0.6f };

        [HarmonyPostfix]
        private static void Postfix(Ladder __instance, Humanoid character)
        {
            if (character is Player player && player == Player.m_localPlayer && __instance.m_targetPos != null)
                Guard.Run("Broadside boarding ladder", () => Land(__instance, player));
        }

        private static void Land(Ladder ladder, Player player)
        {
            KwShipBody? ship = ladder.GetComponentInParent<KwShipBody>();
            if (ship == null || (player.transform.position - ladder.m_targetPos.position).sqrMagnitude > 0.01f)
                return;
            Vector3? clear = Clear(ship.transform, player, player.transform.position);
            KwClimbRoom.Place(player, clear ?? player.transform.position);
        }

        // The landing itself when the player's capsule fits there, else the nearest clear spot inboard and along with the
        // ship's floor under it; null when none is found (the game's own landing stands).
        private static Vector3? Clear(Transform ship, Player player, Vector3 at)
        {
            Vector3 local = ship.InverseTransformPoint(at);
            Vector3 inward = ship.TransformDirection(new Vector3(-Mathf.Sign(local.x), 0f, 0f)), along = ship.forward;
            for (float into = 0f; into <= Inward + 0.001f; into += Step)
            {
                foreach (float shift in Along)
                {
                    Vector3? spot = Standing(ship, player, at + inward * into + along * shift);
                    if (spot != null && KwClimbRoom.Fits(player, spot.Value))
                        return spot;
                }
            }
            return null;
        }

        // The point dropped onto the ship's floor under it, about the landing's height (not on top of a cannon or chest), or null.
        private static Vector3? Standing(Transform ship, Player player, Vector3 at)
        {
            Vector3 from = at + Vector3.up * 0.5f;
            foreach (RaycastHit hit in Physics.RaycastAll(from, Vector3.down, Floor + 0.5f, player.m_placeRayMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.normal.y > 0.7f && hit.collider.transform.IsChildOf(ship) && hit.point.y > at.y - Floor && hit.point.y < at.y + 0.3f)
                    return hit.point + Vector3.up * 0.03f;
            }
            return null;
        }
    }
}
