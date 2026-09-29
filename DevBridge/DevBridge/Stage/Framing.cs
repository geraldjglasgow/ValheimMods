using System.Collections.Generic;
using DevBridge.Server;
using UnityEngine;

namespace DevBridge.Stage
{
    /// <summary>
    /// Points the view at the stage for screenshots: the game's own free-fly camera (the `freefly` console command's)
    /// set to an exact place and direction, or the player moved and turned so the normal camera frames it; and the HUD.
    /// </summary>
    internal static class Framing
    {
        private static bool hudHiddenHere;

        internal static GameCamera Camera => GameCamera.instance ? GameCamera.instance : throw new BridgeException("no game camera (not in a world?)");

        /// <summary>
        /// The view direction: towards the side the row faces (or along the camera's current view when there is no row),
        /// turned yaw degrees round the target and tilted pitch degrees down.
        /// </summary>
        internal static Vector3 Look(float yaw, float pitch)
        {
            Vector3 along = Row.Started ? -Row.Front : Flat(Camera.transform.forward);
            float baseYaw = Mathf.Atan2(along.x, along.z) * Mathf.Rad2Deg;
            return Quaternion.Euler(pitch, baseYaw + yaw, 0f) * Vector3.forward;
        }

        /// <summary>How far back the camera must be for the box to fill the view with a small margin.</summary>
        internal static float Fit(Bounds box, Vector3 look, float fov, float aspect)
        {
            Vector3 right = Vector3.Cross(Vector3.up, look).normalized;
            Vector3 up = Vector3.Cross(look, right);
            float tanV = Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad), tanH = tanV * aspect;
            float fit = Mathf.Max(Across(box.extents, right) / tanH, Across(box.extents, up) / tanV);
            return Mathf.Max(0.5f, fit * 1.15f + Across(box.extents, look));
        }

        private static float Across(Vector3 extents, Vector3 axis) =>
            Mathf.Abs(axis.x) * extents.x + Mathf.Abs(axis.y) * extents.y + Mathf.Abs(axis.z) * extents.z;

        /// <summary>The free camera at the eye looking along look; the player stays where they are and takes no input.</summary>
        internal static void Free(Vector3 eye, Vector3 look, float fov)
        {
            GameCamera camera = Camera;
            camera.m_freeFly = true;
            (camera.m_freeFlyVel, camera.m_freeFlyAcc, camera.m_freeFlySavedVel, camera.m_freeFlyTurnVel) = (Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero);
            (camera.m_freeFlyTarget, camera.m_freeFlyLockon) = (null, null);
            Quaternion rotation = Quaternion.LookRotation(look);
            camera.m_freeFlyYaw = rotation.eulerAngles.y;
            camera.m_freeFlyPitch = Pitch(look);
            camera.m_freeFlyRef = Quaternion.identity;
            camera.transform.SetPositionAndRotation(eye, rotation);
            if (fov <= 0f) return;
            camera.m_camera.fieldOfView = fov;
            camera.m_skyCamera.fieldOfView = fov;
        }

        /// <summary>The player moved on the ground a little behind the free camera, out of its view.</summary>
        internal static void Park(Vector3 eye, Vector3 look)
        {
            Vector3 feet = eye - Flat(look) * 2f;
            feet.y = Row.Floor(feet);
            Move(feet, look);
        }

        /// <summary>
        /// The normal camera: the player stands where their camera will see the target along look, facing it; zoom sets
        /// the camera's distance behind them. Approximate: the game's camera keeps its own offset and collision.
        /// </summary>
        internal static void PlayerAt(Vector3 target, Vector3 look, float distance, float zoom)
        {
            GameCamera camera = Camera;
            camera.m_freeFly = false;
            if (zoom > 0f) camera.m_distance = Mathf.Clamp(zoom, camera.m_minDistance, camera.m_maxDistance);
            Vector3 feet = target - Flat(look) * Mathf.Max(1f, distance - camera.m_distance);
            feet.y = Row.Floor(feet);
            Move(feet, look);
        }

        private static void Move(Vector3 feet, Vector3 look)
        {
            Player player = Player.m_localPlayer ? Player.m_localPlayer : throw new BridgeException("no local player");
            Quaternion yaw = Quaternion.LookRotation(Flat(look));
            player.transform.SetPositionAndRotation(feet, yaw);
            if (player.m_body)
            {
                (player.m_body.position, player.m_body.rotation, player.m_body.linearVelocity) = (feet, yaw, Vector3.zero);
            }
            player.m_lookYaw = yaw;
            player.m_lookPitch = Pitch(look);
        }

        internal static void Release()
        {
            if (GameCamera.instance) GameCamera.instance.m_freeFly = false;
            if (hudHiddenHere) ShowHud(true);
        }

        internal static void ShowHud(bool visible)
        {
            if (!Hud.instance) return;
            Hud.instance.m_userHidden = !visible;
            hudHiddenHere = !visible;
        }

        internal static Dictionary<string, object> Describe()
        {
            GameCamera camera = Camera;
            return new Dictionary<string, object>
            {
                ["camera"] = camera.m_freeFly ? "free" : "player",
                ["position"] = Fmt.V3(camera.transform.position),
                ["look"] = Fmt.V3(camera.transform.forward),
                ["fov"] = Fmt.R(camera.m_camera.fieldOfView),
                ["hud"] = Hud.instance ? !Hud.instance.m_userHidden : (bool?)null,
                ["player"] = Player.m_localPlayer ? Fmt.V3(Player.m_localPlayer.transform.position) : null,
            };
        }

        private static Vector3 Flat(Vector3 direction)
        {
            Vector3 flat = Vector3.ProjectOnPlane(direction, Vector3.up);
            return flat.sqrMagnitude < 0.0001f ? Vector3.forward : flat.normalized;
        }

        private static float Pitch(Vector3 look) => -Mathf.Asin(Mathf.Clamp(look.normalized.y, -1f, 1f)) * Mathf.Rad2Deg;
    }
}
