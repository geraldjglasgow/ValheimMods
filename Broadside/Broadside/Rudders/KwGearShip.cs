using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A ship's pieces as its steering gear is laid out (<see cref="KwRudderGearFind"/>), all in its rudder's kit frame (x
    /// forward from the post's after face, y left, z up from the rudder point): its decks (each its outline, its height
    /// along it and its hatch openings, <see cref="KwDeckHoles"/>), its hull's inside (the planking parts' stations, less
    /// the planks' thickness), what stands in the way under a deck (a mast stepped on a lower deck, a ladder hung from a
    /// hatch: upright columns; its keels and posts, and a cabin on a lower deck: <see cref="Solid"/>) and its captain's
    /// wheels. Built from blueprint pieces, a launched ship's or the dock's read the same way
    /// (<see cref="KwRudderGearDock"/>), with plain maths only, so every peer works out the same from the same pieces.
    /// </summary>
    public sealed class KwGearShip
    {
        private const float MastRadius = 0.35f, LadderRadius = 0.45f;
        private static readonly int SpanHash = KwGenerated.Key.GetStableHashCode();

        private readonly List<KwGearDeck> decks = new List<KwGearDeck>();
        private readonly List<KwGearHull> hulls = new List<KwGearHull>();
        private readonly List<(Vector2 At, float Foot, float Top, float Radius)> columns = new List<(Vector2, float, float, float)>();
        private readonly List<KwGearKeel> keels = new List<KwGearKeel>();
        private readonly List<KwGearCabin> cabins = new List<KwGearCabin>();

        /// <summary>The ship round a rudder: its pieces in any one frame, the rudder among them.</summary>
        public KwGearShip(List<KwBpPiece> pieces, KwBpPiece rudder)
        {
            Quaternion back = Back(rudder.Rotation);
            List<(Vector3 Middle, Vector3[] Corners)> hatches = Hatches(pieces);
            foreach (KwBpPiece piece in pieces)
                Take(piece, new KwGearPose(back * (piece.Position - rudder.Position), back * piece.Rotation), hatches);
        }

        /// <summary>The ship's captain's wheels: each one's place (kit) and its turn on the rudder's frame.</summary>
        public List<(Vector3 At, Quaternion Turn)> Wheels { get; } = new List<(Vector3 At, Quaternion Turn)>();

        /// <summary>
        /// The ship's cabins, and what only a wheel on a cabin's roof looks at (<see cref="KwGearRooms"/>): its planking parts
        /// (for their openings) and anchor windlasses, each placed in the rudder's frame with its saved text.
        /// </summary>
        internal List<KwGearCabin> Cabins => cabins;
        internal List<(KwGearPose Pose, string Prefab, string Saved)> Others { get; } = new List<(KwGearPose Pose, string Prefab, string Saved)>();

        /// <summary>A unit rotation's inverse, worked out without the engine.</summary>
        public static Quaternion Back(Quaternion q) => new Quaternion(-q.x, -q.y, -q.z, q.w);

        /// <summary>
        /// The top of the deck over a point (kit x, y) that lies between two heights, the one nearest a height; null where
        /// none does (off every deck, or over a hatch opening widened by the margin).
        /// </summary>
        public float? Top(float x, float y, float near, float from, float to, float margin = 0f)
        {
            float? best = null;
            foreach (KwGearDeck deck in decks)
            {
                float? top = deck.Top(x, y, margin);
                if (top.HasValue && top.Value >= from && top.Value <= to && (best == null || Mathf.Abs(top.Value - near) < Mathf.Abs(best.Value - near)))
                    best = top;
            }
            return best;
        }

        /// <summary>The top of a deck within a height of a level over a point; null where there is none.</summary>
        public float? TopNear(float x, float y, float level, float within) => Top(x, y, level, level - within, level + within);

        /// <summary>How far out from the centre line the hull's inside is at a point's place along the ship and height (0 off the hull).</summary>
        public float Inside(Vector3 kit)
        {
            float widest = 0f;
            foreach (KwGearHull hull in hulls)
                widest = Mathf.Max(widest, hull.Width(kit));
            return widest;
        }

        /// <summary>Whether a straight run from a to b passes by a mast or a ladder standing at its height.</summary>
        public bool Blocked(Vector3 a, Vector3 b)
        {
            float low = Mathf.Min(a.z, b.z), high = Mathf.Max(a.z, b.z);
            foreach ((Vector2 at, float foot, float top, float radius) in columns)
            {
                if (high >= foot && low <= top && Across(at, new Vector2(a.x, a.y), new Vector2(b.x, b.y)) < radius)
                    return true;
            }
            return false;
        }

        /// <summary>Whether a point lies in a keel or its post, or in a cabin, within a margin of them.</summary>
        public bool Solid(Vector3 p, float margin)
        {
            foreach (KwGearKeel keel in keels)
            {
                if (keel.Holds(p, margin))
                    return true;
            }
            return cabins.Exists(c => c.Holds(p, margin));
        }

        // How far a point is from the segment p to q, across the ship (seen from above).
        private static float Across(Vector2 at, Vector2 p, Vector2 q)
        {
            Vector2 run = q - p;
            float t = run.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(at - p, run) / run.sqrMagnitude);
            return (p + run * t - at).magnitude;
        }

        // One piece: a deck, a planking part, a mast, a ladder, a keel, a cabin or a captain's wheel; anything else is passed by.
        private void Take(KwBpPiece piece, KwGearPose pose, List<(Vector3 Middle, Vector3[] Corners)> hatches)
        {
            if (KwWheel.Is(piece.Prefab))
                Wheels.Add((KwMesh.ToKit(pose.Position), pose.Turn));
            string saved = KwBpData.Text(piece.Data, SpanHash) ?? "";
            string tool = piece.Prefab.StartsWith(KwToolPieces.PartPrefix, System.StringComparison.Ordinal) ? piece.Prefab.Substring(KwToolPieces.PartPrefix.Length) : "";
            if (tool == "planking" || KwAnchor.Is(piece.Prefab))
                Others.Add((pose, piece.Prefab, saved));
            Vector3 foot = KwMesh.ToKit(pose.Position);
            if (tool == KwSailTool.Tool && Mast(saved, out float height))
                columns.Add((new Vector2(foot.x, foot.y), foot.z, foot.z + height, MastRadius));
            else if (tool == KwLadderTool.Tool && KwLadder.TryLength(saved, out float length))
                columns.Add((new Vector2(foot.x, foot.y), foot.z - length, foot.z, LadderRadius));
            else if ((tool == "deck" || tool == "planking") && KwSpan.TryParse(saved, out KwSpan span))
                Hull(piece, pose, tool, span, hatches);
            else if (tool == "keel" && KwKeelSpec.TryParse(saved, out KwKeelSpec keel))
                keels.Add(new KwGearKeel(pose, keel));
            else if (KwHouses.Owns(tool) && KwCabinSpec.TryParse(saved, out KwCabinSpec cabin))
                cabins.Add(new KwGearCabin(pose, cabin));
        }

        private void Hull(KwBpPiece piece, KwGearPose pose, string tool, KwSpan span, List<(Vector3 Middle, Vector3[] Corners)> hatches)
        {
            if (tool == "planking")
            {
                hulls.Add(new KwGearHull(pose, span.Stations()));
                return;
            }
            KwDef def = span.Def(tool);
            Quaternion back = Back(piece.Rotation);
            List<Rect> holes = KwDeckHoles.Of(p => back * (p - piece.Position), def, hatches);
            decks.Add(new KwGearDeck(pose, def, holes));
        }

        // A mast's height, the first number of its text ("mast|height|...").
        private static bool Mast(string saved, out float height)
        {
            string[] p = saved.Split('|');
            height = 0f;
            return p.Length > 1 && p[0] == "mast" && float.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out height) && height > 0f;
        }

        // Every hatch's middle and its opening's corners, in the pieces' frame (as a launched ship's are, KwShipDeckHoles).
        private static List<(Vector3 Middle, Vector3[] Corners)> Hatches(List<KwBpPiece> pieces)
        {
            List<(Vector3, Vector3[])> hatches = new List<(Vector3, Vector3[])>();
            foreach (KwBpPiece piece in pieces)
            {
                Vector2? opening = KwHatch.Opening(piece.Prefab);
                if (opening == null)
                    continue;
                float hx = opening.Value.x / 2f, hy = opening.Value.y / 2f;
                Vector2[] kit = { new Vector2(-hx, -hy), new Vector2(hx, -hy), new Vector2(hx, hy), new Vector2(-hx, hy) };
                Vector3[] corners = new Vector3[4];
                for (int i = 0; i < 4; i++)
                    corners[i] = piece.Position + piece.Rotation * KwMesh.ToUnity(new Vector3(kit[i].x, kit[i].y, 0f));
                hatches.Add((piece.Position, corners));
            }
            return hatches;
        }
    }

    /// <summary>A piece's place and turn in a rudder's frame (Unity axes), and points moved between the two frames' kit axes.</summary>
    public readonly struct KwGearPose
    {
        public KwGearPose(Vector3 position, Quaternion turn) => (Position, Turn) = (position, turn);

        public Vector3 Position { get; }
        public Quaternion Turn { get; }

        /// <summary>A point of the rudder's kit frame in the piece's own kit frame.</summary>
        public Vector3 Local(Vector3 kit) => KwMesh.ToKit(KwGearShip.Back(Turn) * (KwMesh.ToUnity(kit) - Position));

        /// <summary>A point of the piece's own kit frame in the rudder's kit frame.</summary>
        public Vector3 Out(Vector3 kit) => KwMesh.ToKit(Turn * KwMesh.ToUnity(kit) + Position);
    }

    /// <summary>A deck as the gear sees it: its outline along it, its height and its hatch openings, in its own kit frame.</summary>
    internal sealed class KwGearDeck
    {
        private readonly KwGearPose pose;
        private readonly System.Func<float, float> z;
        private readonly List<Vector2> edge;
        private readonly List<Rect> holes;

        public KwGearDeck(KwGearPose pose, KwDef def, List<Rect> holes)
        {
            (this.pose, this.holes) = (pose, holes);
            z = KwMesh.DeckHeight(def);
            edge = def.Edge ?? KwShapes.DeckEdge(def.Stations, z, KwShapes.InsideWidth);
        }

        /// <summary>The deck's top over a point of the rudder's frame (kit x, y), as a kit height there; null off it or over an opening.</summary>
        public float? Top(float x, float y, float margin)
        {
            Vector3 k = pose.Local(new Vector3(x, y, 0f));
            if (Mathf.Abs(k.y) > Half(k.x))
                return null;
            foreach (Rect hole in holes)
            {
                if (k.x > hole.xMin - margin && k.x < hole.xMax + margin && k.y > hole.yMin - margin && k.y < hole.yMax + margin)
                    return null;
            }
            return pose.Out(new Vector3(k.x, k.y, z(k.x))).z;
        }

        // The deck's half width at x along it; 0 past its ends.
        private float Half(float x)
        {
            for (int i = 0; i + 1 < edge.Count; i++)
            {
                Vector2 a = edge[i], b = edge[i + 1];
                if (x >= Mathf.Min(a.x, b.x) && x <= Mathf.Max(a.x, b.x))
                    return Mathf.Abs(b.x - a.x) < 1e-4f ? Mathf.Max(a.y, b.y) : Mathf.Lerp(a.y, b.y, (x - a.x) / (b.x - a.x));
            }
            return 0f;
        }
    }

    /// <summary>A planking part as the gear sees it: its stations' insides along it, in its own kit frame.</summary>
    internal sealed class KwGearHull
    {
        private readonly KwGearPose pose;
        private readonly float[] at;
        private readonly Vector2[][] inside;

        public KwGearHull(KwGearPose pose, List<Station> stations)
        {
            this.pose = pose;
            at = new float[stations.Count];
            inside = new Vector2[stations.Count][];
            for (int i = 0; i < stations.Count; i++)
                (at[i], inside[i]) = (stations[i].X, KwPlanks.Inside(stations[i].Half));
        }

        /// <summary>How far out the hull's inside is at a point's place along the part and height (0 off the part).</summary>
        public float Width(Vector3 kit)
        {
            Vector3 k = pose.Local(kit);
            for (int i = 0; i + 1 < at.Length; i++)
            {
                if (k.x < Mathf.Min(at[i], at[i + 1]) || k.x > Mathf.Max(at[i], at[i + 1]))
                    continue;
                float t = Mathf.Abs(at[i + 1] - at[i]) < 1e-4f ? 0f : (k.x - at[i]) / (at[i + 1] - at[i]);
                return Mathf.Lerp(KwShapes.WidthAt(inside[i], k.z), KwShapes.WidthAt(inside[i + 1], k.z), t);
            }
            return 0f;
        }
    }
}
