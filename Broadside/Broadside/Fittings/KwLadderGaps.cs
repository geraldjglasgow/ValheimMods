using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Ladders and roof railings (<see cref="KwRoofRail"/>) meeting: where a ladder's top comes up on a roof railing's
    /// outline (within <see cref="KwRoofRun.Reach"/> of it, level with the roof), the rail leaves a gap
    /// <see cref="KwRoofRail.Gap"/> wide, so climbing out is natural. A ladder built there (<see cref="Build"/>) builds the
    /// railing again with the gap through the edit's replace path (<see cref="KwEditWork"/>), first, and then the ladder in
    /// the same frame: one undo step, which takes the ladder away and puts the railing back as it was (and redo the other
    /// way, <see cref="KwUndoWork.Swap"/>). A roof railing laid later over a ladder's top leaves the gap too
    /// (<see cref="Ladders"/>). A ladder removed later leaves its gap; the railing's edit closes it. Runs on the clicking
    /// player's game; every part goes through the game's networked removal and placement.
    /// </summary>
    public static class KwLadderGaps
    {
        // How far round a ladder's top roof railings and round a roof ladders are looked for, and how near the roof's height a
        // ladder's top must be.
        private const float Reach = 20f, Level = 0.25f;

        private static readonly string Rail = KwToolPieces.PartPrefix + KwBiomeCost.Rail, Ladder = KwToolPieces.PartPrefix + KwLadderTool.Tool;
        private static readonly List<Piece> Found = new List<Piece>();

        /// <summary>A ladder built, any roof railing at its top built again with a gap there first; how many railings were.</summary>
        public static int Build(Player player, KwLadderPlan plan)
        {
            List<KwEditPlan> cuts = Cuts(plan.Top);
            int done = cuts.Count > 0 ? KwEditWork.Replace(player, KwBiomeCost.Rail, cuts, out _) : 0;
            KwTools.Spawn(player, KwLadderTool.Tool, plan.Top, plan.Turn, KwLadder.Save(plan.Length));
            return done;
        }

        /// <summary>Whether a piece is a roof railing, and its railing and saved text.</summary>
        public static bool RoofRail(Piece piece, out KwRoofRail rail, out string saved)
        {
            rail = null!;
            ZDO? zdo = piece.m_nview != null && piece.m_nview.IsValid() ? piece.m_nview.GetZDO() : null;
            saved = zdo?.GetString(KwGenerated.Key) ?? "";
            return zdo != null && Utils.GetPrefabName(piece.gameObject) == Rail && KwRoofRail.TryParse(saved, out rail);
        }

        /// <summary>A roof railing (to stand at an origin and heading) with a gap at every ladder whose top comes up on its outline.</summary>
        public static KwRoofRail Ladders(KwRoofRail rail, Vector3 origin, Quaternion turn)
        {
            foreach (Piece piece in Near(origin))
            {
                if (Utils.GetPrefabName(piece.gameObject) == Ladder)
                    rail = WithGap(rail, origin, turn, piece.transform.position) ?? rail;
            }
            return rail;
        }

        /// <summary>A roof railing with a gap where a ladder's top (in the world) comes up on its outline; null when it does not.</summary>
        public static KwRoofRail? WithGap(KwRoofRail rail, Vector3 origin, Quaternion turn, Vector3 top)
        {
            Vector3 kit = KwRoofFrame.ToKit(top, origin, turn);
            if (Mathf.Abs(kit.z) > Level)
                return null;
            Vector2? on = rail.Near(new Vector2(kit.x, kit.y), KwRoofRun.Reach);
            return on != null ? rail.WithGap(on.Value) : null;
        }

        // Every roof railing round a ladder's top whose outline it comes up on and that has no gap there yet, with one.
        private static List<KwEditPlan> Cuts(Vector3 top)
        {
            List<KwEditPlan> cuts = new List<KwEditPlan>();
            foreach (Piece piece in Near(top))
            {
                if (!RoofRail(piece, out KwRoofRail rail, out string saved))
                    continue;
                string? text = WithGap(rail, piece.transform.position, piece.transform.rotation, top)?.ToString();
                if (text != null && text != saved)
                    cuts.Add(new KwEditPlan(new KwEditTarget(piece, saved, KwBiomeTiers.Of(piece.m_nview.GetZDO()), new KwPartValues()), text, null));
            }
            return cuts;
        }

        // The pieces loaded within reach of a point.
        private static List<Piece> Near(Vector3 point)
        {
            Found.Clear();
            Piece.GetAllPiecesInRadius(point, Reach, Found);
            List<Piece> near = Found.FindAll(piece => piece != null);
            Found.Clear();
            return near;
        }
    }
}
