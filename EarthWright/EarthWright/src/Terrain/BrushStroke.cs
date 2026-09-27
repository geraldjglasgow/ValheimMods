using UnityEngine;

namespace EarthWright.Terrain
{
    /// <summary>
    /// One application of the brush, as sent to every terrain compiler it touches. The owner of each compiler evaluates
    /// it against its own current terrain, so relative operations (raise, smooth) stay correct under lag. All heights
    /// are absolute world heights, all lengths metres, the rotation degrees about the vertical axis.
    /// </summary>
    public sealed class BrushStroke
    {
        /// <summary>Centre of the footprint. Y is the reference height (the ghost's height); level uses <see cref="Target"/>.</summary>
        public Vector3 Center;

        public BrushShape Shape = BrushShape.Circle;

        /// <summary>Circle and ring outer radius, square and frame half side, rectangle half width.</summary>
        public float Radius = 2f;

        /// <summary>Rectangle half depth, ring inner radius, frame band width. Unused by circle and square.</summary>
        public float Radius2;

        public float Rotation;

        /// <summary>0 = the effect fades from the centre to the rim; 1 = full effect everywhere inside (hard edge).</summary>
        public float Hardness = 0.5f;

        public HeightOp Height = HeightOp.None;

        public LevelStyle Style = LevelStyle.Ease;

        /// <summary>Absolute world height for Level, SetMin and SetMax.</summary>
        public float Target;

        /// <summary>Metres for Raise, Lower and Offset.</summary>
        public float Amount = 1f;

        /// <summary>Largest height change per vertex in one stroke; 0 = unlimited.</summary>
        public float MaxStep = 1f;

        /// <summary>0..1, for Smooth.</summary>
        public float Strength = 0.5f;

        public PaintOp Paint = PaintOp.None;

        /// <summary>Paint footprint radius (same shape as the height footprint); 0 = <see cref="Radius"/>.</summary>
        public float PaintRadius;

        /// <summary>0..1, how strongly the paint replaces what is there.</summary>
        public float PaintStrength = 1f;

        /// <summary>0..1, the vegetation alpha for <see cref="PaintOp.Vegetation"/>.</summary>
        public float Density = 1f;

        public float BandMin;

        public float BandMax;

        /// <summary>0..1, the share of covered vertices affected (admin filter); 1 = all.</summary>
        public float RandomShare = 1f;

        /// <summary>Seed for <see cref="RandomShare"/>, so every compiler picks the same vertices on a shared edge.</summary>
        public int Seed;

        /// <summary>The paint radius actually used.</summary>
        public float EffectivePaintRadius => PaintRadius > 0f ? PaintRadius : Radius;

        /// <summary>A radius around <see cref="Center"/> that contains every vertex and paint cell the stroke may touch.</summary>
        public float Reach
        {
            get
            {
                float size = Mathf.Max(Mathf.Max(Radius, Radius2), EffectivePaintRadius);
                bool cornered = Shape == BrushShape.Square || Shape == BrushShape.Rectangle || Shape == BrushShape.Frame;
                return (cornered ? size * 1.415f : size) + 1.5f;
            }
        }
    }
}
