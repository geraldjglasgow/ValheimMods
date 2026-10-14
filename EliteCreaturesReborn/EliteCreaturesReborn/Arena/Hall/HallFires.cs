using EliteCreaturesReborn.Util;
using LocalEffects;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Hall
{
    /// <summary>
    /// The arena's fire and light (features/arena.md section 1, "Light and mood"), clients only. Blue fire from the game's
    /// blue brazier (<c>MountainKit_brazier_blue</c>: its flames <c>_enabled_high/fx_Brazier_flames</c> and its
    /// <c>_enabled_high/Point light</c> with the game's LightLod and LightFlicker), copied local-only through LocalEffects
    /// (never networked, its warmth and burn areas left behind) at the walls' sixteen sconces (<c>flame_0</c>..<c>15</c>,
    /// bowls 0.54 m across) and the throne's two braziers (<c>fire_0</c>, <c>_1</c>, 1.37 m), each sized to its bowl
    /// against the game brazier's 1 m one, each light tinted the arena's cold blue-green and reaching further than its
    /// flames: 18 point lights. Shadows only on the throne's two braziers, faded in within 25 m of the player. LightLod
    /// lights each one within 90 m of the player (the brazier's own 50 m would leave the walls dark from the arena's
    /// middle, 66 m from them) and keeps them to the game's light limit from the player's graphics settings, nearest
    /// first. The arena is open to the sky: no oculus, no light of its own over it. The copies are children of their
    /// points, so they go with the arena.
    /// </summary>
    internal static class HallFires
    {
        private const string Brazier = "MountainKit_brazier_blue";
        private const string Flames = "_enabled_high/fx_Brazier_flames";
        private const string Glow = "_enabled_high/Point light";

        private const int Sconces = 16;
        private const int Braziers = 2;
        private const float SconceFlames = 0.55f;
        private const float ThroneFlames = 1.35f;

        // The brazier's light reaches 5 m at intensity 2 (flickering); the arena is 132 m across.
        private const float SconceLight = 1.6f;
        private const float ThroneLight = 1.8f;
        private const float LightRise = 0.4f;
        private static readonly Color ColdFire = new Color(0.5f, 0.92f, 0.88f);

        private const float ShadowStrength = 0.7f;
        private const float ShadowReach = 25f;
        private const float LightReach = 90f;

        public static void Kindle()
        {
            GameObject? flames = Part(Flames);
            GameObject? glow = Part(Glow);
            if (flames == null || glow == null)
            {
                Log.Warn($"Hel's arena: the game's {Brazier} has no {Flames} or {Glow}; the arena's fires stay dark.");
                return;
            }
            Fires("flame_", Sconces, flames, glow, SconceFlames, SconceLight, shadows: false);
            Fires("fire_", Braziers, flames, glow, ThroneFlames, ThroneLight, shadows: true);
        }

        /// <summary>A small blue flame with no light of its own, at a point (Hel's Anvil's embers), clients only;
        /// <paramref name="size"/> against the game brazier's 1 m bowl.</summary>
        public static void Flame(Transform at, float size)
        {
            GameObject? flames = Part(Flames);
            if (flames != null)
            {
                LocalEffect.AttachScaled(flames, at, at.position, endless: true, size);
            }
        }

        private static GameObject? Part(string path)
        {
            GameObject? brazier = ZNetScene.instance.GetPrefab(Brazier);
            Transform? part = brazier == null ? null : brazier.transform.Find(path);
            return part == null ? null : part.gameObject;
        }

        private static void Fires(string prefix, int count, GameObject flames, GameObject glow, float size, float reach,
            bool shadows)
        {
            for (int k = 0; k < count; k++)
            {
                if (!HallPoints.TryGet(prefix + k, out Transform at))
                {
                    Log.Warn($"Hel's arena: no point {prefix}{k}; that fire stays dark.");
                    continue;
                }
                LocalEffect.AttachScaled(flames, at, at.position, endless: true, size);
                Vector3 above = at.position + Vector3.up * (LightRise * size);
                Tint(LocalEffect.AttachScaled(glow, at, above, endless: true, reach), shadows);
            }
        }

        // The light keeps its flicker and LightLod, lit within LightReach; LightLod turned shadow LOD off because the
        // brazier casts none, so for the throne's it is turned back on here: shadows fade in within ShadowReach.
        private static void Tint(GameObject? copy, bool shadows)
        {
            Light? light = copy == null ? null : copy.GetComponentInChildren<Light>(true);
            if (light == null)
            {
                return;
            }
            light.color = ColdFire;
            LightLod? lod = light.GetComponent<LightLod>();
            if (lod == null)
            {
                return;
            }
            lod.m_lightDistance = LightReach;
            if (shadows)
            {
                lod.m_shadowLod = true;
                lod.m_shadowDistance = ShadowReach;
                lod.m_baseShadowStrength = ShadowStrength;
            }
        }
    }
}
