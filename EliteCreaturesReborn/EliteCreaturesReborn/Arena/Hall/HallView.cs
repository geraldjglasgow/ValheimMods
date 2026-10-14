using UnityEngine;

namespace EliteCreaturesReborn.Arena.Hall
{
    /// <summary>
    /// What the camera sees while the local player is in the arena's zone (<see cref="HallEnvironment.PlayerInZone"/>,
    /// 160 x 60 x 160 m), clients only, given back as found when they leave or the arena goes:
    /// <list type="bullet">
    /// <item>Reach: the mountains and the cave's roof stand up to 1,149 m from any place in the arena. The game never sets
    /// the far clip in code (GameCamera moves only the near plane; the far plane is the main scene's), so it is raised to
    /// <see cref="Reach"/> when it is shorter.</item>
    /// <item>No sky: the arena is a cave. The game draws the sky (its dome, stars, moon and sun disc, the aurora) with a
    /// camera of its own, <c>GameCamera.m_skyCamera</c>; it is switched off, the cheapest way (nothing drawn, rather than
    /// an empty sky drawn), and the main camera clears to a near-black of the arena's cold blue-green grey instead, so a
    /// gap at a far edge shows dark rock colour, never last frame's.</item>
    /// </list>
    /// Tested twice a second (the zone test is shared and cached), a few comparisons each.
    /// </summary>
    internal sealed class HallView : MonoBehaviour
    {
        private const float Reach = 1300f;
        private static readonly Color CaveDark = new Color(0.035f, 0.05f, 0.055f);

        private Camera? _camera;
        private Camera? _sky;
        private float _far;
        private CameraClearFlags _clear;
        private Color _background;
        private bool _inside;

        private void Update()
        {
            bool inside = HallEnvironment.PlayerInZone;
            if (inside == _inside)
            {
                return;
            }
            if (inside)
            {
                Enter();
            }
            else
            {
                Leave();
            }
        }

        private void OnDestroy() => Leave();

        private void Enter()
        {
            GameCamera? game = GameCamera.instance;
            _camera = game != null ? game.m_camera : null;
            if (_camera == null)
            {
                return;
            }
            _inside = true;
            (_far, _clear, _background) = (_camera.farClipPlane, _camera.clearFlags, _camera.backgroundColor);
            _camera.farClipPlane = Mathf.Max(_far, Reach);
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = CaveDark;
            _sky = game!.m_skyCamera != null && game.m_skyCamera.enabled ? game.m_skyCamera : null;
            if (_sky != null)
            {
                _sky.enabled = false;
            }
        }

        // Only what is still ours is given back: anything another mod changed since is left alone.
        private void Leave()
        {
            _inside = false;
            if (_camera != null)
            {
                if (_camera.farClipPlane == Reach && _far < Reach)
                {
                    _camera.farClipPlane = _far;
                }
                if (_camera.clearFlags == CameraClearFlags.SolidColor && _camera.backgroundColor == CaveDark)
                {
                    (_camera.clearFlags, _camera.backgroundColor) = (_clear, _background);
                }
            }
            if (_sky != null)
            {
                _sky.enabled = true;
            }
            (_camera, _sky) = (null, null);
        }
    }
}
