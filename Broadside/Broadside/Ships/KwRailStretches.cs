using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// One stretch of a launched ship's railing, between two of its posts on one run (a hull railing's side, a roof
    /// railing's run of edges): its piece's index in the blueprint, its part (<see cref="KwRailStretches.Part"/>: run and
    /// number), where it runs along its run, and its two posts' feet on the railing's middle line in the ship's frame, with
    /// the railing's up.
    /// </summary>
    public readonly struct KwRailStretch
    {
        public KwRailStretch(int piece, int run, int index, float from, float to, Vector3 a, Vector3 b, Vector3 up) =>
            (Piece, Run, Index, From, To, A, B, Up) = (piece, run, index, from, to, a, b, up);

        public int Piece { get; }
        public int Run { get; }
        public int Index { get; }
        public int Part => KwRailStretches.Part(Run, Index);
        public float From { get; }
        public float To { get; }
        public Vector3 A { get; }
        public Vector3 B { get; }
        public Vector3 Up { get; }

        /// <summary>How far a point of the ship's frame lies from the stretch, its foot to its top rail.</summary>
        public float Distance(Vector3 point)
        {
            Vector3 run = B - A;
            float t = run.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector3.Dot(point - A, run) / run.sqrMagnitude) : 0f;
            Vector3 off = point - (A + run * t);
            float up = Mathf.Clamp(Vector3.Dot(off, Up), 0f, KwRailStretches.Height);
            return (off - Up * up).magnitude;
        }
    }

    /// <summary>
    /// Where a launched ship's railings' stretches lie (<see cref="KwRailStretch"/>), from its blueprint, plain arithmetic
    /// as the railings are drawn: a hull railing (<see cref="KwRailing"/>, seated on the ship's keels as its look is) on each
    /// side it stands on, between its posts (<see cref="KwRailing.PostsAlong"/>); a roof railing (<see cref="KwRoofRail"/>)
    /// along each run of its railed edges between its posts (<see cref="KwRoofRailShape.Posts"/>), none across a ladder's
    /// gap, a closed run's last stretch back to its first post.
    /// </summary>
    public static class KwRailStretches
    {
        /// <summary>A railing's whole height over its foot: its top rail's top.</summary>
        public const float Height = KwRailing.RailHeight + KwRailing.TopRail;

        private const int PerRun = 1000;
        private const float Join = 0.05f;

        /// <summary>A stretch's part number: its run and its number along it.</summary>
        public static int Part(int run, int index) => run * PerRun + index;

        public static bool IsRail(string prefab) => prefab == KwToolPieces.PartPrefix + KwBiomeCost.Rail;

        public static List<KwRailStretch> Of(KwBlueprint blueprint, KwKeelLines keels)
        {
            List<KwRailStretch> all = new List<KwRailStretch>();
            for (int i = 0; i < blueprint.Pieces.Count; i++)
                all.AddRange(For(i, blueprint.Pieces[i], keels));
            return all;
        }

        /// <summary>One piece's stretches (none for a piece that is not a railing), numbered as the piece at that index (a sea edit's).</summary>
        public static List<KwRailStretch> For(int index, KwBpPiece piece, KwKeelLines keels)
        {
            List<KwRailStretch> all = new List<KwRailStretch>();
            if (!IsRail(piece.Prefab))
                return all;
            string saved = KwShipParts.Saved(piece);
            if (KwRoofRail.TryParse(saved, out KwRoofRail roof))
                Roof(all, index, piece, roof);
            else if (KwSpan.TryParse(saved, out KwSpan span))
                Hull(all, index, piece, span, keels);
            return all;
        }

        /// <summary>A hull railing's stations as its look is built on the ship's keels.</summary>
        public static List<Station> Stations(KwBpPiece piece, KwSpan span, KwKeelLines keels) =>
            new KwSeatAt(keels, piece.Position, piece.Rotation * Vector3.forward).Seated(span).Def("rail").Stations;

        private static void Hull(List<KwRailStretch> all, int index, KwBpPiece piece, KwSpan span, KwKeelLines keels)
        {
            List<Station> st = Stations(piece, span, keels);
            List<float> posts = KwRailing.PostsAlong(st, KwRailText.Spacing(span.Rail)), sides = KwRailing.Drawn(KwRailText.Sides(span.Rail));
            for (int r = 0; r < sides.Count; r++)
            {
                for (int k = 0; k + 1 < posts.Count; k++)
                {
                    Vector3 a = OnShip(piece, Foot(st, posts[k], sides[r])), b = OnShip(piece, Foot(st, posts[k + 1], sides[r]));
                    all.Add(new KwRailStretch(index, r, k, posts[k], posts[k + 1], a, b, piece.Rotation * Vector3.up));
                }
            }
        }

        // A hull railing's foot on its middle line at x along one side (kit), kept just inside its ends as its posts are.
        private static Vector3 Foot(List<Station> st, float x, float side)
        {
            Vector2 e = KwRailing.EdgeAt(st, x);
            return new Vector3(Mathf.Clamp(x, st[0].X + KwRailing.Post, st[st.Count - 1].X - KwRailing.Post), side * e.x, e.y);
        }

        private static void Roof(List<KwRailStretch> all, int index, KwBpPiece piece, KwRoofRail roof)
        {
            List<KwRoofRun> runs = Runs(roof);
            for (int r = 0; r < runs.Count; r++)
            {
                List<float> posts = Posts(runs[r], roof.Spacing);
                for (int k = 0; k + 1 < posts.Count; k++)
                {
                    if (Gapped(runs[r], posts[k], posts[k + 1]))
                        continue;
                    Vector3 a = OnShip(piece, Middle(runs[r], posts[k])), b = OnShip(piece, Middle(runs[r], posts[k + 1]));
                    all.Add(new KwRailStretch(index, r, k, posts[k], posts[k + 1], a, b, piece.Rotation * Vector3.up));
                }
            }
        }

        /// <summary>A roof railing's runs, as its look lays them (<see cref="KwRoofRailShape.Shape"/>).</summary>
        public static List<KwRoofRun> Runs(KwRoofRail roof)
        {
            Vector2 sum = Vector2.zero;
            foreach (KwRoofSegment edge in roof.Edges)
                sum += (edge.A + edge.B) / 2f;
            Vector2 middle = roof.Edges.Count > 0 ? sum / roof.Edges.Count : Vector2.zero;
            return KwRoofRailShape.Runs(roof.Railed).ConvertAll(points => new KwRoofRun(points, middle, roof.Gaps));
        }

        /// <summary>A roof run's posts, a closed run's first again at its end.</summary>
        public static List<float> Posts(KwRoofRun run, float spacing)
        {
            List<float> posts = KwRoofRailShape.Posts(run, spacing);
            if (posts.Count > 0 && (run.Points[0] - run.Points[run.Points.Count - 1]).magnitude < Join)
                posts.Add(run.Length + posts[0]);
            return posts;
        }

        // Whether a ladder's gap lies between two posts (no rail runs there).
        private static bool Gapped(KwRoofRun run, float from, float to) => run.Gaps.Exists(g => g.x > from - 0.01f && g.y < to + 0.01f);

        // A roof run's middle line (half a rail in from its edge) at a place along it, the roof's top (kit z 0).
        private static Vector3 Middle(KwRoofRun run, float at)
        {
            at = run.Length > 0f && at > run.Length ? at - run.Length : at;
            int i = run.Segment(at);
            Vector2 p = run.At(i, at) + run.Inward(i) * KwRailing.Half;
            return new Vector3(p.x, p.y, 0f);
        }

        private static Vector3 OnShip(KwBpPiece piece, Vector3 kit) => piece.Position + piece.Rotation * KwMesh.ToUnity(kit);
    }
}
