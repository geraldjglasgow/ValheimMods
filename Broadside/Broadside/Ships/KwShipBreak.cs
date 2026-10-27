using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Breaking up a launched ship with the hammer's remove (the user's request, 2026-10-26: "You should be allowed to break
    /// ships with the middle mouse button (the hammer's remove). All resources go in floating crates on the water."). The
    /// game's own removal does it, with its own checks (the ward, the karve's workbench in range); only, with anyone aboard,
    /// the remover included, the click is refused here first with clear words (<see cref="KwShipBreakRules"/>), where the
    /// game would say only that it can't be removed now. The game then asks the ship's owner to remove it
    /// (<see cref="KwShipBreakOwner"/>). Ships on the dock are pieces, not this ship, and are removed as before.
    /// </summary>
    [HarmonyPatch(typeof(Player), "RemovePiece")]
    public static class KwShipBreak
    {
        // after the other prefixes, the build camera's reach among them
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Low)]
        private static bool Prefix(Player __instance, ref bool __result)
        {
            // a part of the ship removed (or a near miss refused) by the sea edits' prefix this frame: theirs (KwSeaRemove)
            if (KwSeaRemove.Taken == UnityEngine.Time.frameCount || !Guard.Run("Broadside ship break", Refused, __instance))
                return true;
            __result = false;
            return false;
        }

        // A launched ship aimed at with someone aboard: the player told, and nothing removed.
        private static bool Refused(Player player)
        {
            Piece? piece = player == Player.m_localPlayer ? Aim(player) : null;
            KwShipBody? body = piece != null ? piece.GetComponent<KwShipBody>() : null;
            Ship? ship = body != null ? body.GetComponent<Ship>() : null;
            if (ship == null || KwShipBreakRules.Why(player, piece!, ship, body!.Hull) != KwShipBreakRules.Everyone)
                return false;
            player.Message(MessageHud.MessageType.Center, KwShipBreakRules.Everyone);
            return true;
        }

        // The piece the game's removal aims at: the same ray, mask and reach.
        private static Piece? Aim(Player player)
        {
            if (GameCamera.instance == null)
                return null;
            Transform eye = GameCamera.instance.transform;
            if (!Physics.Raycast(eye.position, eye.forward, out RaycastHit hit, 50f, player.m_removeRayMask)
                || Vector3.Distance(hit.point, player.m_eye.position) >= player.m_maxPlaceDistance)
                return null;
            return hit.collider.GetComponentInParent<Piece>();
        }
    }

    /// <summary>
    /// The ship's owner removing a launched ship as the game asked it (<see cref="KwShipBreak"/>; the game's removal, not a
    /// wreck nor a removal that drops nothing): asked again whether anyone is aboard as the owner sees it (then the ship
    /// stays and the remover is told), else its cost and hung shields go into floating crates along it
    /// (<see cref="KwShipBreakSpill"/>) and the wood breaks along it (<see cref="KwShipBreakLook"/>), and the game destroys
    /// it, its ZDO with it, so it is gone for everyone.
    /// </summary>
    [HarmonyPatch(typeof(WearNTear), "RPC_Remove")]
    public static class KwShipBreakOwner
    {
        [HarmonyPrefix]
        private static bool Prefix(WearNTear __instance, long sender, bool blockDrop)
        {
            KwShipBody? body = blockDrop ? null : __instance.GetComponent<KwShipBody>();
            return body == null || Guard.Run("Broadside ship break", Break, body, sender);
        }

        // True when the game goes on to destroy the ship.
        private static bool Break(KwShipBody body, long sender)
        {
            ZNetView view = body.GetComponent<ZNetView>();
            Ship ship = body.GetComponent<Ship>();
            if (view == null || !view.IsValid() || !view.IsOwner() || ship == null)
                return true;
            if (KwShipBreakRules.Aboard(ship, body.Hull))
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(sender, "ShowMessage", (int)MessageHud.MessageType.Center, KwShipBreakRules.Everyone);
                return false;
            }
            KwShipBody.Step("Broadside ship crates", () => Log.Info($"A ship broken up into {KwShipBreakSpill.Spill(body)} crates"));
            if (body.Hull is KwBpHull hull)
                KwShipBody.Step("Broadside ship breaking", () => KwShipBreakLook.Play(body.transform, hull));
            return true;
        }
    }
}
