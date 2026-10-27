using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A hole's ragged outline and its frame (the user, 2026-10-26: "Instead of a black spot there, can you literally have
    /// a hole?"): square to its part's face at the hole's place (<see cref="KwHullHole.Normal"/>, the way out of the face
    /// there), its axes along the planks (the ship's length laid on the face; level across it on a bulkhead) and up the
    /// face. The outline is a rough round of 13 to 16 corners, from the hole's own number (<see cref="KwHullHole.Id"/>), so
    /// every peer cuts the same: its edge uneven, split further along the grain here and there, notched where a broken
    /// plank end juts into the hole, a little longer along the grain than across it, about the hole's own area. Splinters
    /// stand at the notches (<see cref="Splinters"/>); the widest passage straight through, for the colliders, keeps
    /// within the outline (<see cref="Inner"/>). Pure: the same moved into any frame (<see cref="In"/>).
    /// </summary>
    public sealed class KwHoleShape
    {
        private const float Grain = 1.15f;

        private KwHoleShape(Vector3 centre, Vector3 normal, Vector3 along, Vector3 up, KwHoleShape? like)
        {
            (Centre, Normal, Along, Up) = (centre, normal, along, up);
            if (like == null)
                return;
            (Outline, Splinters, Seed, Radius, Reach, Inner) = (like.Outline, like.Splinters, like.Seed, like.Radius, like.Reach, like.Inner);
        }

        public Vector3 Centre { get; }
        public Vector3 Normal { get; }
        public Vector3 Along { get; }
        public Vector3 Up { get; }

        /// <summary>The ragged outline in (along, up) metres from the middle, anticlockwise.</summary>
        public List<Vector2> Outline { get; private set; } = new List<Vector2>();

        /// <summary>The outline's corners (notches) a splinter stands at.</summary>
        public List<int> Splinters { get; private set; } = new List<int>();

        public int Seed { get; private set; }
        public float Radius { get; private set; }

        /// <summary>How far the outline's furthest corner lies from the middle.</summary>
        public float Reach { get; private set; }

        /// <summary>How far the outline's nearest edge lies from the middle.</summary>
        public float Inner { get; private set; }

        public static KwHoleShape Of(KwHullHole hole)
        {
            Vector3 n = hole.Normal.sqrMagnitude > 1e-6f ? hole.Normal.normalized : Vector3.up;
            Vector3 along = Vector3.ProjectOnPlane(Vector3.forward, n);
            if (along.sqrMagnitude < 0.1f)
                along = Vector3.Cross(Vector3.up, n);
            along.Normalize();
            KwHoleShape shape = new KwHoleShape(hole.Place, n, along, Vector3.Cross(n, along).normalized, null)
                { Seed = hole.Id, Radius = hole.Radius };
            shape.Outline = Ragged(shape.Seed, hole.Radius, shape.Splinters);
            shape.Reach = shape.Outline.ConvertAll(p => p.magnitude).Max();
            shape.Inner = Inradius(shape.Outline);
            return shape;
        }

        /// <summary>The shape in the frame of something standing at origin turned by turn (in the shape's present frame).</summary>
        public KwHoleShape In(Vector3 origin, Quaternion turn)
        {
            Quaternion back = new Quaternion(-turn.x, -turn.y, -turn.z, turn.w);
            return new KwHoleShape(back * (Centre - origin), back * Normal, back * Along, back * Up, this);
        }

        /// <summary>The shape carried by a rigid move (a point's and a way's).</summary>
        public KwHoleShape Moved(System.Func<Vector3, Vector3> point, System.Func<Vector3, Vector3> way) =>
            new KwHoleShape(point(Centre), way(Normal).normalized, way(Along).normalized, way(Up).normalized, this);

        /// <summary>A point of the hole's plane, depth out of the face along its normal.</summary>
        public Vector3 Point(Vector2 at, float depth) => Centre + Along * at.x + Up * at.y + Normal * depth;

        /// <summary>Where a point lies in the hole's plane, and how far out of the face.</summary>
        public Vector2 Flat(Vector3 p) => new Vector2(Vector3.Dot(p - Centre, Along), Vector3.Dot(p - Centre, Up));

        public float Depth(Vector3 p) => Vector3.Dot(p - Centre, Normal);

        // The break: a rough round, split further where the planks' grain runs (along), with notches where a broken plank
        // end juts in; turned and jittered by the seed, stretched along the grain.
        private static List<Vector2> Ragged(int seed, float radius, List<int> splinters)
        {
            int corners = 13 + Mathf.Min(3, (int)(Noise(seed, 99) * 4f));
            float turn = Noise(seed, 98) * 2f * Mathf.PI;
            List<Vector2> outline = new List<Vector2>(corners);
            for (int i = 0; i < corners; i++)
            {
                float angle = turn + 2f * Mathf.PI * (i + 0.35f * (Noise(seed, i) - 0.5f)) / corners;
                float reach = Corner(seed, i, Mathf.Abs(Mathf.Cos(angle)));
                outline.Add(new Vector2(Mathf.Cos(angle) * Grain, Mathf.Sin(angle) / Grain) * (radius * reach));
                if (reach < 0.8f)
                    splinters.Add(i);
            }
            return outline;
        }

        // How far one corner of the break reaches, in the hole's radii: rough round it, a split along the grain (where the
        // corner points along the planks) half the time, a notch a quarter of the time elsewhere.
        private static float Corner(int seed, int i, float along)
        {
            float reach = 0.88f + 0.24f * Noise(seed + 1, i), roll = Noise(seed + 2, i);
            if (along > 0.8f && roll > 0.5f)
                return reach + 0.25f + 0.3f * Noise(seed + 3, i);
            return roll < 0.25f ? reach * (0.66f + 0.1f * Noise(seed + 4, i)) : reach;
        }

        // The nearest any edge of an outline comes to its middle.
        private static float Inradius(List<Vector2> outline)
        {
            float least = float.MaxValue;
            for (int i = 0; i < outline.Count; i++)
            {
                Vector2 a = outline[i], b = outline[(i + 1) % outline.Count], run = b - a;
                float t = run.sqrMagnitude > 1e-12f ? Mathf.Clamp01(-Vector2.Dot(a, run) / run.sqrMagnitude) : 0f;
                least = Mathf.Min(least, (a + run * t).magnitude);
            }
            return least;
        }

        /// <summary>A steady number from 0 to 1 for a seed and an index, the same on every peer.</summary>
        public static float Noise(int seed, int i)
        {
            uint h = unchecked((uint)seed * 73856093u ^ (uint)i * 19349663u);
            h = unchecked((h ^ (h >> 13)) * 0x5bd1e995u);
            return ((h ^ (h >> 15)) & 0xFFFFFFu) / 16777215f;
        }
    }
}
