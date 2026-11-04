using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Whether a planned decoration fits where it was put (DECORATE.md, "What a decoration needs"), cheapest test first:
    /// every point of its footprint on a cell of its kind of place (a dock's band at the slot's height, level ground, the
    /// shore, shallow water of its depth, clear air under a hung piece) and never in a keep-clear zone, path, lane or access
    /// (a hung piece over a lane is let be, and a piece set against a dock's edge may reach its slot's
    /// <see cref="KwSlot.Reach"/> into the lane); apart from the other planned decorations of other groups and those standing,
    /// by the larger of the two spacings; then the world's own rules and what stands there (<see cref="KwDecorWorld"/>).
    /// </summary>
    public static class KwDecorFitCheck
    {
        private const float DeckLevel = 0.15f, GroundLevel = 0.3f, MinUp = 0.94f, Waterline = 0.4f, Shallowest = 0.15f;
        private const KwCellMark Walked = KwCellMark.Path | KwCellMark.Lane | KwCellMark.Access;
        private static readonly List<Vector3> points = new List<Vector3>();

        public static bool Fits(KwDecorArea area, KwDecorPlan plan, List<KwDecorPlan> planned) =>
            plan.Kind.Fit.In(plan.Slot.Place) && Cells(area.Grid, plan) && Apart(area, plan, planned) && KwDecorWorld.Allows(area.Grid, plan);

        /// <summary>Apart from the planned decorations of other groups and those standing (a mooring post over a dock's piling, <see cref="KwDecorMoorings"/>).</summary>
        public static bool Spaced(KwDecorArea area, KwDecorPlan plan, List<KwDecorPlan> planned) => Apart(area, plan, planned);

        private static bool Cells(KwDecorGrid grid, KwDecorPlan plan)
        {
            KwDecorFoot.Samples(plan.Kind.Fit, plan.Position, plan.Rotation, plan.Slot.Pushed, points);
            foreach (Vector3 point in points)
            {
                if (!grid.CellOf(point, out int k) || !CellFits(grid.Cells[k], plan, plan.Slot.At.y, Lane(plan.Slot, point)))
                    return false;
            }
            return true;
        }

        // A point of a piece set against a dock's edge within its slot's reach into the lane.
        private static bool Lane(KwSlot slot, Vector3 point) => slot.Pushed && slot.Reach > 0f && Vector3.Dot(point - slot.At, -slot.Back) <= slot.Reach;

        private static bool CellFits(KwCell cell, KwDecorPlan plan, float level, bool lane)
        {
            KwDecorFit fit = plan.Kind.Fit;
            bool hung = plan.Slot.Place == KwDecorPlace.Wall || plan.Slot.Place == KwDecorPlace.Edge;
            KwCellMark walked = lane ? Walked & ~KwCellMark.Lane : Walked;
            if (cell.Has(KwCellMark.Clear) || (!fit.InPath && !hung && cell.Has(walked)))
                return false;
            switch (plan.Slot.Place)
            {
                case KwDecorPlace.Deck: return OnDeck(cell, plan.Slot.Region, level, lane);
                case KwDecorPlace.Ground: return OnGround(cell, level);
                case KwDecorPlace.Shore: return OnGround(cell, level) || OnDeck(cell, plan.Slot.Region, level, lane) || (fit.Wet && AtWaterline(cell, level));
                case KwDecorPlace.Shallows: return cell.Kind == KwCellKind.Water && cell.Depth >= Shallowest && cell.Depth <= fit.MaxDepth;
                case KwDecorPlace.Edge: return cell.Region != plan.Slot.Region && (cell.Kind == KwCellKind.None || cell.Y < level - fit.Height);
                default: return cell.Kind != KwCellKind.Blocked || cell.Y < level + fit.Lift;
            }
        }

        private static bool OnDeck(KwCell cell, short region, float level, bool lane) =>
            cell.Kind == KwCellKind.Deck && cell.Region == region && (cell.Has(KwCellMark.Band) || (lane && cell.Has(KwCellMark.Lane))) && Mathf.Abs(cell.Y - level) <= DeckLevel;

        private static bool OnGround(KwCell cell, float level) =>
            (cell.Kind == KwCellKind.Ground && cell.Up >= MinUp && Mathf.Abs(cell.Y - level) <= GroundLevel)
            || (cell.Kind == KwCellKind.Deck && cell.Has(KwCellMark.Open) && Mathf.Abs(cell.Y - level) <= DeckLevel);

        private static bool AtWaterline(KwCell cell, float level) =>
            cell.Kind == KwCellKind.Water && cell.Depth <= Waterline && Mathf.Abs(cell.Y - level) <= Waterline;

        // Apart from other groups' pieces; a piece hung on a dock's side over the water and one standing on the deck are
        // either side of the edge, so only pieces of the same side keep their spacing, but a mooring post or lantern pole
        // over a piling (at the deck's edge, right over where a fender would hang) keeps it from both.
        private static bool Apart(KwDecorArea area, KwDecorPlan plan, List<KwDecorPlan> planned)
        {
            float mine = plan.Kind.Fit.Spacing;
            foreach (KwDecorPlan other in planned)
            {
                bool kept = (plan.Group < 0 || other.Group != plan.Group) && (SameSide(plan, other) || Seated(plan) || Seated(other));
                if (kept && plan.Box.Overlaps(other.Box, Mathf.Max(mine, other.Kind.Fit.Spacing)))
                    return false;
            }
            foreach ((KwDecorBox box, float spacing) in area.Existing)
            {
                if (plan.Box.Overlaps(box, Mathf.Max(mine, spacing)))
                    return false;
            }
            return true;
        }

        private static bool SameSide(KwDecorPlan a, KwDecorPlan b) => (a.Slot.Place == KwDecorPlace.Edge) == (b.Slot.Place == KwDecorPlace.Edge);

        // A mooring post or lantern pole over a piling at the deck's edge: a fender keeps clear of it too.
        private static bool Seated(KwDecorPlan plan) => plan.Slot.Pushed && KwDockMooring.OnPiling(plan.Kind.Name);
    }
}
