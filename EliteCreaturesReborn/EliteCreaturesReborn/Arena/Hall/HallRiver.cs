using UnityEngine;

namespace EliteCreaturesReborn.Arena.Hall
{
    /// <summary>
    /// Hel's river flowing (features/arena.md section 1), clients only, on the river's surface (<c>ecr_helarena_river</c>,
    /// a flat ring at y -8 in its own Standard material): two swirls drifting through each other, no waves. Its material
    /// instance (made once, destroyed with it) moves <c>_MainTex</c> (the albedo and the emission together) by (0.010,
    /// 0.004) tiles a second, about 0.5 m/s, and <c>_DetailAlbedoMap</c> the other way by (-0.007, 0.006), the river's
    /// README. Every frame while the local player is in the arena's zone, nothing otherwise; offsets kept in 0 to 1, no
    /// allocations.
    /// </summary>
    internal sealed class HallRiver : MonoBehaviour
    {
        private static readonly Vector2 MainFlow = new Vector2(0.010f, 0.004f);
        private static readonly Vector2 DetailFlow = new Vector2(-0.007f, 0.006f);
        private static readonly int Detail = Shader.PropertyToID("_DetailAlbedoMap");

        private Material? _material;
        private Vector2 _main;
        private Vector2 _detail;

        private void Awake()
        {
            Renderer? surface = GetComponentInChildren<Renderer>(true);
            _material = surface != null ? surface.material : null;
            if (_material != null)
            {
                _main = _material.mainTextureOffset;
                _detail = _material.GetTextureOffset(Detail);
            }
            enabled = _material != null;
        }

        private void OnDestroy()
        {
            if (_material != null)
            {
                Destroy(_material);
            }
        }

        private void Update()
        {
            if (!HallEnvironment.PlayerInZone)
            {
                return;
            }
            float dt = Time.deltaTime;
            _main = Wrap(_main + MainFlow * dt);
            _detail = Wrap(_detail + DetailFlow * dt);
            _material!.mainTextureOffset = _main;
            _material.SetTextureOffset(Detail, _detail);
        }

        private static Vector2 Wrap(Vector2 offset) =>
            new Vector2(offset.x - Mathf.Floor(offset.x), offset.y - Mathf.Floor(offset.y));
    }
}
