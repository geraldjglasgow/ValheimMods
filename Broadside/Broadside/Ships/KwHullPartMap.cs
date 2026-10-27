using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Where a launched ship's hull parts lie, for its holes: points over the surface of every planking part (each station's
    /// outline keel to sheer, both sides, and outlines mixed between stations, and a flat stern's transom board), every deck
    /// (across its top) and every
    /// cabin (its raised sides, its own walls and its roof, <see cref="KwHullPartCabins"/>), each with the way the surface
    /// faces there and its part (the piece's index in the ship's blueprint), in the ship's frame; at most
    /// <see cref="Step"/> apart. From the parts' seated spans and texts, as their meshes are made, so a dedicated server
    /// has it too.
    /// A hit goes to the point whose surface it is nearest to and faces it best: its distance plus <see cref="Facing"/>
    /// times how far the surface turns from square to the hit's way (a shot along a ship's side at a deck's height holes
    /// the side, not the deck's edge inside it; a shot falling on a deck holes the deck). On a hull's planking or a cabin's
    /// raised side, upright there, the hole is put on the outside line at the hit's place along and height and faces
    /// square out of the face there (<see cref="KwOpeningSurface.Out"/>, as an opening's tunnel does), whatever way the hit
    /// came; elsewhere (a flat bottom, a deck, a wall, a roof) it is moved from the point onto the surface's plane there (at
    /// most <see cref="Slide"/>). Nothing within <see cref="Far"/> of the hit, no part.
    /// </summary>
    public sealed class KwHullPartMap
    {
        private const float Step = 0.4f, Slide = 0.3f, Far = 1.2f, Facing = 0.6f, Upright = 0.7f, Moved = 0.6f, Turned = 0.7f;
        private static readonly float[] Sides = { -1f, 1f };
        private readonly List<KwHullPoint> points = new List<KwHullPoint>();

        public int Count => points.Count;

        public static KwHullPartMap Of(KwBlueprint blueprint)
        {
            KwHullPartMap map = new KwHullPartMap();
            foreach ((KwBpPiece piece, string tool, KwSpan span) in KwBpHull.Parts(blueprint))
            {
                int index = blueprint.Pieces.IndexOf(piece);
                if (tool == "planking")
                {
                    map.Planking(index, piece, span.Stations());
                    map.Transoms(index, piece, span.Def("planking"));
                }
                else if (tool == "deck")
                    map.Deck(index, piece, span.Def("deck"));
            }
            for (int i = 0; i < blueprint.Pieces.Count; i++)
                KwHullPartCabins.Add(map, i, blueprint.Pieces[i]);
            return map;
        }

        /// <summary>
        /// The part a hit at a point of the ship's frame holes, coming along way (the ship's frame; zero when not known), and
        /// the place on its surface there and the way out of the surface.
        /// </summary>
        public bool Nearest(Vector3 local, Vector3 way, out int part, out Vector3 place, out Vector3 normal) =>
            Nearest(local, way, 0f, out part, out place, out normal);

        /// <summary>
        /// The same for a face struck whose own normal is known (a box of the ship's, in its frame): only the points whose
        /// surface turns from it by less than <see cref="Turned"/> (a side's box is the planking, not the deck's edge beside
        /// it; a deck's slab the deck, not the side), any when none near does.
        /// </summary>
        public bool Struck(Vector3 local, Vector3 face, out int part, out Vector3 place, out Vector3 normal) =>
            Nearest(local, face, Turned, out part, out place, out normal) || Nearest(local, face, 0f, out part, out place, out normal);

        private bool Nearest(Vector3 local, Vector3 way, float least, out int part, out Vector3 place, out Vector3 normal)
        {
            (part, place, normal) = (-1, local, Vector3.up);
            int found = Best(local, way.sqrMagnitude > 1e-6f ? way.normalized : Vector3.zero, least);
            if (found < 0)
                return false;
            KwHullPoint p = points[found];
            part = p.Part;
            if (Onto(p, local, out place, out normal))
                return true;
            normal = p.Normal;
            Vector3 off = local - p.At;
            place = p.At + Vector3.ClampMagnitude(off - normal * Vector3.Dot(off, normal), Slide);
            return true;
        }

        // The point within Far whose surface a hit is nearest to and faces best (and at least least square to way); -1 with none.
        private int Best(Vector3 local, Vector3 way, float least)
        {
            (int found, float best) = (-1, float.MaxValue);
            for (int i = 0; i < points.Count; i++)
            {
                float sq = (points[i].At - local).sqrMagnitude, square = Mathf.Abs(Vector3.Dot(points[i].Normal, way));
                if (sq >= Far * Far || square < least)
                    continue;
                float score = Mathf.Sqrt(sq) + Facing * (1f - square);
                if (score < best)
                    (best, found) = (score, i);
            }
            return found;
        }

        // On an upright planking or raised side: the outside line at the hit's place along and height, square out of the
        // face there; false elsewhere, or where that lies far from the point found.
        private static bool Onto(KwHullPoint p, Vector3 local, out Vector3 place, out Vector3 normal)
        {
            (place, normal) = (local, p.Normal);
            if (!(p.Face is KwPlankSurface face) || Mathf.Abs(p.Normal.y) > Upright)
                return false;
            Quaternion back = new Quaternion(-p.Rotation.x, -p.Rotation.y, -p.Rotation.z, p.Rotation.w);
            Vector3 kit = KwMesh.ToKit(back * (local - p.Position));
            (float from, float to) = face.Reach;
            float u = Mathf.Clamp(kit.x, from + 0.01f, to - 0.01f);
            (float lo, float hi) = face.Range(u);
            float z = Mathf.Clamp(kit.z, lo, hi);
            Vector3 n = face.Out(u, z);
            if (n.sqrMagnitude < 0.5f)
                return false;
            (place, normal) = (p.Position + p.Rotation * KwMesh.ToUnity(face.Point(u, z, 0f)), (p.Rotation * KwMesh.ToUnity(n)).normalized);
            return (place - p.At).magnitude <= Moved;
        }

        // Every station's outline, and outlines mixed between two stations as far apart as Step (when they pair point for point).
        private void Planking(int index, KwBpPiece piece, List<Station> stations)
        {
            KwPlankSurface[] faces = { new KwPlankSurface(KwOpening.Starboard, stations, -1f), new KwPlankSurface(KwOpening.Port, stations, 1f) };
            for (int k = 0; k < stations.Count; k++)
            {
                Station a = stations[k];
                Outline(index, piece, a.X, a.Half, faces);
                if (k + 1 >= stations.Count || stations[k + 1].Half.Length != a.Half.Length)
                    continue;
                Station b = stations[k + 1];
                int cuts = Mathf.CeilToInt(Mathf.Abs(b.X - a.X) / Step);
                for (int c = 1; c < cuts; c++)
                    Outline(index, piece, Mathf.Lerp(a.X, b.X, c / (float)cuts), Mix(a.Half, b.Half, c / (float)cuts), faces);
            }
        }

        // One outline's points, bottom to top, every segment cut to Step, on both sides, facing out of the hull.
        private void Outline(int index, KwBpPiece piece, float x, Vector2[] half, KwPlankSurface[] faces)
        {
            for (int j = 0; j + 1 < half.Length; j++)
            {
                Vector2 a = half[j], run = half[j + 1] - a;
                if (run.sqrMagnitude < 1e-6f)
                    continue;
                Vector2 outward = new Vector2(run.y, -run.x).normalized;
                int cuts = Mathf.Max(1, Mathf.CeilToInt(run.magnitude / Step));
                for (int c = 0; c <= cuts; c++)
                {
                    Vector2 p = a + run * (c / (float)cuts);
                    for (int s = 0; s < 2; s++)
                        Put(index, piece, new Vector3(x, Sides[s] * p.x, p.y), new Vector3(0f, Sides[s] * outward.x, outward.y), faces[s]);
                }
            }
        }

        // A flat stern's transom board (its first or last station closed, KwPlanks.TransomBoard): level lines across it from
        // its keel to its sheer, Step apart, facing out along the part.
        private void Transoms(int index, KwBpPiece piece, KwDef def)
        {
            foreach ((bool flat, Station st, float outward) in new[] { (def.FlatFore, def.Stations[def.Stations.Count - 1], 1f), (def.FlatAft, def.Stations[0], -1f) })
            {
                if (!flat)
                    continue;
                float keel = st.Half[0].y, sheer = st.Half[st.Half.Length - 1].y;
                int rows = Mathf.Max(1, Mathf.CeilToInt((sheer - keel) / Step));
                for (int r = 0; r <= rows; r++)
                {
                    float z = Mathf.Lerp(keel, sheer, r / (float)rows), half = KwPlankSurface.Across(st.Half, z);
                    int cuts = Mathf.Max(1, Mathf.CeilToInt(2f * half / Step));
                    for (int c = 0; c <= cuts; c++)
                        Put(index, piece, new Vector3(st.X, Mathf.Lerp(-half, half, c / (float)cuts), z), new Vector3(outward, 0f, 0f), null);
                }
            }
        }

        // A deck's top: lines across it at every point of its edge and between them, Step apart, facing up.
        private void Deck(int index, KwBpPiece piece, KwDef def)
        {
            System.Func<float, float> z = KwMesh.DeckHeight(def);
            List<Vector2> edge = KwShapes.DeckEdge(def.Stations, z, KwShapes.InsideWidth);
            for (int k = 0; k < edge.Count; k++)
            {
                Across(index, piece, edge[k].x, -edge[k].y, edge[k].y, z(edge[k].x));
                if (k + 1 >= edge.Count)
                    continue;
                int cuts = Mathf.CeilToInt(Mathf.Abs(edge[k + 1].x - edge[k].x) / Step);
                for (int c = 1; c < cuts; c++)
                {
                    Vector2 mid = Vector2.Lerp(edge[k], edge[k + 1], c / (float)cuts);
                    Across(index, piece, mid.x, -mid.y, mid.y, z(mid.x));
                }
            }
        }

        /// <summary>A level line of points across a part (kit y from right to left at height), Step apart, facing up.</summary>
        public void Across(int index, KwBpPiece piece, float x, float right, float left, float height)
        {
            int cuts = Mathf.Max(1, Mathf.CeilToInt((left - right) / Step));
            for (int c = 0; c <= cuts; c++)
                Put(index, piece, new Vector3(x, Mathf.Lerp(right, left, c / (float)cuts), height), new Vector3(0f, 0f, 1f), null);
        }

        /// <summary>A point and its facing in the piece's kit frame, placed as the piece is in the ship, with the face it lies on.</summary>
        public void Put(int index, KwBpPiece piece, Vector3 kit, Vector3 facing, KwOpeningSurface? face) =>
            points.Add(new KwHullPoint(index, piece.Position + piece.Rotation * KwMesh.ToUnity(kit), (piece.Rotation * KwMesh.ToUnity(facing)).normalized,
                face, piece.Position, piece.Rotation));

        /// <summary>The step between points over a surface.</summary>
        public static float Spacing => Step;

        private static Vector2[] Mix(Vector2[] a, Vector2[] b, float w)
        {
            Vector2[] mixed = new Vector2[a.Length];
            for (int i = 0; i < a.Length; i++)
                mixed[i] = Vector2.Lerp(a[i], b[i], w);
            return mixed;
        }
    }

    /// <summary>One point of a <see cref="KwHullPartMap"/>: its part, place and facing in the ship's frame, the face it lies on (null on a flat deck or roof) and its piece's place.</summary>
    public readonly struct KwHullPoint
    {
        public KwHullPoint(int part, Vector3 at, Vector3 normal, KwOpeningSurface? face, Vector3 position, Quaternion rotation) =>
            (Part, At, Normal, Face, Position, Rotation) = (part, at, normal, face, position, rotation);

        public int Part { get; }
        public Vector3 At { get; }
        public Vector3 Normal { get; }
        public KwOpeningSurface? Face { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
    }
}
