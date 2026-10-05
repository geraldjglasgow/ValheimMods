using UnityEngine;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// What a blueprint paints on the ground, by the names DevBridge's blueprint files use ("dirt", "paved",
    /// "cultivated", "grass", "original", "clearvegetation"). Grass and original both clear the game's paint.
    /// </summary>
    public enum GroundPaint : byte
    {
        None = 0,
        Dirt = 1,
        Cultivated = 2,
        Paved = 3,
        Grass = 4,
        ClearVegetation = 5,
        Original = 8,
    }

    public static class GroundPaints
    {
        /// <summary>The game's paint mask colour for a paint (its alpha is the vegetation, kept unless the paint clears it).</summary>
        public static Color Colour(GroundPaint paint)
        {
            switch (paint)
            {
                case GroundPaint.Dirt: return Heightmap.m_paintMaskDirt;
                case GroundPaint.Cultivated: return Heightmap.m_paintMaskCultivated;
                case GroundPaint.Paved: return Heightmap.m_paintMaskPaved;
                case GroundPaint.ClearVegetation: return Heightmap.m_paintMaskClearVegetation;
                default: return Heightmap.m_paintMaskNothing;
            }
        }

        /// <summary>The mask after painting: the paint's colour, the vegetation alpha kept except where vegetation is cleared.</summary>
        public static Color Apply(Color before, GroundPaint paint)
        {
            Color colour = Colour(paint);
            if (paint != GroundPaint.ClearVegetation)
                colour.a = before.a;
            return colour;
        }
    }
}
