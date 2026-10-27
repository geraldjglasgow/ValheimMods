using HarmonyLib;
using UnityEngine;

namespace DevBridge.Studio
{
    /// <summary>
    /// The studio's Free camera: the camera let go from the player where it is and flown like the game's fly mode. It
    /// moves where it looks (forward while looking down goes down), at the player's fly speed (5 m/s, 12 m/s with Run
    /// held), Jump up and Crouch down, the mouse turning it. The game's free-fly flag stays on, so the player takes no
    /// input and other mods see a free camera, but the camera is placed here after the game and every mod have placed it
    /// (GameCamera.LateUpdate, last), so neither the game's own free-fly speed and smoothing nor a mod's camera changes (a
    /// ship's steadied view) reach it. The flag going off, or /frame taking the camera, lets it go.
    /// </summary>
    internal static class FlyCamera
    {
        private const float SlowSpeed = 5f, FastSpeed = 12f, MostPitch = 89f;

        private static bool flying;
        private static Vector3 position;
        private static float yaw, pitch;

        internal static void Start(GameCamera camera)
        {
            camera.m_freeFly = true;
            (camera.m_freeFlyTarget, camera.m_freeFlyLockon) = (null, null);
            Transform at = camera.transform;
            (position, yaw, pitch, flying) = (at.position, at.eulerAngles.y, Mathf.Clamp(Mathf.DeltaAngle(0f, at.eulerAngles.x), -MostPitch, MostPitch), true);
        }

        internal static void Stop(GameCamera camera)
        {
            flying = false;
            camera.m_freeFly = false;
        }

        /// <summary>Another hand (/frame) takes the free camera: this lets go and leaves the free-fly flag to it.</summary>
        internal static void Yield() => flying = false;

        /// <summary>Each frame after the game has placed the camera: steers and places it.</summary>
        internal static void Fly(GameCamera camera)
        {
            if (!flying) return;
            if (!camera.m_freeFly) { flying = false; return; }
            if (!Typing()) Steer(Time.unscaledDeltaTime);
            camera.transform.SetPositionAndRotation(position, Quaternion.Euler(pitch, yaw, 0f));
        }

        private static void Steer(float dt)
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 mouse = ZInput.GetMouseDelta();
                yaw += mouse.x;
                pitch = Mathf.Clamp(pitch - mouse.y, -MostPitch, MostPitch);
            }
            Vector3 along = Quaternion.Euler(pitch, yaw, 0f) * new Vector3(Held("Right") - Held("Left"), 0f, Held("Forward") - Held("Backward"));
            Vector3 move = along + Vector3.up * (Held("Jump") - Held("Crouch"));
            position += Vector3.ClampMagnitude(move, 1f) * Speed() * dt;
        }

        private static float Held(string button) => ZInput.GetButton(button) ? 1f : 0f;

        // The player's own fly speeds (a mod may change them), else the game's.
        private static float Speed()
        {
            Player player = Player.m_localPlayer;
            bool fast = ZInput.GetButton("Run");
            if (!player) return fast ? FastSpeed : SlowSpeed;
            return fast ? player.m_flyFastSpeed : player.m_flySlowSpeed;
        }

        private static bool Typing() =>
            global::Console.IsVisible() || (Chat.instance && Chat.instance.HasFocus()) || TextInput.IsVisible() || Menu.IsVisible();
    }

    [HarmonyPatch(typeof(GameCamera), "LateUpdate")]
    internal static class FlyCameraPatch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(GameCamera __instance) => FlyCamera.Fly(__instance);
    }
}
