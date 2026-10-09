using System;
using System.Collections.Generic;
using UnityEngine;

namespace EliteEquipment.Boots
{
    /// <summary>
    /// The leggings' own data maps under their body paint (<c>_LegsMetal</c>, <c>_LegsBumpMap</c>) with the trousers'
    /// paint edits (<see cref="NativeSplit"/>) copied into them too, every channel (the metal map's alpha is its gloss),
    /// at the map's own size: paint carried down to the ankle keeps the metal and relief of what it was copied from (the
    /// Embla mail, carried over the brown leather, came out flat white with the leather's maps). Null for a set without
    /// edits or map. Read through the GPU the first time a client needs it, and kept; none on a dedicated server.
    /// </summary>
    internal static class PaintMaps
    {
        public const string Prefix = "EE_LegsMap_";
        public static readonly string[] Properties = { "_LegsMetal", "_LegsBumpMap" };

        private static readonly Dictionary<(BootSet, string), Texture2D> made = new Dictionary<(BootSet, string), Texture2D>();

        public static Texture2D For(BootSet set, string property)
        {
            if (!made.TryGetValue((set, property), out Texture2D map))
                made[(set, property)] = map = Guarded(set, property);
            return map;
        }

        /// <summary>The map the game would draw for these leggings: their own, or none.</summary>
        public static Texture Game(int legsHash, string property)
        {
            GameObject prefab = legsHash != 0 && ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(legsHash) : null;
            Material paint = prefab != null ? prefab.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_armorMaterial : null;
            return paint != null && paint.HasProperty(property) ? paint.GetTexture(property) : null;
        }

        private static Texture2D Guarded(BootSet set, string property)
        {
            try
            {
                return Make(set, property);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"EliteEquipment: the {set.Key} leggings keep their own {property}: {e.Message}");
                return null;
            }
        }

        private static Texture2D Make(BootSet set, string property)
        {
            SplitData data = NativeSplit.For(set.Key);
            Texture source = data != null && data.Edits.Count > 0 && data.PaintSize > 0 ? Game(set.LegsHash, property) : null;
            Paint native = Paint.ReadLinear(source);
            if (native == null)
                return null;
            Paint map = native.Copy();
            foreach (PaintEdit edit in data.Edits)
                Apply(map, native, edit, native.Width / (float)data.PaintSize, native.Height / (float)data.PaintSize);
            return map.ToTexture(Prefix + set.Key + property);
        }

        /// <summary>One edit at the map's scale: the texel taken whole (c, f) or blended toward (b).</summary>
        private static void Apply(Paint map, Paint native, PaintEdit edit, float sx, float sy)
        {
            int target = map.Index(Scaled(edit.X, sx, map.Width), Scaled(edit.Y, sy, map.Height));
            Color32 to = native.Pixels[native.Index(Scaled(edit.SourceX, sx, map.Width), Scaled(edit.SourceY, sy, map.Height))];
            map.Pixels[target] = edit.Kind == 'b' ? Color32.Lerp(map.Pixels[target], to, edit.Weight) : to;
        }

        private static int Scaled(int texel, float scale, int size) => Mathf.Clamp((int)(texel * scale), 0, size - 1);
    }
}
