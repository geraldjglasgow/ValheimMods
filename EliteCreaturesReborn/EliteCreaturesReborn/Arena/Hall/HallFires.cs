using EliteCreaturesReborn.Util;
using LocalEffects;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Hall
{
    /// <summary>
    /// The hall's fire and light (features/arena.md section 1, "Light and mood"), clients only. Blue fire from the game's
    /// blue brazier (<c>MountainKit_brazier_blue</c>: its flames <c>_enabled_high/fx_Brazier_flames</c> and its
    /// <c>_enabled_high/Point light</c> with the game's LightLod and LightFlicker), copied local-only through LocalEffects
    /// (never networked, its warmth and burn areas left behind) at the eight sconces (<c>flame_0</c>..<c>7</c>, bowls
    /// 0.54 m across) and the throne's two braziers (<c>fire_0</c>, <c>_1</c>, 1.37 m), each sized to its bowl against
    /// the game brazier's 1 m one, each light tinted the hall's cold blue-green and reaching further than its flames. A
    /// cold spot light hangs in the oculus (<c>light_oculus</c>), shining down on the rune circle. Shadows only on the
    /// two throne braziers and the oculus, each faded in within reach of the player by the game's LightLod, which also
    /// keeps every light to the game's light limits. The copies are children of their points, so they go with the hall.
    /// </summary>
    internal static class HallFires
    {
        private const string Brazier = "MountainKit_brazier_blue";
        private const string Flames = "_enabled_high/fx_Brazier_flames";
        private const string Glow = "_enabled_high/Point light";

        private const int Sconces = 8;
        private const int Braziers = 2;
        private const float SconceFlames = 0.55f;
        private const float ThroneFlames = 1.35f;

        // The brazier's light reaches 5 m at intensity 2 (flickering); the hall is 51 m across.
        private const float SconceLight = 1.4f;
        private const float ThroneLight = 1.8f;
        private const float LightRise = 0.4f;
        private static readonly Color ColdFire = new Color(0.5f, 0.92f, 0.88f);

        private const float ShadowStrength = 0.7f;
        private const float ShadowReach = 25f;

        // The oculus: a cold blue-white spot from 24 m up, wide enough to wash the rune circle (10 m) and fade at the rim.
        private static readonly Color Cold = new Color(0.62f, 0.8f, 1f);
        private const float OculusAngle = 70f;
        private const float OculusRange = 34f;
        private const float OculusIntensity = 1.4f;
        private const float OculusReach = 80f;

        public static void Kindle()
        {
            GameObject? brazier = ZNetScene.instance.GetPrefab(Brazier);
            GameObject? flames = brazier == null ? null : brazier.transform.Find(Flames)?.gameObject;
            GameObject? glow = brazier == null ? null : brazier.transform.Find(Glow)?.gameObject;
            if (flames == null || glow == null)
            {
                Log.Warn($"Hel's hall: the game's {Brazier} has no {Flames} or {Glow}; the hall's fires stay dark.");
            }
            else
            {
                Fires("flame_", Sconces, flames, glow, SconceFlames, SconceLight, shadows: false);
                Fires("fire_", Braziers, flames, glow, ThroneFlames, ThroneLight, shadows: true);
            }
            Oculus();
        }

        private static void Fires(string prefix, int count, GameObject flames, GameObject glow, float size, float reach,
            bool shadows)
        {
            for (int k = 0; k < count; k++)
            {
                if (!HallPoints.TryGet(prefix + k, out Transform at))
                {
                    Log.Warn($"Hel's hall: no point {prefix}{k}; that fire stays dark.");
                    continue;
                }
                LocalEffect.AttachScaled(flames, at, at.position, endless: true, size);
                Vector3 above = at.position + Vector3.up * (LightRise * size);
                Tint(LocalEffect.AttachScaled(glow, at, above, endless: true, reach), shadows);
            }
        }

        // The light keeps its flicker and LightLod; LightLod turned shadow LOD off because the brazier casts none, so it is
        // turned back on here: shadows fade in within ShadowReach of the player.
        private static void Tint(GameObject? copy, bool shadows)
        {
            Light? light = copy == null ? null : copy.GetComponentInChildren<Light>(true);
            if (light == null)
            {
                return;
            }
            light.color = ColdFire;
            LightLod? lod = light.GetComponent<LightLod>();
            if (shadows && lod != null)
            {
                lod.m_shadowLod = true;
                lod.m_shadowDistance = ShadowReach;
                lod.m_baseShadowStrength = ShadowStrength;
            }
        }

        private static void Oculus()
        {
            if (!HallPoints.TryGet("light_oculus", out Transform at))
            {
                Log.Warn("Hel's hall: no point light_oculus; the oculus casts no light.");
                return;
            }
            var spot = new GameObject("ecr_oculus_light");
            spot.transform.SetParent(at, false);
            spot.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
            Light light = spot.AddComponent<Light>();
            light.type = LightType.Spot;
            light.spotAngle = OculusAngle;
            light.range = OculusRange;
            light.intensity = OculusIntensity;
            light.color = Cold;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = ShadowStrength;
            LightLod lod = spot.AddComponent<LightLod>(); // wakes now: reads the range and shadows above
            lod.m_lightDistance = OculusReach;
            lod.m_shadowDistance = OculusReach;
        }
    }
}
