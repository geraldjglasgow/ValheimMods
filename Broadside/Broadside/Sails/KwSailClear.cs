using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A square sail's cloth kept off its mast whatever moves it (<see cref="KwSailMotion"/>: the wind backing it, its
    /// billows and luffing, the ship's swing, its hauled-up bags): every point that would come nearer the mast's axis than
    /// the mast's radius and <see cref="Margin"/> is pushed out in front of it along the cloth's own forward (the way it
    /// bellies), never round it, so cloth blown back onto the mast lies on its front. The push eases in over
    /// <see cref="Feather"/> and the cloth beside the mast drapes back from it on a smooth curve, so nothing creases. A band
    /// as wide as the mast and a little more than the cloth's widest face either side is held in front whole, so no face
    /// between two points beside the mast cuts through it either, nor through the climbing pegs; the bolt rope, on the
    /// cloth's edge, keeps its own half width less. Worked in the cloth's own axes (Unity's),
    /// where the mast's axis stands upright on the cloth's middle line; the bolt rope's points are kept the same way, so it
    /// stays on the cloth's edge. The mast's radius is taken at the cloth's lowest point, where it is thickest. A sail along
    /// the ship or a jib needs none: its cloth only moves to its sides (<see cref="KwSailPoints"/>), behind the mast by the
    /// mast's radius and 5 cm at its luff. Made once a hoist; nothing is made while it keeps.
    /// </summary>
    public sealed class KwSailClear
    {
        /// <summary>The least room kept between the cloth and the mast's surface, and how far out past that the push eases in.</summary>
        public const float Margin = 0.04f, Feather = 0.04f;

        // How far past the mast on each side the band held in front of it reaches, in the cloth's widest faces: one is the
        // least that keeps every face clear (a face touching the mast's width has all its corners in the band), and a quarter
        // more for the motion drawing the faces a little wider.
        private const float Faces = 1.25f;

        private readonly float axis, clear, band, drape;

        private KwSailClear(float axis, float clear, float span)
        {
            (this.axis, this.clear) = (axis, clear);
            (band, drape) = (clear + Faces * span, Mathf.Max(span, 0.05f));
        }

        /// <summary>The mast's axis in the cloth's own axes: its z (it stands behind the cloth's origin).</summary>
        public float Axis => axis;

        /// <summary>The least distance kept from the mast's axis: its radius at the cloth's lowest point and the margin.</summary>
        public float Clear => clear;

        /// <summary>
        /// The keeping for a square sail's cloth at a hoist, from its mesh's vertices slack and full and its triangles (the
        /// same for both); none for a sail along the ship or a jib.
        /// </summary>
        public static KwSailClear? Of(KwSailLayout s, Vector3[] slack, Vector3[] full, int[] triangles)
        {
            if (!s.Square || slack.Length == 0 || full.Length != slack.Length)
                return null;
            Vector3 origin = s.ClothOrigin;
            float low = origin.z + Mathf.Min(Lowest(slack), Lowest(full));
            float span = Mathf.Max(Span(slack, triangles), Span(full, triangles));
            return new KwSailClear(-origin.x, KwMastTops.RadiusAt(s.Mast, low) + Margin, span);
        }

        /// <summary>Points of the cloth (its own axes) kept clear of the mast, in place.</summary>
        public void Keep(Vector3[] points)
        {
            float near = clear + Feather;
            for (int i = 0; i < points.Length; i++)
            {
                float front = points[i].z - axis;
                if (front < near)
                    points[i].z = axis + Kept(front, Mathf.Abs(points[i].x));
            }
        }

        // How far in front of the mast's axis a point stands once kept, from how far it stood and how far to its side: as
        // it was, or eased out onto the floor (the mast's front in the band, curving back beside it), never under it.
        private float Kept(float front, float side)
        {
            float past = side - band, floor = past <= 0f ? clear : clear - past * past / (2f * drape);
            float over = front - floor;
            return over >= Feather ? front : floor + Feather * Mathf.Exp((over - Feather) / Feather);
        }

        // The lowest a point stands (Unity's y, the kit's up).
        private static float Lowest(Vector3[] points)
        {
            float low = float.MaxValue;
            foreach (Vector3 p in points)
                low = Mathf.Min(low, p.y);
            return low;
        }

        // The widest any face reaches across the cloth (Unity's x, the kit's across).
        private static float Span(Vector3[] points, int[] triangles)
        {
            float most = 0f;
            for (int t = 0; t + 2 < triangles.Length; t += 3)
            {
                float a = points[triangles[t]].x, b = points[triangles[t + 1]].x, c = points[triangles[t + 2]].x;
                most = Mathf.Max(most, Mathf.Max(a, Mathf.Max(b, c)) - Mathf.Min(a, Mathf.Min(b, c)));
            }
            return most;
        }
    }
}
