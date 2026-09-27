using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Preview
{
    /// <summary>
    /// One footprint as the engine evaluates it: the engine's own <see cref="Terrain.Footprint"/> (built by
    /// <see cref="StrokeParams.From"/>, so the sizes are clamped and normalized exactly as the owner will apply them:
    /// rectangle depth 0 means a square, ring inner edge at most 5 cm inside the rim, frame band 0 means a filled
    /// square, the paint footprint scaled by paint radius over radius), plus the centre height and the turn the outline
    /// needs. Circles and rings do not turn, as in the engine.
    /// </summary>
    internal struct FootprintSpec
    {
        public Terrain.Footprint Print;
        public float CenterY;
        public float Rotation;

        public static FootprintSpec Height(StrokeParams p) => Of(p.HeightPrint, p.Stroke);

        public static FootprintSpec Paint(StrokeParams p) => Of(p.PaintPrint, p.Stroke);

        private static FootprintSpec Of(Terrain.Footprint print, BrushStroke stroke)
        {
            bool turns = print.Shape == BrushShape.Square || print.Shape == BrushShape.Rectangle || print.Shape == BrushShape.Frame;
            return new FootprintSpec { Print = print, CenterY = stroke.Center.y, Rotation = turns ? stroke.Rotation : 0f };
        }

        public Vector3 Center => new Vector3(Print.CenterX, CenterY, Print.CenterZ);

        public bool Cornered => Print.Shape == BrushShape.Square || Print.Shape == BrushShape.Rectangle || Print.Shape == BrushShape.Frame;

        /// <summary>Half extents along the footprint's own u (turned X) and v (turned Z) axes.</summary>
        public float HalfU => Print.Outer;

        public float HalfV => Print.Shape == BrushShape.Rectangle ? Print.Inner : Print.Outer;

        /// <summary>Ring: the inner radius; frame: the inner half side (outer minus the band); 0 when there is no hole.</summary>
        public float Hole
        {
            get
            {
                if (Print.Shape == BrushShape.Ring)
                    return Print.Inner;
                return Print.Shape == BrushShape.Frame ? Print.Outer - Print.Inner : 0f;
            }
        }

        /// <summary>The engine would touch this world point (weight above 0; includes its "nearest vertex" rule).</summary>
        public bool Covers(float x, float z) => Print.Weight(x, z) > 0f;

        public bool SameAs(FootprintSpec other)
        {
            Terrain.Footprint a = Print, b = other.Print;
            return a.Shape == b.Shape && Mathf.Abs(a.CenterX - b.CenterX) < 0.01f && Mathf.Abs(a.CenterZ - b.CenterZ) < 0.01f
                && Mathf.Abs(CenterY - other.CenterY) < 0.01f && Mathf.Abs(a.Outer - b.Outer) < 0.001f && Mathf.Abs(a.Inner - b.Inner) < 0.001f
                && Mathf.Abs(Rotation - other.Rotation) < 0.01f;
        }
    }
}
