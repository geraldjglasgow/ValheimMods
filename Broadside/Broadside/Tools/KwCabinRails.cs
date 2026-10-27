using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The hull's railing a cabin would stand in (the user's requests, 2026-10-25): where a side of the cabin stands at the
    /// hull, its raised side would run through the railing on the sheer, so that side's railing along the cabin's length is
    /// taken away when it is built, or when an edit brings a side back to the hull; a side drawn in, a walkway outside it,
    /// keeps its railing. Found by prefab and place only: railing parts on the cabin's keel line whose run overlaps the
    /// cabin's own length, while the cabin's deck is lower than the railing's top; a roof's railing (its text not a hull
    /// span) is never touched. A railing part holds both sides of the hull, so a part the cabin takes one side of is built
    /// again on the other side only, and one reaching past the cabin is cut at the ribs round the cabin's ends, the railing
    /// past them kept whole (<see cref="KwRailCut"/>). The old part is taken the hammer's way
    /// (<see cref="KwUndoWork.RemoveOne"/>: a ward the player has no access to keeps it) and its pieces spawned through the
    /// tools' own spawn path with its tier and other values, all in the same undo step as the cabin (<see cref="KwUndo.Taken"/>),
    /// so undoing the cabin puts the old part back. Nothing comes back when a side is drawn in later: the railing tool builds it again.
    /// </summary>
    public static class KwCabinRails
    {
        private const float Reach = 60f, Overlap = 0.05f;
        private static readonly string Rail = KwToolPieces.PartPrefix + KwBiomeCost.Rail, Rib = KwToolPieces.PartPrefix + KwShapeTool.Rib;

        /// <summary>The railing parts the cabin standing there (its frame at, facing forward) would change.</summary>
        public static List<Piece> Under(Vector3 at, Vector3 forward, KwCabinSpec spec) => Plans(at, forward, spec).ConvertAll(plan => plan.Rail);

        /// <summary>Whether the cabin reaches the hull's railing at all: a side at the hull, its deck under the railing's top.</summary>
        public static bool Reaches(KwCabinSpec spec) =>
            (spec.Left.AtHull || spec.Right.AtHull) && spec.Floor < KwCabinOutline.Sheer(spec) + KwRailing.RailHeight + 0.08f;

        // Every railing part the cabin changes and the pieces it leaves of each.
        private static List<KwRailPlan> Plans(Vector3 at, Vector3 forward, KwCabinSpec spec)
        {
            List<KwRailPlan> plans = new List<KwRailPlan>();
            if (!Reaches(spec))
                return plans;
            List<Piece> near = new List<Piece>();
            Piece.GetAllPiecesInRadius(at, Reach, near);
            List<float> ribs = Ribs(near, at, forward);
            foreach (Piece piece in near)
            {
                KwRailPlan? plan = piece != null && Utils.GetPrefabName(piece.gameObject) == Rail ? Plan(piece, at, forward, spec, ribs) : null;
                if (plan != null)
                    plans.Add(plan);
            }
            return plans;
        }

        // Where the ribs on the cabin's keel line stand along it.
        private static List<float> Ribs(List<Piece> near, Vector3 at, Vector3 forward)
        {
            List<float> ribs = new List<float>();
            foreach (Piece piece in near)
            {
                if (piece != null && Utils.GetPrefabName(piece.gameObject) == Rib && KwCabinHull.InLine(at, forward, piece.transform.position, piece.transform.forward))
                    ribs.Add(Vector3.Dot(piece.transform.position - at, forward));
            }
            return ribs;
        }

        // A hull railing (its text a span) on the cabin's keel line over the cabin's own length, and its pieces once the
        // cabin takes its sides at the hull; null for any other part, or one the cabin leaves as it is.
        private static KwRailPlan? Plan(Piece rail, Vector3 at, Vector3 forward, KwCabinSpec spec, List<float> ribs)
        {
            ZNetView? view = rail.m_nview;
            string saved = view != null && view.IsValid() ? view.GetZDO().GetString(KwGenerated.Key) : "";
            if (!KwSpan.TryParse(saved, out KwSpan span) || !Overlaps(rail.transform.position, rail.transform.forward, span.Length, at, forward, spec))
                return null;
            List<KwRailPiece>? pieces = Pieces(rail.transform.position, rail.transform.forward, span, at, forward, spec, ribs);
            return pieces != null ? new KwRailPlan(rail, saved, span, pieces) : null;
        }

        /// <summary>
        /// The pieces a cabin (its frame at, facing forward; ribs standing at these places along it) leaves of a railing part
        /// on its keel line (where it starts, its way, its span), each from and to along the part: null when it leaves it as
        /// it is, empty when the part goes (<see cref="KwRailCut.Plan"/>).
        /// </summary>
        public static List<KwRailPiece>? Pieces(Vector3 position, Vector3 way, KwSpan span, Vector3 at, Vector3 forward, KwCabinSpec spec, List<float> ribs)
        {
            float start = Vector3.Dot(position - at, forward), sign = Mathf.Sign(Vector3.Dot(way, forward));
            float a = (spec.X0 - start) * sign, b = (spec.X1 - start) * sign;
            List<float> along = ribs.ConvertAll(rib => (rib - start) * sign);
            return KwRailCut.Plan(span, Mathf.Min(a, b), Mathf.Max(a, b), along, KwRailCut.Taken(spec, sign > 0f));
        }

        /// <summary>Whether a railing part (where it starts, its way, its length) lies on the cabin's keel line over the cabin's own length.</summary>
        public static bool Overlaps(Vector3 position, Vector3 way, float length, Vector3 at, Vector3 forward, KwCabinSpec spec)
        {
            if (!KwCabinHull.InLine(at, forward, position, way))
                return false;
            float start = Vector3.Dot(position - at, forward), end = start + Mathf.Sign(Vector3.Dot(way, forward)) * length;
            return Mathf.Min(Mathf.Max(start, end), spec.X1) - Mathf.Max(Mathf.Min(start, end), spec.X0) > Overlap;
        }

        /// <summary>The railing the cabin stands in taken from its sides at the hull, kept with this frame's undo step; how many parts changed.</summary>
        public static int Take(Player player, Vector3 at, Vector3 forward, KwCabinSpec spec)
        {
            int changed = 0;
            foreach (KwRailPlan plan in Plans(at, forward, spec))
                changed += Cut(player, plan) ? 1 : 0;
            return changed;
        }

        // One railing part taken the hammer's way and its pieces spawned in its place along it, with its tier and other
        // values; false when a ward keeps it.
        private static bool Cut(Player player, KwRailPlan plan)
        {
            Piece rail = plan.Rail;
            if (rail == null || rail.m_nview == null || !rail.m_nview.IsValid())
                return false;
            Transform t = rail.transform;
            (Vector3 position, Vector3 way, Quaternion turn) = (t.position, t.forward, t.rotation);
            ZDO zdo = rail.m_nview.GetZDO();
            KwBiomeTier? tier = KwRailLooks.Tiered(zdo) ? KwBiomeTiers.Of(zdo) : null;
            string ship = zdo.GetString(KwBlueprintStation.ShipHash);
            KwUndoPiece old = KwUndoWork.Kept(rail);
            if (!KwUndoWork.RemoveOne(rail))
                return false;
            List<KwBpValue> keep = new List<KwBpValue>(old.Data);
            if (ship.Length > 0)
                keep.Add(new KwBpValue(KwBlueprintStation.ShipHash, ship));
            foreach (KwRailPiece piece in plan.Pieces)
                KwTools.Spawn(player, KwBiomeCost.Rail, position + way * piece.From, turn, KwRailCut.Text(plan.Saved, plan.Span, piece), tier, keep);
            KwUndo.Taken(player, old);
            return true;
        }

    }

    /// <summary>A railing part a cabin changes: the piece, its saved text and span, and the pieces it leaves (none: it goes whole).</summary>
    public sealed class KwRailPlan
    {
        public KwRailPlan(Piece rail, string saved, KwSpan span, List<KwRailPiece> pieces)
        {
            Rail = rail;
            Saved = saved;
            Span = span;
            Pieces = pieces;
        }

        public Piece Rail { get; }
        public string Saved { get; }
        public KwSpan Span { get; }
        public List<KwRailPiece> Pieces { get; }
    }
}
