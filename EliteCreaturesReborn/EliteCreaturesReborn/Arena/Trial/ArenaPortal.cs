using UnityEngine;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// One archway's portal on this client (<see cref="ArenaPortals"/> opens and closes it): a plain local object at the
    /// archway's <c>portal_N</c> point carrying the portal's light, and while open the portal's loop
    /// (<see cref="ArenaPortalLook"/>). Opening plays the loop, which forms itself, and brings the light up over half a
    /// second; closing stops the loop's emission (it fades out on its own and is removed after three seconds), plays the
    /// closing one-shot, and takes the light down over a second and a half. It runs a frame update only while the light
    /// is changing.
    /// </summary>
    internal sealed class ArenaPortal : MonoBehaviour
    {
        private const float LightInSeconds = 0.5f;
        private const float LightOutSeconds = 1.5f;

        /// <summary>Seconds a stopped loop is kept for its particles to fade.</summary>
        private const float LoopLinger = 3f;

        private Light _light = null!;
        private GameObject? _loop;
        private float _level;

        /// <summary>True from the open to the close.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>A closed portal at a spot, a child of the parent (it goes when the parent goes).</summary>
        public static ArenaPortal Make(Transform parent, ArenaSpots.Spot at)
        {
            var anchor = new GameObject("ecr_arena_portal");
            anchor.transform.SetParent(parent, worldPositionStays: true);
            anchor.transform.SetPositionAndRotation(at.Position, at.Facing);
            ArenaPortal portal = anchor.AddComponent<ArenaPortal>();
            portal._light = ArenaPortalLook.Light(anchor.transform);
            portal.enabled = false;
            return portal;
        }

        public void Open()
        {
            if (IsOpen)
            {
                return;
            }
            IsOpen = true;
            _loop = ArenaPortalLook.Open(transform);
            enabled = true;
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }
            IsOpen = false;
            if (_loop != null)
            {
                foreach (ParticleSystem system in _loop.GetComponentsInChildren<ParticleSystem>())
                {
                    system.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                }
                Destroy(_loop, LoopLinger);
                _loop = null;
            }
            ArenaPortalLook.Closing(transform);
            enabled = true;
        }

        private void Update()
        {
            float target = IsOpen ? 1f : 0f;
            float step = Time.deltaTime / (IsOpen ? LightInSeconds : LightOutSeconds);
            _level = Mathf.MoveTowards(_level, target, step);
            _light.intensity = _level * ArenaPortalLook.FullLight;
            if (Mathf.Approximately(_level, target))
            {
                enabled = false;
            }
        }
    }
}
