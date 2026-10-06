using System.Collections.Generic;
using EliteCrafting.Affixes;
using EliteCrafting.Config;
using EliteCrafting.Items;
using EliteCrafting.Rules;
using UnityEngine;

namespace EliteCrafting.Display
{
    /// <summary>
    /// One dropped item the glow manager knows (display.md section 5): whether it glows and in which color, and its
    /// light and loot beam (<see cref="GlowBeams"/>) if it has them. The item's data is reloaded from its ZDO only when
    /// the saved item bytes really changed (<see cref="Reload"/>: a moving drop's ZDO changes revision with every
    /// position update, its item bytes do not), and the color is re-decided only when the item's data object, its
    /// custom-data dictionary (the game's <c>Load</c> replaces it), the rules generation or the <c>Glow stones</c> switch
    /// changed, so a tick touches the parse cache only for new or changed items. Viewing client only.
    /// </summary>
    internal sealed class GlowItem
    {
        private const float LightHeight = 0.3f;

        private byte[]? _bytes;
        private ItemDrop.ItemData? _data;
        private Dictionary<string, string>? _source;
        private int _rulesGeneration = -1;
        private bool _stonesSwitch;
        private Light? _light;
        private GameObject? _beam;
        private Color32 _beamColor;

        public GlowItem(ItemDrop drop)
        {
            Drop = drop;
        }

        public ItemDrop Drop { get; }
        public int Stamp { get; set; }
        public float SqrDistance { get; set; }
        public bool Glows { get; private set; }
        public Color32 Color { get; private set; }

        /// <summary>
        /// The game's <c>Load</c> (a full re-read of the item) only when the ZDO's item bytes are a new array with new
        /// content: the first sight, or a real change to the item on the ground. A moving drop's ZDO is re-sent with
        /// every position update, as a new array holding the same bytes; a byte compare skips those.
        /// </summary>
        public void Reload(ZDO zdo)
        {
            byte[]? bytes = zdo.GetByteArray(ZDOVars.s_itemData);
            if (ReferenceEquals(bytes, _bytes))
            {
                return;
            }
            bool same = bytes != null && _bytes != null && SameBytes(bytes, _bytes);
            _bytes = bytes;
            if (!same)
            {
                Drop.Load();
            }
        }

        private static bool SameBytes(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>Re-decides glow and color if anything it depends on changed; removes the light if it stops glowing.</summary>
        public void Refresh()
        {
            ItemDrop.ItemData data = Drop.m_itemData;
            bool stones = ModSettings.GlowStones.Value;
            if (ReferenceEquals(data, _data) && ReferenceEquals(data?.m_customData, _source)
                && _rulesGeneration == ActiveRules.Generation && stones == _stonesSwitch)
            {
                return;
            }
            _data = data;
            _source = data?.m_customData;
            _rulesGeneration = ActiveRules.Generation;
            _stonesSwitch = stones;
            Glows = Decide(data, stones, out Color32 color);
            Color = color;
            if (!Glows)
            {
                RemoveGlow();
            }
        }

        /// <summary>Turns the light (and, with <paramref name="beam"/>, the loot beam) on with the current preferences.</summary>
        public void Shine(float intensity, float range, bool beam)
        {
            ShineLight(intensity, range);
            if (beam)
            {
                ShowBeam();
            }
            else
            {
                RemoveBeam();
            }
        }

        // Each value is written only when it differs (rule 14): a resting drop's light is left alone.
        private void ShineLight(float intensity, float range)
        {
            if (_light == null)
            {
                _light = GlowLights.Create(Drop.transform);
            }
            Transform at = _light.transform;
            Vector3 position = Drop.transform.position + Vector3.up * LightHeight;
            if (at.position != position)
            {
                at.position = position;
            }
            Color color = Color;
            if (_light.color != color)
            {
                _light.color = color;
            }
            SetLevels(_light, intensity, range);
        }

        private static void SetLevels(Light light, float intensity, float range)
        {
            if (light.intensity != intensity)
            {
                light.intensity = intensity;
            }
            if (light.range != range)
            {
                light.range = range;
            }
            if (!light.enabled)
            {
                light.enabled = true;
            }
        }

        // The beam is made in the item's color the first time, made again when the color changed, and stood upright on
        // every tick (a dropped item rolls; the beam is its child).
        private void ShowBeam()
        {
            if (_beam != null && !_beamColor.Equals(Color))
            {
                RemoveBeam();
            }
            if (_beam == null)
            {
                _beam = GlowBeams.Create(Drop.transform, Color);
                _beamColor = Color;
            }
            if (_beam != null)
            {
                GlowBeams.Upright(_beam.transform);
                if (!_beam.activeSelf)
                {
                    _beam.SetActive(true);
                }
            }
        }

        /// <summary>Over the nearest-N cap: the light and the beam are switched off, not destroyed.</summary>
        public void Dim()
        {
            if (_light != null)
            {
                _light.enabled = false;
            }
            if (_beam != null)
            {
                _beam.SetActive(false);
            }
        }

        /// <summary>The light and the beam are destroyed (the item stopped glowing, or the glow was switched off).</summary>
        public void RemoveGlow()
        {
            if (_light != null)
            {
                UnityEngine.Object.Destroy(_light.gameObject);
            }
            _light = null;
            RemoveBeam();
        }

        private void RemoveBeam()
        {
            if (_beam != null)
            {
                UnityEngine.Object.Destroy(_beam);
            }
            _beam = null;
        }

        /// <summary>Magic items glow in their rarity color when the rarity says <c>glow</c> (never the base rarity);
        /// stones glow in their tint only with <c>Glow stones</c> on.</summary>
        private static bool Decide(ItemDrop.ItemData? data, bool stones, out Color32 color)
        {
            color = default;
            if (data == null)
            {
                return false;
            }
            if (ItemClasses.IsStone(data))
            {
                // Tint from the Items area; a rune without one glows white.
                color = stones ? (Color32)StoneVisuals.TintOfPrefab(ItemTier.PrefabName(data)) : default;
                return stones;
            }
            RarityDef? rarity = ItemState.Read(data).Rarity;
            if (rarity == null || rarity.IsBase || !rarity.Glow)
            {
                return false;
            }
            color = rarity.Color32;
            return true;
        }
    }
}
