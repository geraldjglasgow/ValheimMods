using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The quay's lantern poles (DECORATE.md, "The quay"): on a dock section's water side, or a narrow pier end's corner, each
    /// stands over the free piling station nearest its place in the rhythm (<see cref="KwDockMooring.Over"/>, as one placed by
    /// hand on a dock): its post on the piling's axis, so the piling reads as rising through the deck into it, turned so its
    /// lamp (the model's -X) hangs in over the deck toward the dock's middle, drawn and placed without its cairn
    /// (<see cref="KwDockTrim"/>); the section's curb is cut round it once placed. A station where another dock branches off,
    /// a dock ladder hangs or a mooring post or lantern stands or is planned is passed over, and one where the dock is
    /// stepped onto or in a zone kept clear is left empty. A game floor has no pilings: none
    /// there. The deck holds it at full support wherever along the section it stands (DOCKS.md, "Support").
    /// </summary>
    public static class KwDecorLights
    {
        private const string Piece = "lanternpole";
        private const float Ends = 0.3f;

        /// <summary>A lantern pole over a piling near a distance along a water run, added when it fits.</summary>
        public static bool OnPiling(KwDecorLayout layout, KwEdgeRun run, float t, int level)
        {
            KwDecorKind? kind = KwDecorKind.Find(Piece);
            if (kind == null || t < Ends || t > run.Length - Ends)
                return false;
            KwDecorGrid grid = layout.Area.Grid;
            Vector3 point = run.PointAt(grid, t);
            KwPierDeck? deck = KwDecorMoorings.Deck(point);
            (Vector3 At, Quaternion Turn)? pose = deck != null ? KwDockMooring.Over(deck, point, KwDecorMoorings.Planned(layout), kind.Name) : null;
            if (pose == null || !Open(grid, pose.Value.At))
                return false;
            int k = run.CellAt(t);
            KwSlot slot = new KwSlot(KwDecorPlace.Deck, k, pose.Value.At, grid.Dir(run.Out), true, grid.Cells[k].Region);
            KwDecorPlan plan = new KwDecorPlan(kind, pose.Value.At, pose.Value.Turn, slot, level, -1);
            if (!KwDecorFitCheck.Spaced(layout.Area, plan, layout.Plans) || !KwDecorRules.Allowed(grid, plan) || !KwDecorMoorings.Clear(plan))
                return false;
            layout.Plans.Add(plan);
            return true;
        }

        // Not where the dock is stepped onto (its access) nor in a zone kept clear (a ladder's, a door's, a way between them).
        private static bool Open(KwDecorGrid grid, Vector3 at) => grid.CellOf(at, out int k) && !grid.Cells[k].Has(KwCellMark.Access | KwCellMark.Clear);

        /// <summary>Whether a plan is a lantern pole over a dock's piling (previewed and placed without its cairn).</summary>
        public static bool Seated(KwDecorPlan plan) => plan.Kind.Name == KwDockside.Prefix + Piece && plan.Slot.Pushed;
    }
}
