using System;
using System.Collections.Generic;
using BundlePrefabs;
using EliteCrafting.Core;
using EliteCrafting.Display;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EliteCrafting.Tables
{
    /// <summary>
    /// The Rune Table's vortex: motes and wisps spiralling up from the slab and dissipating above the table, from the
    /// embedded bundle <c>ecf_tablefx</c> (ValheimAssets <c>Assets/Effects/ecf_rune_vortex</c>), dressed in the game's
    /// particle shaders (<c>BundleEffects</c>) and recoloured per stone colour (<see cref="ParticleRetint"/>, one copy kept per
    /// colour). It plays once on its own; the instance is destroyed after <see cref="Life"/>. Local only, never networked.
    /// Without the bundle nothing shows (the craft sound still plays).
    /// </summary>
    internal static class TableVortex
    {
        private const string Bundle = "ecf_tablefx";
        private const string Effect = "ecf_rune_vortex";
        private const float Life = 4f;

        private static readonly Dictionary<int, GameObject> Templates = new Dictionary<int, GameObject>();
        private static GameObject? _effect;
        private static bool _tried;

        public static void Spawn(Vector3 at, Color colour)
        {
            GameObject? template = Template(colour);
            if (template != null)
            {
                Object.Destroy(Object.Instantiate(template, at, Quaternion.identity), Life);
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

        private static GameObject? Prepared()
        {
            if (_tried || ZNetScene.instance == null)
            {
                return _effect;
            }
            _tried = true;
            try
            {
                _effect = BundleEffects.Prepare(EmbeddedBundle.Load(typeof(TableVortex).Assembly, Bundle), Effect);
            }
            catch (Exception e)
            {
                Log.Warn($"the Rune Table's vortex did not load, its uses only sound: {e.Message}");
            }
            return _effect;
        }
    }
}
