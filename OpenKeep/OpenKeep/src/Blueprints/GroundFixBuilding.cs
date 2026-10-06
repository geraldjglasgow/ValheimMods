using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Blueprints
{
    /// <summary>One building piece as Fix ground sees it: its drawn footprint on the ground (a turned rectangle) and its bottom.</summary>
    public sealed class FixPiece
    {
        public Piece Piece;
        public Vector3 Centre;
        public Vector3 Across;
        public Vector3 Along;
        public Vector2 Half;
        public float Bottom;
        public float Height;

        /// <summary>The world rectangle (x, z) holding the footprint.</summary>
        public Rect Box => Rect.MinMaxRect(Centre.x - Reach, Centre.z - Reach, Centre.x + Reach, Centre.z + Reach);

        public float Reach => Mathf.Sqrt(Half.x * Half.x + Half.y * Half.y);

        public bool Covers(float x, float z)
        {
            Vector3 d = new Vector3(x - Centre.x, 0f, z - Centre.z);
            return Mathf.Abs(Vector3.Dot(d, Across)) <= Half.x && Mathf.Abs(Vector3.Dot(d, Along)) <= Half.y;
        }
    }

    /// <summary>
    /// The building under the crosshair: the player-built piece looked at, and every piece joined to it through pieces
    /// whose drawn boxes touch (within a few centimetres), up to <see cref="BlueprintRules.FixMaxPieces"/> pieces within
    /// <see cref="BlueprintRules.FixReach"/> m; for Copy also everything inside it (<see cref="WithInside"/>). A box is
    /// the piece's drawn shape (<see cref="PieceShapes"/>) turned with the piece, so a turned wall is not mistaken for a
    /// wide one.
    /// </summary>
    public static class GroundFixBuilding
    {
        private const float Touch = 0.15f;
        private const float Cell = 4f;

        /// <summary>Rounds of "what is inside, then what touches that" at most (furniture on a rug on a floor is two).</summary>
        private const int InsideRounds = 4;
        private static int pieceMask;

        /// <summary>The player-built piece under the crosshair, or null.</summary>
        public static Piece Aimed()
        {
            GameCamera camera = GameCamera.instance;
            if (pieceMask == 0)
                pieceMask = LayerMask.GetMask("piece", "piece_nonsolid", "Default", "static_solid");
            if (camera == null || !Physics.Raycast(camera.transform.position, camera.transform.forward, out RaycastHit hit, BlueprintRules.AimRange, pieceMask))
                return null;
            Piece piece = hit.collider.GetComponentInParent<Piece>();
            return piece != null && piece.IsPlacedByPlayer() && !BlueprintMenu.IsOurs(piece) ? piece : null;
        }

        /// <summary>The piece and every piece joined to it through touching boxes.</summary>
        public static List<FixPiece> From(Piece start) => Grow(Gather(start), start, inside: false);

        /// <summary>
        /// The building as <see cref="From"/> finds it and everything inside it: a piece whose centre lies within the
        /// footprint of one of the building's pieces (under its roofs, on its floors) and between its lowest bottom and
        /// highest top joins it though it touches nothing of it, and so does what touches that piece in turn.
        /// </summary>
        public static List<FixPiece> WithInside(Piece start) => Grow(Gather(start), start, inside: true);

        /// <summary>Every player-built piece within reach of the start, in a grid of cells (the distance test first: a big base loads tens of thousands of pieces).</summary>
        private static Dictionary<long, List<FixPiece>> Gather(Piece start)
        {
            Dictionary<long, List<FixPiece>> grid = new Dictionary<long, List<FixPiece>>();
            Vector3 origin = start.transform.position;
            float reach = BlueprintRules.FixReach * BlueprintRules.FixReach;
            foreach (Piece piece in Piece.s_allPieces)
            {
                if (piece != null && FlatSqr(piece.transform.position - origin) <= reach && piece.IsPlacedByPlayer() && !BlueprintMenu.IsOurs(piece))
                    Add(grid, Describe(piece));
            }
            return grid;
        }

        /// <summary>Floods from the start; with <paramref name="inside"/>, adds what lies inside and floods on from it until nothing is added.</summary>
        private static List<FixPiece> Grow(Dictionary<long, List<FixPiece>> grid, Piece start, bool inside)
        {
            HashSet<Piece> seen = new HashSet<Piece> { start };
            List<FixPiece> building = new List<FixPiece> { Describe(start) };
            int from = 0;
            for (int round = 0; round < InsideRounds; round++)
            {
                Flood(grid, building, seen, from);
                from = building.Count;
                if (inside)
                    GroundFixInside.Add(grid, building, seen);
                if (building.Count == from)
                    break;
            }
            return building;
        }

        /// <summary>
        /// Adds every piece touching a piece of the building from index <paramref name="from"/> on, and what touches those.
        /// A piece is marked seen only once it joins: a piece that does not touch one piece may still touch another.
        /// </summary>
        private static void Flood(Dictionary<long, List<FixPiece>> grid, List<FixPiece> building, HashSet<Piece> seen, int from)
        {
            for (int next = from; next < building.Count && building.Count < BlueprintRules.FixMaxPieces; next++)
            {
                foreach (FixPiece other in Near(grid, building[next]))
                {
                    if (!seen.Contains(other.Piece) && Touching(building[next], other))
                    {
                        seen.Add(other.Piece);
                        building.Add(other);
                    }
                }
            }
        }

        /// <summary>A piece's footprint and height from its drawn bounds (a one-metre box when it draws nothing).</summary>
        public static FixPiece Describe(Piece piece)
        {
            PieceShape shape = PieceShapes.Of(Utils.GetPrefabName(piece.gameObject));
            Bounds b = shape?.Template != null ? shape.Bounds : new Bounds(new Vector3(0f, 0.5f, 0f), Vector3.one);
            Transform t = piece.transform;
            Quaternion yaw = Quaternion.Euler(0f, t.eulerAngles.y, 0f);
            return new FixPiece
            {
                Piece = piece, Centre = t.position + yaw * new Vector3(b.center.x, 0f, b.center.z),
                Across = yaw * Vector3.right, Along = yaw * Vector3.forward, Half = new Vector2(b.extents.x, b.extents.z),
                Bottom = t.position.y + b.min.y, Height = b.size.y,
            };
        }

        /// <summary>Two boxes touch: their heights overlap and their footprints come within <see cref="Touch"/> (tested as circles and rectangles).</summary>
        private static bool Touching(FixPiece a, FixPiece b)
        {
            if (a.Bottom > b.Bottom + b.Height + Touch || b.Bottom > a.Bottom + a.Height + Touch)
                return false;
            if (Flat(a.Centre - b.Centre) > a.Reach + b.Reach + Touch)
                return false;
            return a.Covers(b.Centre.x, b.Centre.z) || b.Covers(a.Centre.x, a.Centre.z) || Separation(a, b) <= Touch;
        }

        /// <summary>How far apart the two turned rectangles are along the four axes (a separating-axis estimate; 0 or less when they overlap).</summary>
        private static float Separation(FixPiece a, FixPiece b)
        {
            float gap = float.MinValue;
            foreach (Vector3 axis in new[] { a.Across, a.Along, b.Across, b.Along })
            {
                float d = Mathf.Abs(Vector3.Dot(b.Centre - a.Centre, axis));
                gap = Mathf.Max(gap, d - Extent(a, axis) - Extent(b, axis));
            }
            return gap;
        }

        private static float Extent(FixPiece p, Vector3 axis) => Mathf.Abs(Vector3.Dot(p.Across, axis)) * p.Half.x + Mathf.Abs(Vector3.Dot(p.Along, axis)) * p.Half.y;

        internal static void Add(Dictionary<long, List<FixPiece>> grid, FixPiece p)
        {
            long key = Key(Mathf.FloorToInt(p.Centre.x / Cell), Mathf.FloorToInt(p.Centre.z / Cell));
            if (!grid.TryGetValue(key, out List<FixPiece> list))
                grid[key] = list = new List<FixPiece>();
            list.Add(p);
        }

        /// <summary>The pieces whose centres lie in the cells this piece's reach (plus the longest piece's) can touch.</summary>
        internal static IEnumerable<FixPiece> Near(Dictionary<long, List<FixPiece>> grid, FixPiece p)
        {
            int span = Mathf.CeilToInt((p.Reach + 6f) / Cell);
            int cx = Mathf.FloorToInt(p.Centre.x / Cell), cz = Mathf.FloorToInt(p.Centre.z / Cell);
            for (int x = cx - span; x <= cx + span; x++)
            {
                for (int z = cz - span; z <= cz + span; z++)
                {
                    if (grid.TryGetValue(Key(x, z), out List<FixPiece> list))
                        foreach (FixPiece other in list)
                            yield return other;
                }
            }
        }

        private static long Key(int x, int z) => ((long)x << 32) ^ (uint)z;

        private static float Flat(Vector3 v) => Mathf.Sqrt(FlatSqr(v));

        private static float FlatSqr(Vector3 v) => v.x * v.x + v.z * v.z;
    }
}
