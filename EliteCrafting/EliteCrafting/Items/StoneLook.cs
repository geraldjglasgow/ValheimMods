using System.Collections.Generic;
using UnityEngine;

namespace EliteCrafting.Items
{
    /// <summary>
    /// The visual state of one stone prefab on a client (prefabs.md section 4b-4c): its own material instances (so the
    /// vanilla base never changes), the base's lights, and its visual children for the grade scale. Tint and scale are
    /// written into the prefab; spawned stones copy them, and because world instances share the prefab's material
    /// instances, a tint change reaches stones already on the ground too. Never built on a dedicated server.
    /// </summary>
    internal sealed class StoneLook
    {
        // Emission strength relative to the tint: enough for the hue to read on a dark or strongly colored base.
        // Judgement call, to be checked in game.
        private const float EmissionStrength = 0.6f;

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        private readonly List<TintedMaterial> _materials = new List<TintedMaterial>();
        private readonly List<KeyValuePair<Light, Color>> _lights = new List<KeyValuePair<Light, Color>>();
        private readonly List<ChildPose> _children = new List<ChildPose>();

        public StoneLook(GameObject prefab)
        {
            foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterials = OwnCopies(renderer.sharedMaterials);
            }
            foreach (Light light in prefab.GetComponentsInChildren<Light>(true))
            {
                _lights.Add(new KeyValuePair<Light, Color>(light, light.color));
            }
            foreach (Transform child in prefab.transform)
            {
                _children.Add(new ChildPose(child));
            }
        }

        private Material[] OwnCopies(Material[] shared)
        {
            Material[] copies = new Material[shared.Length];
            for (int i = 0; i < shared.Length; i++)
            {
                if (shared[i] != null)
                {
                    copies[i] = new Material(shared[i]);
                    _materials.Add(new TintedMaterial(copies[i]));
                }
            }
            return copies;
        }

        /// <summary>Tints (or, with null, restores) every material and light of the prefab.</summary>
        public void Tint(Color? tint)
        {
            foreach (TintedMaterial material in _materials)
            {
                material.Apply(tint, EmissionStrength, ColorId, EmissionId);
            }
            foreach (KeyValuePair<Light, Color> light in _lights)
            {
                if (light.Key != null)
                {
                    light.Key.color = tint ?? light.Value;
                }
            }
        }

        /// <summary>
        /// Scales the visual children, not the root: the game resets the root's scale from the item quality whenever
        /// an item wakes up (ItemDrop.SetQuality), so a root scale would never survive a spawn.
        /// </summary>
        public void Scale(float factor)
        {
            foreach (ChildPose child in _children)
            {
                child.Apply(factor);
            }
        }

        private sealed class ChildPose
        {
            private readonly Transform _transform;
            private readonly Vector3 _scale;
            private readonly Vector3 _position;

            public ChildPose(Transform transform)
            {
                _transform = transform;
                _scale = transform.localScale;
                _position = transform.localPosition;
            }

            public void Apply(float factor)
            {
                _transform.localScale = _scale * factor;
                _transform.localPosition = _position * factor;
            }
        }
    }

    /// <summary>One material instance of a stone prefab, with the values it had before any tint.</summary>
    internal sealed class TintedMaterial
    {
        private readonly Material _material;
        private readonly bool _hasColor;
        private readonly Color _color;
        private readonly bool _hasEmission;
        private readonly Color _emission;
        private readonly bool _emissionOn;

        public TintedMaterial(Material material)
        {
            _material = material;
            _hasColor = material.HasProperty("_Color");
            _color = _hasColor ? material.GetColor("_Color") : Color.white;
            _hasEmission = material.HasProperty("_EmissionColor");
            _emission = _hasEmission ? material.GetColor("_EmissionColor") : Color.black;
            _emissionOn = material.IsKeywordEnabled("_EMISSION");
        }

        public void Apply(Color? tint, float emissionStrength, int colorId, int emissionId)
        {
            if (_hasColor)
            {
                _material.SetColor(colorId, tint.HasValue ? new Color(tint.Value.r, tint.Value.g, tint.Value.b, _color.a) : _color);
            }
            if (!_hasEmission)
            {
                return;
            }
            _material.SetColor(emissionId, tint.HasValue ? tint.Value * emissionStrength : _emission);
            if (tint.HasValue || _emissionOn)
            {
                _material.EnableKeyword("_EMISSION");
            }
            else
            {
                _material.DisableKeyword("_EMISSION");
            }
        }
    }
}
