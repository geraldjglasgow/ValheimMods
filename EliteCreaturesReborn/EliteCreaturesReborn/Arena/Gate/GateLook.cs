using BundlePrefabs;
using EliteCreaturesReborn.Arena.Hall;
using EliteCreaturesReborn.Config;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Gate
{
    /// <summary>
    /// Helgrind open or closed to the eye, on clients only (features/arena.md section 2): while Arena is on, the swirl at
    /// the model's <c>portal_center</c>, its light at <c>light</c> (<see cref="GateSwirl"/>) and the stone portal's looping
    /// sound faded in by its <c>EffectFade</c>; while it is off, none of them. The glow parts glow either way. Read from the
    /// setting on this machine (the server's value while it binds), nothing sent; settled, one comparison a frame. Off on
    /// a dedicated server and on a placement ghost (no ZDO).
    /// </summary>
    internal sealed class GateLook : MonoBehaviour
    {
        private Transform? _center;
        private Transform? _light;
        private EffectFade? _fade;
        private GameObject? _swirl;
        private GameObject? _glow;
        private bool _shown;

        // Start, not Awake: by now the piece's ZNetView has woken and has its ZDO, or none on a ghost.
        private void Start()
        {
            ZNetView? view = GetComponent<ZNetView>();
            if (HallBuilder.NoScreen || view == null || view.GetZDO() == null)
            {
                enabled = false;
                return;
            }
            _center = GameMaterials.Find(transform, GateModel.Center);
            _light = GameMaterials.Find(transform, GateModel.LightPoint);
            _fade = GetComponentInChildren<EffectFade>(true);
        }

        private void Update()
        {
            bool open = ArenaSettings.Enabled;
            if (open == _shown)
            {
                return;
            }
            _shown = open;
            if (_fade != null)
            {
                _fade.SetActive(open);
            }
            if (open)
            {
                Show();
            }
            else
            {
                Hide();
            }
        }

        private void Show()
        {
            if (_swirl == null && _center != null)
            {
                _swirl = GateSwirl.Swirl(_center);
            }
            if (_glow == null && _light != null)
            {
                _glow = GateSwirl.Light(_light, _light.position);
            }
        }

        private void Hide()
        {
            if (_swirl != null)
            {
                Destroy(_swirl);
            }
            if (_glow != null)
            {
                Destroy(_glow);
            }
            _swirl = null;
            _glow = null;
        }
    }
}
