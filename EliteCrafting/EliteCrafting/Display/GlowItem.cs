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
    /// light if it has one. The color is re-decided only when the item's data object, its custom-data dictionary (the
    /// game's <c>Load</c> replaces it when the ZDO changed), the rules generation or the <c>Glow stones</c> switch
    /// changed, so a tick touches the parse cache only for new or changed items. Viewing client only.
    /// </summary>
    internal sealed class GlowItem
    {
        private const float LightHeight = 0.3f;

        private ItemDrop.ItemData? _data;
        private Dictionary<string, string>? _source;
        private int _rulesGeneration = -1;
        private bool _stonesSwitch;
        private Light? _light;

        public GlowItem(ItemDrop drop)
        {
            Drop = drop;
        }

        public ItemDrop Drop { get; }
        public int Stamp { get; set; }
        public float SqrDistance { get; set; }
        public bool Glows { get; private set; }
        public Color32 Color { get; private set; }

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
                RemoveLight();
            }
        }

        /// <summary>Turns the light on with the current preferences, creating it the first time.</summary>
        public void Shine(float intensity, float range)
        {
            if (_light == null)
            {
                _light = GlowLights.Create(Drop.transform);
            }
            _light.transform.position = Drop.transform.position + Vector3.up * LightHeight;
            _light.color = Color;
            _light.intensity = intensity;
            _light.range = range;
            _light.enabled = true;
        }

        /// <summary>Over the nearest-N cap: the light is disabled, not destroyed.</summary>
        public void Dim()
        {
            if (_light != null)
            {
                _light.enabled = false;
            }
        }

        public void RemoveLight()
        {
            if (_light != null)
            {
                UnityEngine.Object.Destroy(_light.gameObject);
            }
            _light = null;
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
            if (ItemSlots.IsStone(data))
            {
                // Tint from the Items area; a stone without one (Honing, Tempering) glows white.
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
