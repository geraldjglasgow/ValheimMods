using System;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The game's third-person camera on a launched Broadside ship (the game's <c>GetCameraPosition</c>, which places it
    /// for a player on foot or at a helm). The game's camera passes through the game's own ships. It once stopped at every
    /// box of ours (the vehicle layer added to its block mask), and at the rudder's post and the hatches (on the piece
    /// layer, which the game's mask holds), so a zoomed-out view snapped in to the player in one frame whenever a transom,
    /// a cabin wall, a deck edge or the rudder came between. In the open (on deck, on a roof, aloft, or steering) the camera
    /// now passes through the whole ship as it does through the game's ships: the ship's own colliders on the game's
    /// blocking layers are off them while the game places it (<see cref="KwShipCameraSwap"/>), and a camera that would come
    /// to rest shut in the ship (in a hold, between decks, in a cabin, the ship between it and the player) is raised
    /// straight up onto the deck or roof over it, as the game raises it out of the sea (<see cref="KwShipCameraCover"/>).
    /// Nothing on the ship pulls it in toward the player. With his eyes under a deck or a roof, or under a hatch in one
    /// (<see cref="KwShipCameraCover.Within"/>), and not steering, the hull stops it as before, as a room ashore does.
    /// The lowest it may stand this frame (<see cref="Floor"/>) keeps the steadied view (<see cref="KwShipSteady"/>) out of
    /// the ship and the sea. Local only.
    /// </summary>
    [HarmonyPatch(typeof(GameCamera), "GetCameraPosition")]
    public static class KwShipCamera
    {
        private const string Context = "Broadside ship camera";
        private static KwShipBody? ship;
        private static bool open, changed;
        private static int mask, floorFrame = -1;
        private static float floor;
        private static Vector3 eyes;

        /// <summary>The lowest the camera may stand this frame (world height); minus infinity off our ships.</summary>
        public static float Floor => floorFrame == Time.frameCount ? floor : float.NegativeInfinity;

        /// <summary>The Broadside ship a player stands on or sits in (a helm, a bench), or null.</summary>
        public static KwShipBody? Aboard(Player player)
        {
            Ship? on = player.GetStandingOnShip();
            if (on == null && player.IsAttached() && player.m_attachPoint != null)
                on = player.m_attachPoint.GetComponentInParent<Ship>();
            return on != null ? on.GetComponent<KwShipBody>() : null;
        }

        [HarmonyPrefix]
        private static void Prefix(GameCamera __instance)
        {
            try
            {
                Before(__instance);
            }
            catch (Exception e)
            {
                Guard.Report(e, Context);
            }
        }

        [HarmonyPostfix]
        private static void Postfix(GameCamera __instance, ref Vector3 pos, ref Quaternion rot)
        {
            try
            {
                pos = After(__instance, pos, rot);
            }
            catch (Exception e)
            {
                Guard.Report(e, Context);
            }
        }

        // The game's mask and the ship's fittings put back, whatever happened in between.
        [HarmonyFinalizer]
        private static void Finalizer(GameCamera __instance) => Restore(__instance);

        // On our ship: in the open its fittings leave the game's blocking layers; inside, its boxes join them.
        private static void Before(GameCamera camera)
        {
            Player? player = Player.m_localPlayer;
            ship = player != null && !KwBpCamera.Out ? Aboard(player) : null;
            if (player == null || ship == null)
                return;
            (mask, changed, eyes) = (camera.m_blockCameraMask.value, true, player.m_eye.position);
            open = player.GetControlledShip() != null || !KwShipCameraCover.Within(ship, eyes);
            if (open)
                KwShipCameraSwap.Off(ship, mask);
            else
                camera.m_blockCameraMask = mask | KwShipCameraCover.Vehicle;
        }

        private static void Restore(GameCamera camera)
        {
            if (!changed)
                return;
            changed = false;
            camera.m_blockCameraMask = mask;
            KwShipCameraSwap.On();
        }

        // The game's placing done: the fittings back, in the open the camera raised out of the ship and the near plane
        // brought in by its hull, and the floor for this frame (never over where the camera stands).
        private static Vector3 After(GameCamera camera, Vector3 pos, Quaternion rot)
        {
            Restore(camera);
            if (ship == null)
                return pos;
            float lifted = float.NegativeInfinity;
            if (open)
            {
                lifted = KwShipCameraCover.Lift(ship, ref pos, eyes, camera.m_minWaterDistance);
                NearPlane(camera, pos, rot);
            }
            float sea = Floating.GetLiquidLevel(pos) + camera.m_minWaterDistance;
            floor = Mathf.Min(pos.y, Mathf.Max(lifted, Mathf.Max(sea, KwShipCameraCover.Ground(ship, pos))));
            floorFrame = Time.frameCount;
            return pos;
        }

        // Close by the ship's boxes the near plane comes in, as the game brings it in by a wall, so the hull is not cut open.
        private static void NearPlane(GameCamera camera, Vector3 pos, Quaternion rot)
        {
            float reach = camera.m_nearClipPlaneMax;
            Vector3 ahead = pos + rot * Vector3.forward * reach;
            if (camera.m_camera != null && Physics.CheckSphere(ahead, reach, KwShipCameraCover.Mask, QueryTriggerInteraction.Ignore))
                camera.m_camera.nearClipPlane = camera.m_nearClipPlaneMin;
        }
    }
}
