using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The quay's mooring posts (DECORATE.md, "The quay"): on a dock section's water side each stands over the free piling
    /// station nearest its place in the rhythm (<see cref="KwDockMooring.Over"/>, as one placed by hand on a dock): centred on
    /// the piling's axis, square to the edge, its rope coil in over the deck; the section's curb is cut round it once it is
    /// placed (<see cref="KwPierGaps"/>), so the test of what stands in its way passes over the dock's own curb and timbers.
    /// On a game floor dock it is set against the edge as before (<see cref="KwDecorQuay.Against"/>).
    /// </summary>
    public static class KwDecorMoorings
    {
        private const string Piece = "mooringpost";
        private const float Level = 0.35f, Shrink = 0.04f, Lift = 0.06f;
        private static readonly Collider[] found = new Collider[16];
        private static int mask;

        private static int Mask => mask != 0 ? mask : mask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "vehicle", "character", "character_net", "character_noenv");

        /// <summary>A mooring post at a distance along a water run, added when it fits.</summary>
        public static bool Post(KwDecorLayout layout, KwEdgeRun run, float t, int level)
        {
            KwDecorGrid grid = layout.Area.Grid;
            Vector3 point = run.PointAt(grid, t);
            KwPierDeck? deck = Deck(point);
            if (deck == null)
                return KwDecorQuay.Against(layout, run, t, Piece, KwDecorPlace.Shore, level, -1);
            KwDecorKind? kind = KwDecorKind.Find(Piece);
            (Vector3 At, Quaternion Turn)? pose = kind != null ? KwDockMooring.Over(deck, point, Planned(layout), kind.Name) : null;
            if (pose == null)
                return false;
            int k = run.CellAt(t);
            KwSlot slot = new KwSlot(KwDecorPlace.Shore, k, pose.Value.At, grid.Dir(run.Out), true, grid.Cells[k].Region);
            KwDecorPlan plan = new KwDecorPlan(kind!, pose.Value.At, pose.Value.Turn, slot, level, -1);
            if (!KwDecorFitCheck.Spaced(layout.Area, plan, layout.Plans) || !KwDecorRules.Allowed(grid, plan) || !Clear(plan))
                return false;
            layout.Plans.Add(plan);
            return true;
        }

        /// <summary>The built dock section whose deck a point is on, at its height.</summary>
        public static KwPierDeck? Deck(Vector3 point)
        {
            foreach (KwPierDeck deck in KwPierDeck.Near(point, 0.05f))
            {
                if (Mathf.Abs(point.y - deck.Top) <= Level)
                    return deck;
            }
            return null;
        }

        /// <summary>Where the layout's mooring posts and lantern poles stand so far (a piling holds one).</summary>
        public static List<Vector3> Planned(KwDecorLayout layout) =>
            layout.Plans.FindAll(p => KwDockMooring.OnPiling(p.Kind.Name)).ConvertAll(p => p.Position);

        /// <summary>Its boxes at its pose, lifted and shrunk as every decoration's, meeting nothing but a dock section's own parts.</summary>
        public static bool Clear(KwDecorPlan plan)
        {
            Matrix4x4 pose = Matrix4x4.TRS(plan.Position + Vector3.up * Lift, plan.Rotation, Vector3.one);
            foreach ((Matrix4x4 at, Vector3 centre, Vector3 size) in plan.Kind.Solids)
            {
                Matrix4x4 box = pose * at;
                Vector3 half = Vector3.Max(Vector3.Scale(size * 0.5f, box.lossyScale) - Vector3.one * Shrink, Vector3.one * 0.01f);
                int hits = Physics.OverlapBoxNonAlloc(box.MultiplyPoint3x4(centre), half, found, box.rotation, Mask, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < hits; i++)
                {
                    if (found[i].GetComponentInParent<KwPier>() == null)
                        return false;
                }
            }
            return true;
        }
    }
}
