using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A dry dock as the blueprint station sees it: its pieces in line (keel block guides, or keel blocks alone), its
    /// frame (the aft one's), its length (from the aft piece to the end of the fore one) and the width of ground it serves.
    /// </summary>
    public sealed class KwBpDockInfo
    {
        public KwBpDockInfo(List<Piece> pieces, Transform frame, KwBpDockKind kind)
        {
            Pieces = pieces;
            Frame = frame;
            Kind = kind;
            float fore = 0f;
            foreach (Piece piece in pieces)
                fore = Mathf.Max(fore, Vector3.Dot(piece.transform.position - frame.position, frame.forward));
            Length = fore + kind.Length;
        }

        public List<Piece> Pieces { get; }
        public Transform Frame { get; }
        public KwBpDockKind Kind { get; }
        public float Length { get; }
        public float Width => Kind.Width;

        /// <summary>What the dock is made of, for the station's hover: "5 keel block guides" or "8 keel blocks", and its length.</summary>
        public string Label => $"{Pieces.Count} {(Pieces.Count == 1 ? Kind.One : Kind.Many)}, {Length:0} m";
    }

    /// <summary>
    /// The kinds of dry dock piece: the keel block guide (4 m long, serving <see cref="GuideReach"/> to each side, so a
    /// ship may be as wide as it is built), and the keel block alone (a row of them is a dock too, the blocks up to
    /// <see cref="BlockGap"/> apart, serving as the guide does).
    /// </summary>
    public sealed class KwBpDockKind
    {
        public const float GuideReach = 16f, BlockGap = 5f;
        public static readonly KwBpDockKind Guide = new KwBpDockKind("KW_dockguide", KwDock.GuideLength, 2f * GuideReach, "keel block guide", "keel block guides");
        public static readonly KwBpDockKind Block = new KwBpDockKind("KW_keelcradle", 1f, 2f * GuideReach, "keel block", "keel blocks") { Gap = BlockGap };

        private KwBpDockKind(string prefab, float length, float width, string one, string many)
        {
            Hash = prefab.GetStableHashCode();
            (Length, Width, One, Many) = (length, width, one, many);
        }

        public int Hash { get; }
        public float Length { get; }

        /// <summary>For pieces set apart rather than end to end (keel blocks): the most two in a row may be apart; 0 for end to end.</summary>
        public float Gap { get; private set; }
        public float Width { get; }
        public string One { get; }
        public string Many { get; }

        /// <summary>The kind of dock piece a piece is, or null.</summary>
        public static KwBpDockKind? Of(Piece? piece)
        {
            ZNetView? view = piece != null ? piece.m_nview : null;
            int hash = view != null && view.IsValid() ? view.GetZDO().GetPrefab() : 0;
            return hash == Guide.Hash ? Guide : hash == Block.Hash ? Block : null;
        }
    }

    /// <summary>
    /// Finding dry docks and the ships on them, from the pieces loaded on this client. A dock is a dock piece (a keel
    /// block guide; where none is near, a keel block) and every piece of its kind joined to it in a row (same heading, in
    /// line, one guide length apart, keel blocks up to 5 m apart); its frame is the aft-most of them, so a ship is saved
    /// and built from the dock's aft end whichever piece the station stands by. The ship on a dock is every ship piece (<see cref="KwBlueprintStation.IsShipPiece"/>) whose
    /// origin is over the dock: within the width it serves and a little to the sides, from just under the ground to well
    /// above a big ship's masts, and up to 6 m past either end; past that, a piece belongs to the ship when its origin lies
    /// within what the ship's other pieces reach along the dock (a long ship overhangs a short dock). Pieces are recognised by their ZDO's prefab hash, never by reading names.
    /// </summary>
    public static class KwBpDock
    {
        private const float Side = 1.5f, Below = 2f, Above = 24f, Ends = 6f, Overhang = 40f, Touch = 1.5f, Gather = 120f;
        private static readonly Dictionary<int, string> shipNames = new Dictionary<int, string>();
        private static readonly List<Piece> found = new List<Piece>();
        private static readonly List<Collider> colliders = new List<Collider>();
        private static int namesFrom = -1;

        /// <summary>The dock piece nearest the point (measured to its footprint, not its origin) within range, or null.</summary>
        public static Piece? NearestPiece(Vector3 at, float range) => Nearest(at, range, false) ?? Nearest(at, range, true);

        // The nearest keel block guide; or, asked for blocks, the nearest keel block (a dock only where no guide is).
        private static Piece? Nearest(Vector3 at, float range, bool blocks)
        {
            Piece? best = null;
            float nearest = range;
            foreach (Piece piece in Piece.s_allPieces)
            {
                KwBpDockKind? kind = KwBpDockKind.Of(piece);
                if (kind == null || (kind == KwBpDockKind.Block) != blocks)
                    continue;
                float distance = Footprint(piece.transform, kind, at);
                if (distance <= nearest)
                {
                    best = piece;
                    nearest = distance;
                }
            }
            return best;
        }

        /// <summary>The dock a station at this point serves (a few metres of slack on its range), or null.</summary>
        public static KwBpDockInfo? Find(Vector3 at)
        {
            Piece? start = NearestPiece(at, KwBlueprintStation.Range + 3f);
            if (start == null)
                return null;
            KwBpDockKind kind = KwBpDockKind.Of(start)!;
            List<Piece> pieces = Connected(start, kind);
            return new KwBpDockInfo(pieces, Aft(pieces, start.transform), kind);
        }

        /// <summary>
        /// Every ship piece over the dock: those whose origin is over it, and, further along it, those within what the
        /// first ones' shapes reach (a long ship's bow and stern overhang a short dock, a bowsprit standing on its stem's top).
        /// </summary>
        public static List<Piece> Ship(KwBpDockInfo dock)
        {
            Transform frame = dock.Frame;
            found.Clear();
            Piece.GetAllPiecesInRadius(frame.position + frame.forward * (dock.Length / 2f), dock.Length / 2f + Overhang + dock.Width + Above, found);
            List<Piece> ship = new List<Piece>(), beyond = new List<Piece>();
            foreach (Piece piece in found)
            {
                if (NameOf(piece) == null || !Across(dock, piece.transform.position))
                    continue;
                (Over(dock, piece.transform.position) ? ship : beyond).Add(piece);
            }
            found.Clear();
            if (ship.Count > 0 && beyond.Count > 0)
                Extend(dock.Frame, ship, beyond);
            return ship;
        }

        // Adds the pieces lying further along the dock whose origins are within what the ship's pieces span along it, again
        // with each piece added (a keel joined on past the first carries the bowsprit on its stem).
        private static void Extend(Transform frame, List<Piece> ship, List<Piece> beyond)
        {
            Vector2 span = Span(frame, ship, new Vector2(float.MaxValue, float.MinValue));
            while (beyond.Count > 0)
            {
                List<Piece> joined = beyond.FindAll(piece => Within(frame, piece, span));
                if (joined.Count == 0)
                    return;
                beyond.RemoveAll(joined.Contains);
                ship.AddRange(joined);
                span = Span(frame, joined, span);
            }
        }

        private static bool Within(Transform frame, Piece piece, Vector2 span)
        {
            float along = Vector3.Dot(piece.transform.position - frame.position, frame.forward);
            return along >= span.x - Touch && along <= span.y + Touch;
        }

        // The least and most the pieces' colliders reach along the dock's frame, widening a span already found.
        private static Vector2 Span(Transform frame, List<Piece> pieces, Vector2 from)
        {
            float aft = from.x, fore = from.y;
            Vector3 f = frame.forward;
            foreach (Piece piece in pieces)
            {
                piece.GetComponentsInChildren(colliders);
                foreach (Collider collider in colliders)
                {
                    Bounds b = collider.bounds;
                    float mid = Vector3.Dot(b.center - frame.position, f);
                    float half = Mathf.Abs(b.extents.x * f.x) + Mathf.Abs(b.extents.y * f.y) + Mathf.Abs(b.extents.z * f.z);
                    (aft, fore) = (Mathf.Min(aft, mid - half), Mathf.Max(fore, mid + half));
                }
            }
            colliders.Clear();
            return new Vector2(aft, fore);
        }

        /// <summary>A loaded piece's prefab name when it is a ship piece, else null.</summary>
        public static string? NameOf(Piece? piece)
        {
            ZNetView? view = piece != null ? piece.m_nview : null;
            if (view == null || !view.IsValid())
                return null;
            return ShipNames().TryGetValue(view.GetZDO().GetPrefab(), out string name) ? name : null;
        }

        // How far a point is from a dock piece's footprint (its length by the width it serves; 0 inside it); far above or
        // below counts as out of reach.
        private static float Footprint(Transform piece, KwBpDockKind kind, Vector3 p)
        {
            Vector3 local = Quaternion.Inverse(piece.rotation) * (p - piece.position);
            if (Mathf.Abs(local.y) > Above)
                return float.MaxValue;
            float across = Mathf.Max(0f, Mathf.Abs(local.x) - kind.Width / 2f);
            float along = Mathf.Max(0f, Mathf.Max(-local.z, local.z - kind.Length));
            return Mathf.Sqrt(across * across + along * along);
        }

        private static List<Piece> Connected(Piece start, KwBpDockKind kind)
        {
            List<Piece> near = new List<Piece>();
            foreach (Piece piece in Piece.s_allPieces)
            {
                if (KwBpDockKind.Of(piece) == kind && (piece.transform.position - start.transform.position).sqrMagnitude < Gather * Gather)
                    near.Add(piece);
            }
            List<Piece> chain = new List<Piece> { start };
            for (int i = 0; i < chain.Count; i++)
            {
                foreach (Piece other in near)
                {
                    if (!chain.Contains(other) && Joined(chain[i].transform, other.transform, kind))
                        chain.Add(other);
                }
            }
            return chain;
        }

        // Two dock pieces in a row: the same heading, in line, and one piece length apart (or, keel blocks, no more than their gap).
        private static bool Joined(Transform a, Transform b, KwBpDockKind kind)
        {
            Vector3 local = Quaternion.Inverse(a.rotation) * (b.position - a.position);
            float apart = Mathf.Abs(local.z);
            bool spaced = kind.Gap > 0f ? apart <= kind.Gap : Mathf.Abs(apart - kind.Length) < Mathf.Min(1f, kind.Length / 4f);
            return Quaternion.Angle(a.rotation, b.rotation) < 10f && Mathf.Abs(local.x) < 1f && Mathf.Abs(local.y) < 1.5f && spaced;
        }

        private static Transform Aft(List<Piece> pieces, Transform start)
        {
            Transform aft = start;
            foreach (Piece piece in pieces)
            {
                if (Vector3.Dot(piece.transform.position - aft.position, start.forward) < -0.5f)
                    aft = piece.transform;
            }
            return aft;
        }

        private static bool Over(KwBpDockInfo dock, Vector3 p)
        {
            Vector3 local = Quaternion.Inverse(dock.Frame.rotation) * (p - dock.Frame.position);
            return local.z >= -Ends && local.z <= dock.Length + Ends;
        }

        // Within the width the dock serves (and a little to the sides), from just under the ground to above the masts.
        private static bool Across(KwBpDockInfo dock, Vector3 p)
        {
            Vector3 local = Quaternion.Inverse(dock.Frame.rotation) * (p - dock.Frame.position);
            return Mathf.Abs(local.x) <= dock.Width / 2f + Side && local.y >= -Below && local.y <= Above;
        }

        // Prefab hash to name for every kit ship piece, made again when the kit has more pieces.
        private static Dictionary<int, string> ShipNames()
        {
            if (namesFrom == KwPrefabs.All.Count)
                return shipNames;
            namesFrom = KwPrefabs.All.Count;
            shipNames.Clear();
            foreach (GameObject prefab in KwPrefabs.All)
            {
                if (KwBlueprintStation.IsShipPiece(prefab.name))
                    shipNames[prefab.name.GetStableHashCode()] = prefab.name;
            }
            return shipNames;
        }
    }
}
