using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A carved head (<see cref="KwCapHead"/>) fitted to its post as the code's caps are (<see cref="KwCapForm"/>): its neck
    /// from the foot's top to its head_start stretched by the cap's length and bent about its own middle by the lean
    /// (<see cref="KwCapBend"/>), the head above moved and turned whole with the neck's top; then the foot's underside scaled to the post's
    /// section (x to its width, z to its depth, flush) and everything from the foot's top up by the cap's size
    /// (<see cref="KwCapSpec.Size"/>), blended over the foot's moulding; last, turned half round about the post for a cap
    /// that faces inboard. Each corner's normal is carried by the warp's own derivatives (the cofactors of its Jacobian), its
    /// tangent by the Jacobian, so the shading and the normal map follow the bend and the stretch. Pure arithmetic: a copy
    /// is made, the head's model is never changed.
    /// </summary>
    public sealed class KwCapWarp
    {
        private readonly float footX, footZ, size;
        private readonly KwCapBend bend;
        private readonly bool inboard;

        public KwCapWarp(KwCapSpec spec, KwCapHead head)
        {
            size = spec.Size;
            (footX, footZ, inboard) = (spec.Width / KwCapHead.FootWidth, spec.Depth / KwCapHead.FootDepth, spec.Inboard);
            bend = new KwCapBend(KwCapHead.FootTop, head.Start, spec.Lean, spec.Length, head.Middle);
        }

        /// <summary>The head's corners, normals and tangents warped onto the post (new arrays).</summary>
        public (Vector3[] Vertices, Vector3[] Normals, Vector4[] Tangents) Apply(KwCapHead head)
        {
            int count = head.Vertices.Length;
            Vector3[] vertices = new Vector3[count], normals = new Vector3[count];
            Vector4[] tangents = new Vector4[head.Tangents.Length == count ? count : 0];
            for (int i = 0; i < count; i++)
            {
                KwCapJet jet = At(head.Vertices[i]);
                vertices[i] = jet.Point;
                normals[i] = i < head.Normals.Length ? jet.Normal(head.Normals[i]) : Vector3.up;
                if (tangents.Length > 0)
                    tangents[i] = jet.Tangent(head.Tangents[i]);
            }
            return (vertices, normals, tangents);
        }

        /// <summary>Where a corner of the head goes, with the warp's derivatives there.</summary>
        public KwCapJet At(Vector3 p)
        {
            KwCapJet jet = Scaled(Bent(p), p.y);
            return inboard ? jet.Turned() : jet;
        }

        // The neck stretched and bent in the plane of up (y) and forward (z), the head moved whole; x across untouched.
        private KwCapJet Bent(Vector3 p)
        {
            Vector2 at = bend.At(p.y, p.z, out Vector2 dUp, out Vector2 dForward);
            return new KwCapJet(new Vector3(p.x, at.x, at.y), Vector3.right, new Vector3(0f, dUp.x, dUp.y), new Vector3(0f, dForward.x, dForward.y));
        }

        // The foot's underside scaled to the post's section, the rest by the cap's size, blended over the moulding by the
        // corner's own height as carved (from).
        private KwCapJet Scaled(KwCapJet jet, float from)
        {
            float foot = KwCapHead.FootTop, w = Mathf.Clamp01(from / foot), rate = from > 0f && from < foot ? 1f / foot : 0f;
            Vector3 s = new Vector3(Mathf.Lerp(footX, size, w), size, Mathf.Lerp(footZ, size, w));
            Vector3 grow = new Vector3((size - footX) * rate * jet.Point.x, 0f, (size - footZ) * rate * jet.Point.z);
            return new KwCapJet(Vector3.Scale(jet.Point, s), Vector3.Scale(jet.Dx, s), Vector3.Scale(jet.Dy, s) + grow, Vector3.Scale(jet.Dz, s));
        }
    }

    /// <summary>A warped point with the warp's derivatives there: how it moves with the original x, y and z (the Jacobian's columns).</summary>
    public readonly struct KwCapJet
    {
        public KwCapJet(Vector3 point, Vector3 dx, Vector3 dy, Vector3 dz)
        {
            Point = point;
            (Dx, Dy, Dz) = (dx, dy, dz);
        }

        public Vector3 Point { get; }
        public Vector3 Dx { get; }
        public Vector3 Dy { get; }
        public Vector3 Dz { get; }

        /// <summary>A point not yet moved: its derivatives the axes.</summary>
        public static KwCapJet Plain(Vector3 p) => new KwCapJet(p, Vector3.right, Vector3.up, Vector3.forward);

        /// <summary>Turned half round about the upright through the origin (x and z negated), derivatives too.</summary>
        public KwCapJet Turned()
        {
            static Vector3 Half(Vector3 v) => new Vector3(-v.x, v.y, -v.z);
            return new KwCapJet(Half(Point), Half(Dx), Half(Dy), Half(Dz));
        }

        /// <summary>A surface normal carried through the warp: by the Jacobian's cofactors (its inverse transposed, scaled).</summary>
        public Vector3 Normal(Vector3 n) => (n.x * Vector3.Cross(Dy, Dz) + n.y * Vector3.Cross(Dz, Dx) + n.z * Vector3.Cross(Dx, Dy)).normalized;

        /// <summary>A tangent carried along the surface by the Jacobian; its sign kept (the warp never mirrors).</summary>
        public Vector4 Tangent(Vector4 t)
        {
            Vector3 way = (t.x * Dx + t.y * Dy + t.z * Dz).normalized;
            return new Vector4(way.x, way.y, way.z, t.w);
        }
    }
}
