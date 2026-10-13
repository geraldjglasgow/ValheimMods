using System;
using BundlePrefabs;
using EliteCreaturesReborn.Arena.Gate;
using EliteCreaturesReborn.Util;
using LocalEffects;
using UnityEngine;
using UnityEngine.Rendering;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// What an archway's portal looks like (features/arena.md section 3, "Waves"), clients only: the workshop's
    /// <c>ecr_helhall_portal</c> (bundle <c>ecr_helhall_portalfx</c>, ValheimAssets <c>Assets/Effects/ecr_helhall_portal</c>),
    /// Helgrind's swirl reworked to fill one recess exactly (4.0 m wide, the arch springing at 2.8 m, its crown at 5.0 m,
    /// mist along its floor) round the walls' <c>portal_N</c> point, +Z into the hall. It has no prewarm, so playing it
    /// opens it (the dark heart fades in over 0.5 s while a flare runs round the edge); stopping its emission closes it
    /// (gone within 2.5 s), with its one-shot <c>ecr_helhall_portal_close</c> (1.2 s, removing itself after 3 s) played on
    /// the same anchor. Copies are local only, through LocalEffects (never networked, nothing on a dedicated server),
    /// dressed in the game's particle shaders by BundlePrefabs once per process. The light is the mod's: the hall's cold
    /// fire #80ebe0, intensity 1.8, range 7, no shadows (four may burn at once), 0.8 m out from the anchor. The effect is
    /// the one <see cref="Current"/> line: Helgrind's swirl stood in for it before it existed, at 1.55 times its size.
    /// </summary>
    internal static class ArenaPortalLook
    {
        // Helgrind's swirl, the stand-in before the archway portal existed:
        // new Look(GateModel.EffectBundle, "ecr_helgate_swirl", "", 1.55f)
        private static readonly Look Current = new Look("ecr_helhall_portalfx", "ecr_helhall_portal", "ecr_helhall_portal_close", 1f);

        private static readonly Color ColdFire = new Color(0.5f, 0.92f, 0.88f);

        /// <summary>The light's intensity when the portal is open.</summary>
        public const float FullLight = 1.8f;

        private const float Range = 7f;

        /// <summary>The light's place from the anchor: inside the recess.</summary>
        private static readonly Vector3 LightAt = new Vector3(0f, 0f, 0.8f);

        private static GameObject? _loop;
        private static GameObject? _close;
        private static bool _tried;

        /// <summary>The effect's source: its bundle, the loop, the closing one-shot ("" for none), and how many times its
        /// authored size it is drawn.</summary>
        private readonly struct Look
        {
            public Look(string bundle, string loop, string close, float scale)
            {
                Bundle = bundle;
                Loop = loop;
                Close = close;
                Scale = scale;
            }

            public string Bundle { get; }
            public string Loop { get; }
            public string Close { get; }
            public float Scale { get; }
        }

        /// <summary>True on a machine with no screen (a dedicated server): no portals are drawn there.</summary>
        public static bool NoScreen => SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;

        /// <summary>The loop at the anchor, facing as it faces, opening as it plays; null without the bundle.</summary>
        public static GameObject? Open(Transform anchor) => Facing(Attach(Prepared() ? _loop : null, anchor, true), anchor);

        /// <summary>The closing one-shot at the anchor; it removes itself.</summary>
        public static void Closing(Transform anchor) => Facing(Attach(Prepared() ? _close : null, anchor, false), anchor);

        /// <summary>The portal's light under the anchor, dark until the portal opens.</summary>
        public static Light Light(Transform anchor)
        {
            var glow = new GameObject("ecr_arena_portal_light");
            glow.transform.SetParent(anchor, false);
            glow.transform.localPosition = LightAt;
            Light light = glow.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = ColdFire;
            light.intensity = 0f;
            light.range = Range;
            light.shadows = LightShadows.None; // four may burn at once: the hall's braziers cast the shadows
            glow.AddComponent<LightLod>(); // the game's light limit and distance fade; it reads the range above
            return light;
        }

        private static GameObject? Attach(GameObject? prefab, Transform anchor, bool endless) =>
            LocalEffect.AttachScaled(prefab, anchor, anchor.position, endless, Current.Scale);

        private static GameObject? Facing(GameObject? copy, Transform anchor)
        {
            if (copy != null)
            {
                copy.transform.rotation = anchor.rotation;
            }
            return copy;
        }

        // Both effects prepared once per process; false without the bundle or before the scene is up.
        private static bool Prepared()
        {
            if (!_tried && ZNetScene.instance != null)
            {
                _tried = true;
                Prepare();
            }
            return _loop != null;
        }

        private static void Prepare()
        {
            AssetBundle? bundle = GateModel.Load(Current.Bundle);
            try
            {
                _loop = bundle != null ? BundleEffects.Prepare(bundle, Current.Loop, "ecr_arena_portal") : null;
                bool closes = bundle != null && Current.Close.Length > 0;
                _close = closes ? BundleEffects.Prepare(bundle!, Current.Close, "ecr_arena_portal_close") : null;
            }
            catch (Exception e)
            {
                Log.Error($"Hel's Trial: the archway portal did not load: {e.Message}");
            }
        }
    }
}
