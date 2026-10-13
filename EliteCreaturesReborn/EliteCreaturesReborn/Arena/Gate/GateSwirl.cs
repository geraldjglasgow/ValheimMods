using System;
using BundlePrefabs;
using EliteCreaturesReborn.Util;
using LocalEffects;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Gate
{
    /// <summary>
    /// Helgrind's swirl and light, clients only, at the gate (<see cref="GateLook"/>) and at the hall's way out
    /// (<see cref="GateWayOut"/>). The swirl is the workshop's <c>ecr_helgate_swirl</c> (bundle <c>ecr_helgatefx</c>,
    /// ValheimAssets <c>Assets/Effects/ecr_helgate_swirl</c>: the game's portal swirl in the hall's blue-green, filling
    /// the gate's arched opening round its origin, facing its +Z, looping, in local space), dressed in the game's particle
    /// shaders by BundlePrefabs once per process and copied local-only through LocalEffects (never networked, made nothing
    /// on a dedicated server). The effect has no light: each copy gets a point light in the hall's cold fire #80ebe0,
    /// intensity 2 (the game's portal lights'), range 6 for the gate's wood-portal size, soft shadows, faded by the game's
    /// LightLod.
    /// </summary>
    internal static class GateSwirl
    {
        private const string Effect = "ecr_helgate_swirl";
        private static readonly Color ColdFire = new Color(0.5f, 0.92f, 0.88f);
        private const float Intensity = 2f;
        private const float Range = 6f;

        private static GameObject? _effect;
        private static bool _tried;

        /// <summary>The swirl at the anchor, facing as it faces, a child of it; null without the bundle or a screen.</summary>
        public static GameObject? Swirl(Transform anchor)
        {
            GameObject? copy = LocalEffect.Attach(Prepared(), anchor, anchor.position, endless: true);
            if (copy != null)
            {
                copy.transform.rotation = anchor.rotation;
            }
            return copy;
        }

        /// <summary>The swirl's light at a point, a child of the parent.</summary>
        public static GameObject Light(Transform parent, Vector3 at)
        {
            var glow = new GameObject("ecr_helgrind_light");
            glow.transform.SetParent(parent, false);
            glow.transform.position = at;
            Light light = glow.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = ColdFire;
            light.intensity = Intensity;
            light.range = Range;
            light.shadows = LightShadows.Soft;
            glow.AddComponent<LightLod>(); // wakes now: reads the range and shadows above
            return glow;
        }

        private static GameObject? Prepared()
        {
            if (_tried || ZNetScene.instance == null)
            {
                return _effect;
            }
            _tried = true;
            AssetBundle? bundle = GateModel.Load(GateModel.EffectBundle);
            try
            {
                _effect = bundle != null ? BundleEffects.Prepare(bundle, Effect) : null;
            }
            catch (Exception e)
            {
                Log.Error($"Helgrind's swirl did not load: {e.Message}");
            }
            return _effect;
        }
    }
}
