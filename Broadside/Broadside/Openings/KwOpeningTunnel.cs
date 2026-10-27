using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// An opening's straight tunnel through a hull's planking or a cabin's raised side (the user, 2026-10-25, of round ports
    /// near the bow: "They should point straight out of the ship, and instead they're almost a diagonal drill through the
    /// ship"). The wall's own depth goes along its sections' normal (<see cref="KwPlankSurface.Fibre"/>), square to the hull
    /// only where the hull runs straight fore and aft; where it turns toward a stem the two part by up to 60 degrees, and an
    /// opening cut along it was a slanting bore. A tunnel goes one way, <see cref="Way"/>: square to the hull's face at the
    /// opening's middle (<see cref="KwOpeningSurface.Out"/>). A point at a depth is the wall's face point carried
    /// along that way as far as reaches the face the wall's own depth reaches (the same distance square to the face), so the
    /// planking's faces, the frames' stand-off and their clearances keep their measures and only where a depth lands along
    /// the face changes. The cut (<see cref="KwOpeningRing"/>), lining, frames and leaves all go by it; on a side running
    /// straight fore and aft it is the wall itself to within millimetres. Measures along and up, the reach and the heights
    /// are the wall's. Kit axes of the part. Pure.
    /// </summary>
    public sealed class KwOpeningTunnel : KwOpeningSurface
    {
        /// <summary>The least share of a depth's way that still crosses the face (a face turned nearly along the tunnel).</summary>
        private const float Grazing = 0.2f;

        public KwOpeningTunnel(KwPlankSurface face, KwOpening o) : base(face.Wall)
        {
            Face = face;
            Way = Square(face, o);
        }

        /// <summary>The wall the opening is cut in.</summary>
        public KwPlankSurface Face { get; }

        /// <summary>The tunnel's way out (kit, a unit).</summary>
        public Vector3 Way { get; }

        /// <summary>How much of the way the tunnel bends from the wall's own depth: 1, less where its ring has no room.</summary>
        public float Strength { get; private set; } = 1f;

        /// <summary>How far round the cut the strakes turn from the tunnel's way to the wall's own (metres on the face; 0 none).</summary>
        public float Ring { get; private set; }

        /// <summary>The ring and the strength its room allows (<see cref="KwOpeningRings"/>).</summary>
        public void Size(float ring, float strength) => (Ring, Strength) = (ring, Mathf.Clamp01(strength));

        public override float Thick => Face.Thick;
        public override float Proud => Face.Proud;
        public override (float From, float To) Reach => Face.Reach;
        public override (float Lo, float Hi) Range(float u) => Face.Range(u);
        public override float Rise(float u, float z, float metres) => Face.Rise(u, z, metres);
        public override Vector2 Upright(float u, float z, float metres) => Face.Upright(u, z, metres);
        public override float Run(float u, float z, float metres) => Face.Run(u, z, metres);
        public override float Metres(float u0, float u1, float z) => Face.Metres(u0, u1, z);

        public override Vector3 Point(float u, float z, float depth)
        {
            Vector3 at = Face.Point(u, z, 0f);
            return Mathf.Abs(depth) < 1e-6f ? at : at + Fibre(u, z) * depth;
        }

        /// <summary>The tunnel's way out, wherever on the wall.</summary>
        public override Vector3 Out(float u, float z) => Way;

        /// <summary>What a depth is multiplied by at a point of the wall: along the tunnel, as far as the wall's own unit depth reaches.</summary>
        public Vector3 Fibre(float u, float z) => Bend(Face.Fibre(u, z), Face.Out(u, z), 1f);

        /// <summary>
        /// A wall's depth way at a point (any length; normal, the wall's own way out there) bent a share of the way to the
        /// tunnel's, so a depth along it still reaches the same face: the share times <see cref="Strength"/>.
        /// </summary>
        public Vector3 Bend(Vector3 fibre, Vector3 normal, float share)
        {
            Vector3 tunnel = Way * (Vector3.Dot(fibre, normal) / Mathf.Max(Vector3.Dot(Way, normal), Grazing));
            return fibre + (tunnel - fibre) * (share * Strength);
        }

        // The way square to the face at an opening's middle (half its height up its middle line, kept on the wall), outward.
        private static Vector3 Square(KwPlankSurface face, KwOpening o)
        {
            Vector2 mid = face.Fit(o, new Vector2(o.U, o.Sill + o.Height / 2f));
            (float lo, float hi) = face.Range(mid.x);
            mid.y = Mathf.Clamp(mid.y, lo, hi);
            Vector3 n = face.Out(mid.x, mid.y);
            return n.sqrMagnitude > 0.5f ? n : face.Fibre(mid.x, mid.y);
        }
    }
}
