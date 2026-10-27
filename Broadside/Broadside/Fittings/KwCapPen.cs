using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Where a post cap's shapes draw (<see cref="KwCaps"/>): into a mesh builder, every point carried through the cap's
    /// form first (<see cref="KwCapForm"/>: the foot to its post's section, the carving scaled to the post and stretched
    /// along its height; the whole turned to look inboard when the cap does). A stem is made longer and bent by the cap's
    /// lean by its shape, through <see cref="Rise"/> and <see cref="Bend"/>. The caps placed with the hammer before the Post
    /// cap tool draw through no form, as they always did. Planar UVs are taken from the points as drawn, so the wood's grain
    /// keeps its size on a bigger cap. Kit axes.
    /// </summary>
    public sealed class KwCapPen
    {
        private readonly MeshBuilder b;
        private readonly Func<Vector3, Vector3> map;

        public KwCapPen(MeshBuilder b, Func<Vector3, Vector3>? map = null, float length = 1f, float lean = 0f)
        {
            this.b = b;
            this.map = map ?? (p => p);
            Length = length;
            Lean = lean;
        }

        /// <summary>How far a cap with a stem (a neck) makes it longer: 1 as carved. Its head or curl keeps its shape.</summary>
        public float Length { get; }

        /// <summary>A height on a stem rising from the foot (at foot) made longer by <see cref="Length"/>.</summary>
        public float Rise(float z, float foot) => foot + (z - foot) * Length;

        /// <summary>How far a stem leans (degrees, + toward +x): 0 as carved (<see cref="KwCapSpec.Lean"/>).</summary>
        public float Lean { get; }

        /// <summary>The bend of a stem from its foot's top to its top (as made longer) by <see cref="Lean"/>.</summary>
        public KwCapBend Bend(float foot, float top) => new KwCapBend(foot, top, Lean);

        /// <summary>A six-faced solid from its eight corners (as <see cref="Solid.Hexa(MeshBuilder, Vector3[])"/>).</summary>
        public void Hexa(Vector3[] corners) => Solid.Hexa(b, corners.Select(map).ToArray());

        public void Box(Vector3 min, Vector3 max) => Hexa(new[]
        {
            new Vector3(min.x, min.y, min.z), new Vector3(max.x, min.y, min.z), new Vector3(max.x, max.y, min.z), new Vector3(min.x, max.y, min.z),
            new Vector3(min.x, min.y, max.z), new Vector3(max.x, min.y, max.z), new Vector3(max.x, max.y, max.z), new Vector3(min.x, max.y, max.z),
        });

        /// <summary>A face with UVs of its own.</summary>
        public void Polygon(List<Vector3> kit, List<Vector2> uv) => b.Polygon(kit.ConvertAll(p => map(p)), uv);

        /// <summary>A flat face with planar UVs.</summary>
        public void Flat(List<Vector3> kit)
        {
            List<Vector3> drawn = kit.ConvertAll(p => map(p));
            b.Polygon(drawn, Solid.Planar(drawn));
        }

        /// <summary>A round log from a to c, as <see cref="Solid.Log"/> draws one, its bark along it.</summary>
        public void Log(Vector3 a, Vector3 c, float radius, int sides)
        {
            Vector3 axis = (c - a).normalized;
            Vector3 u = Vector3.Cross(axis, Mathf.Abs(axis.z) < 0.9f ? Vector3.forward : Vector3.right).normalized;
            Vector3 v = Vector3.Cross(axis, u);
            Vector3[] ring = Enumerable.Range(0, sides).Select(i => (u * Mathf.Cos(i * 2f * Mathf.PI / sides) + v * Mathf.Sin(i * 2f * Mathf.PI / sides)) * radius).ToArray();
            float length = (c - a).magnitude * 0.5f;
            for (int i = 0; i < sides; i++)
            {
                int j = (i + 1) % sides;
                float s0 = (float)i / sides, s1 = (float)(i + 1) / sides;
                Polygon(new List<Vector3> { a + ring[i], c + ring[i], c + ring[j], a + ring[j] },
                    new List<Vector2> { new Vector2(0f, s0), new Vector2(length, s0), new Vector2(length, s1), new Vector2(0f, s1) });
            }
            Flat(ring.Select(r => a + r).ToList());
            Flat(ring.Select(r => c + r).ToList());
        }
    }

    /// <summary>
    /// A post cap's form on its post: the shapes are drawn for the standard post (0.4 m across, 0.3 m along the keel, their
    /// foot <see cref="KwCaps.FootHalfX"/> by <see cref="KwCaps.FootHalfY"/>, a centimetre over it all round) and carried
    /// to the cap's base. The foot's underside takes the base's own width and depth (a centimetre over, scaled), and from
    /// the foot's top up the carving is the standard one scaled evenly by the base's size (the square root of how much
    /// wider and deeper it is than the standard post's), so it keeps its proportions; the foot between is blended from one
    /// to the other. A plain cap is a board on the post: all of it takes the base's width and depth. The length (1 for a
    /// style that makes its own stem longer, <see cref="KwCapPen.Length"/>) stretches what stands over the foot (the plain
    /// cap's board) along the height only. Pure.
    /// </summary>
    public readonly struct KwCapForm
    {
        /// <summary>The standard post the shapes are drawn for: its width across the ship and its depth along the keel.</summary>
        public const float PostWidth = 0.4f, PostDepth = 0.3f;

        // How far the foot stands over the post's sides, on the standard post.
        private const float Over = 0.01f;

        private readonly float baseX, baseY, foot, length;
        private readonly bool flat;

        public KwCapForm(float width, float depth, float length, float foot, bool flat)
        {
            Size = Of(width, depth);
            baseX = (depth / 2f + Over * Size) / KwCaps.FootHalfX;
            baseY = (width / 2f + Over * Size) / KwCaps.FootHalfY;
            (this.foot, this.length, this.flat) = (foot, length, flat);
        }

        /// <summary>The carving's scale: 1 on the standard post.</summary>
        public float Size { get; }

        /// <summary>A base's size against the standard post's (the square root of its width times its depth over theirs).</summary>
        public static float Of(float width, float depth) => Mathf.Sqrt(Mathf.Max(width, 1e-4f) / PostWidth * Mathf.Max(depth, 1e-4f) / PostDepth);

        /// <summary>A point of the standard cap carried onto this one.</summary>
        public Vector3 Map(Vector3 p)
        {
            float w = flat || foot <= 0f ? 0f : Mathf.Clamp01(p.z / foot);
            float z = p.z <= foot ? p.z : foot + (p.z - foot) * length;
            return new Vector3(p.x * Mathf.Lerp(baseX, Size, w), p.y * Mathf.Lerp(baseY, Size, w), z * Size);
        }
    }
}
