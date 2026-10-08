using System;
using System.Collections.Generic;
using PlateColumn;
using UnityEngine;

namespace OpenKeep.Boots
{
    /// <summary>
    /// A leggings' body paint (<c>m_armorMaterial</c>'s <c>_LegsTex</c>) cut into the trousers' and the boots' paint at the
    /// workshop's ankle row (<see cref="NativeSplit"/>), each the game's texels with the other's rows cleared; the
    /// trousers then get the workshop's edits that carry them down to the ankle (Bronze, Leather, Troll leather), each a
    /// texel taken from elsewhere in the same paint. The game's rags have no footwear: the rag shoes wear the workshop's own
    /// paint instead (<see cref="OwnBoots"/>, cloth foot-wraps in the rags' colours). Read through the GPU the first time a
    /// client needs it, and kept; null
    /// for a set that paints nothing, on a dedicated server, or when the game's paint is not the size the split expects.
    /// </summary>
    internal static class PaintCut
    {
        private const string Resources = "OpenKeep.assets.boots.";
        private static readonly int LegsTex = Shader.PropertyToID("_LegsTex");
        private static readonly Dictionary<BootSet, Paint[]> cut = new Dictionary<BootSet, Paint[]>();

        public static Paint Trousers(BootSet set) => Parts(set)?[0];

        public static Paint Boots(BootSet set) => Parts(set)?[1];

        private static Paint[] Parts(BootSet set)
        {
            if (!cut.TryGetValue(set, out Paint[] parts))
                cut[set] = parts = Guarded(set);
            return parts;
        }

        private static Paint[] Guarded(BootSet set)
        {
            try
            {
                return Make(set, NativeSplit.For(set.Key));
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"OpenKeep: the {set.Key} leggings keep their whole paint: {e.Message}");
                return null;
            }
        }

        private static Paint[] Make(BootSet set, SplitData data)
        {
            Texture source = Source(set);
            Paint native = data != null && data.PaintSize > 0 && source != null ? Paint.Read(source) : null;
            if (native == null)
                return null;
            if (native.Width != data.PaintSize || native.Height != data.PaintSize)
            {
                Plugin.Log.LogWarning($"OpenKeep: the {set.Legs} paint is {native.Width}x{native.Height}, not {data.PaintSize}; it stays whole");
                return null;
            }
            Paint trousers = Rows(native, 0, data.PaintBoundary);
            Edit(trousers, native, data.Edits);
            Paint boots = OwnBoots(set) ?? (data.PaintBoundary < native.Height ? Rows(native, data.PaintBoundary, native.Height) : null);
            return new[] { trousers, boots };
        }

        /// <summary>
        /// Boots paint the workshop drew for a set whose leggings have none (<c>assets/boots/paint_boots_&lt;set&gt;.png</c>,
        /// the rags' only: ValheimAssets <c>NativeSplit_v001/Revisions/Rag_v002</c>), in the body's UV; null for the others.
        /// </summary>
        private static Paint OwnBoots(BootSet set)
        {
            string name = "paint_boots_" + set.Key.ToLowerInvariant();
            if (typeof(PaintCut).Assembly.GetManifestResourceInfo(Resources + name + ".png") == null)
                return null;
            Sprite sprite = EmbeddedSprite.Load(typeof(PaintCut).Assembly, Resources + name + ".png", "OpenKeep_" + name);
            return sprite != null ? Paint.Read(sprite.texture) : null;
        }

        private static Texture Source(BootSet set)
        {
            Material paint = set.LegsPrefab != null ? set.LegsPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_armorMaterial : null;
            return paint != null && paint.HasProperty(LegsTex) ? paint.GetTexture(LegsTex) : null;
        }

        /// <summary>A copy that keeps the texel rows from <paramref name="from"/> to before <paramref name="to"/>, counted from the top.</summary>
        private static Paint Rows(Paint native, int from, int to)
        {
            Paint part = native.Copy();
            for (int y = 0; y < native.Height; y++)
            {
                if (y >= from && y < to)
                    continue;
                for (int x = 0; x < native.Width; x++)
                    part.Pixels[native.Index(x, y)].a = 0;
            }
            return part;
        }

        private static void Edit(Paint trousers, Paint native, List<PaintEdit> edits)
        {
            foreach (PaintEdit edit in edits)
            {
                if (edit.Kind != 'b')
                    Take(trousers, native, edit);
            }
            foreach (PaintEdit edit in edits)
            {
                if (edit.Kind == 'b')
                    Blend(trousers, native, edit);
            }
        }

        private static void Take(Paint trousers, Paint native, PaintEdit edit)
        {
            int target = trousers.Index(edit.X, edit.Y);
            Color32 source = native.Pixels[native.Index(edit.SourceX, edit.SourceY)];
            byte alpha = edit.Kind == 'f' ? (byte)255 : trousers.Pixels[target].a;
            trousers.Pixels[target] = new Color32(source.r, source.g, source.b, alpha);
        }

        private static void Blend(Paint trousers, Paint native, PaintEdit edit)
        {
            int target = trousers.Index(edit.X, edit.Y);
            Color32 from = trousers.Pixels[target];
            Color32 to = native.Pixels[native.Index(edit.SourceX, edit.SourceY)];
            float w = edit.Weight;
            trousers.Pixels[target] = new Color32((byte)Mathf.RoundToInt(from.r + (to.r - from.r) * w),
                (byte)Mathf.RoundToInt(from.g + (to.g - from.g) * w), (byte)Mathf.RoundToInt(from.b + (to.b - from.b) * w), from.a);
        }
    }
}
