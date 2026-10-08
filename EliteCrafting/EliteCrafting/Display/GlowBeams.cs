using System;
using System.Collections.Generic;
using BundlePrefabs;
using EliteCrafting.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EliteCrafting.Display
{
    /// <summary>
    /// The loot beam (display.md section 5, the user's pick "C" of the 2026-10-05 previews): a soft shaft of light about
    /// 3.7 m tall with sparks rising round its foot, from the embedded bundle <c>ecf_lootglow</c> (ValheimAssets
    /// <c>Assets/Effects/ecf_lootglow_beam_motes</c>), dressed in the game's particle shaders (<c>BundleEffects</c>).
    /// Its five systems are authored in Magic green; one recoloured copy is kept per glow colour (<see cref="Tint"/>),
    /// so an instance starts in its own colour (<see cref="ParticleRetint"/>). Local only: no ZNetView, nothing sent, never on a headless server.
    /// </summary>
    internal static class GlowBeams
    {
        public const string Bundle = "ecf_lootglow";
        public const string ChildName = "ecf_lootbeam";
        private const string Effect = "ecf_lootglow_beam_motes";

        private static readonly Dictionary<int, GameObject> Templates = new Dictionary<int, GameObject>();
        private static GameObject? _effect;
        private static bool _tried;

        /// <summary>A beam in this colour on the item, upright and unscaled; null when the effect cannot load.</summary>
        public static GameObject? Create(Transform item, Color32 colour)
        {
            GameObject? template = Template(colour);
            if (template == null)
            {
                return null;
            }
            GameObject beam = Object.Instantiate(template, item, false);
            beam.name = ChildName;
            Upright(beam.transform);
            return beam;
        }

        /// <summary>Stands the beam straight up at normal size, however the item lies or is scaled.</summary>
        public static void Upright(Transform beam)
        {
            if (beam.rotation != Quaternion.identity)
            {
                beam.rotation = Quaternion.identity;
            }
            float scale = beam.parent != null ? Mathf.Max(beam.parent.lossyScale.x, 0.001f) : 1f;
            Vector3 local = Vector3.one / scale;
            if (beam.localScale != local)
            {
                beam.localScale = local;
            }
        }

        private static GameObject? Template(Color32 colour)
        {
            int key = (colour.r << 16) | (colour.g << 8) | colour.b;
            if (Templates.TryGetValue(key, out GameObject template) && template != null)
            {
                return template;
            }
            GameObject? effect = Prepared();
            if (effect == null)
            {
                return null;
            }
            template = PrefabBench.Copy(effect, $"{Effect}_{key:X6}");
            ParticleRetint.Tint(template, colour);
            Templates[key] = template;
            return template;
        }

        /// <summary>The dressed effect, made once after the game's scene (and its shaders) is up; null on failure.</summary>
        private static GameObject? Prepared()
        {
            if (_tried || ZNetScene.instance == null)
            {
                return _effect;
            }
            _tried = true;
            try
            {
                _effect = BundleEffects.Prepare(EmbeddedBundle.Load(typeof(GlowBeams).Assembly, Bundle), Effect);
            }
            catch (Exception e)
            {
                Log.Warn($"the loot beam did not load, dropped items keep only their light: {e.Message}");
            }
            return _effect;
        }
    }
}
