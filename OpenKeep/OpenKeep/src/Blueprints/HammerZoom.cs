using UnityEngine;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// With an entry of the hammer's Blueprints tab selected in build mode the camera zooms out to <see cref="MaxDistance"/>
    /// m (the game's own limit is about 6), so a large structure fits on screen, and the wheel zooms faster the farther
    /// out it is (the tab's entries cannot rotate, so the game hands the wheel to the camera). The camera's own values
    /// come back as soon as another piece is picked or the hammer is put away.
    /// </summary>
    public static class HammerZoom
    {
        public const float MaxDistance = 80f;
        private const float StepDistance = 6f;

        private static float savedMax = -1f;
        private static float savedSensitivity;

        public static void Tick()
        {
            GameCamera camera = GameCamera.instance;
            if (camera == null)
            {
                savedMax = -1f;
                return;
            }
            if (Active())
                Open(camera);
            else if (savedMax >= 0f)
                Close(camera);
        }

        private static bool Active() => BlueprintMenu.InHand(Player.m_localPlayer);

        private static void Open(GameCamera camera)
        {
            if (savedMax < 0f)
            {
                savedMax = camera.m_maxDistance;
                savedSensitivity = camera.m_zoomSens;
            }
            camera.m_maxDistance = Mathf.Max(savedMax, MaxDistance);
            camera.m_zoomSens = savedSensitivity * Mathf.Max(1f, camera.m_distance / StepDistance);
        }

        private static void Close(GameCamera camera)
        {
            camera.m_maxDistance = savedMax;
            camera.m_zoomSens = savedSensitivity;
            camera.m_distance = Mathf.Min(camera.m_distance, savedMax);
            savedMax = -1f;
        }
    }
}
