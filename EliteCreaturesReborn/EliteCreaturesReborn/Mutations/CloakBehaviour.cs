using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Traits;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// Cloaked: hides the creature beyond its reveal distance from the local player and phases it back in over a fade
    /// time as the player closes, rather than snapping, so a hard cut never looks like a rendering fault. A hysteresis
    /// band (fade in at reveal distance, out only at reveal distance + margin) stops it strobing for a player standing
    /// on the boundary. This is a client-side visual, so it runs on every client; the nameplate patch reads
    /// <see cref="Hidden"/> and hides the nameplate in step. See DECISIONS.md on why the body may snap on opaque shaders.
    /// </summary>
    public sealed class CloakBehaviour : MonoBehaviour
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private Renderer[] _renderers = System.Array.Empty<Renderer>();
        private MaterialPropertyBlock _block = null!;
        private float _reveal = 6f;
        private float _margin = 1f;
        private float _fadeTime = 0.5f;
        private bool _revealed;
        private float _phase;

        /// <summary>True while the creature is fully cloaked, so the nameplate can hide in exact step with the body.</summary>
        public bool Hidden => _phase <= 0f;

        private void Start()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
            _block = new MaterialPropertyBlock();
            EliteController controller = GetComponent<EliteController>();
            if (controller != null)
            {
                _reveal = Scaling.Enhance.Magnitude(controller.Rules, controller.Traits, Mutation.Cloaked,
                    Fields.RevealDistance);
                _margin = controller.Rules.PowerOf(Mutation.Cloaked, Fields.FadeMargin);
                _fadeTime = controller.Rules.PowerOf(Mutation.Cloaked, Fields.FadeTime);
            }
            Apply();
        }

        private void Update() => Guard.Run("CloakBehaviour.Update", Step);

        private void Step()
        {
            Player local = Player.m_localPlayer;
            if (local == null)
            {
                return;
            }
            float distance = Vector3.Distance(local.transform.position, transform.position);
            UpdateReveal(distance);
            _phase = Advance(_phase, _revealed ? 1f : 0f);
            Apply();
        }

        /// <summary>Hysteresis: reveal at the reveal distance, re-hide only a margin further out, so the two never coincide.</summary>
        private void UpdateReveal(float distance)
        {
            if (_revealed && distance > _reveal + _margin)
            {
                _revealed = false;
            }
            else if (!_revealed && distance <= _reveal)
            {
                _revealed = true;
            }
        }

        private float Advance(float current, float target)
        {
            if (_fadeTime <= 0f)
            {
                return target;
            }
            return Mathf.MoveTowards(current, target, Time.deltaTime / _fadeTime);
        }

        private void Apply()
        {
            bool visible = _phase > 0.001f;
            foreach (Renderer renderer in _renderers)
            {
                if (renderer == null)
                {
                    continue;
                }
                renderer.enabled = visible;
                if (visible)
                {
                    SetAlpha(renderer, _phase);
                }
            }
        }

        /// <summary>Best-effort alpha fade; a no-op on opaque creature shaders, which is why the body can snap.</summary>
        private void SetAlpha(Renderer renderer, float alpha)
        {
            Material shared = renderer.sharedMaterial;
            if (shared == null || !shared.HasProperty(ColorId))
            {
                return;
            }
            renderer.GetPropertyBlock(_block);
            Color color = shared.GetColor(ColorId);
            color.a = alpha;
            _block.SetColor(ColorId, color);
            renderer.SetPropertyBlock(_block);
        }
    }
}
