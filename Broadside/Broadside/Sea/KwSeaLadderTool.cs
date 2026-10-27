using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The ladder tool on a launched ship (<see cref="KwSeaLadders"/>), on the editing player's game: aimed at the ship, the
    /// ladder the dock's one click would build there (<see cref="KwSeaLadderPlanner"/>, over the ship's blueprint as it is
    /// now, <see cref="KwSeaLadderShip"/>, made once a revision) shown see-through on the ship (<see cref="KwSeaGhost"/>),
    /// where it snapped or why not told as on the dock (<see cref="KwLadderTool.Told"/>); a click asks the ship's owner for
    /// it, with a gap cut in any roof railing its top comes up on (<see cref="KwSeaAsk"/>: the workbench and the ward at its
    /// top, its wood). Aimed at a ladder, the click opens its length window (<see cref="KwLadderEdit"/>, with a Remove
    /// button, <see cref="KwSeaWindow"/>) and the hammer's remove takes it (<see cref="Pick"/>).
    /// </summary>
    public static class KwSeaLadderTool
    {
        private static KwSeaLadderShip? model;
        private static KwSeaShip? modelShip;
        private static int modelVersion = -1;

        public static bool Click(KwSeaLadders kind, Player player, KwSeaAim aim)
        {
            KwSeaLadderShip ship = Model(aim.Ship);
            int ladder = Aimed(ship, aim);
            if (ladder >= 0)
            {
                Edit(kind, player, aim.Ship, ladder);
                return true;
            }
            KwLadderPlan? plan = KwSeaLadderPlanner.Plan(ship, aim.From, aim.Way, aim.Hit ? aim.Point : (Vector3?)null, out string why, out _);
            if (plan == null)
                player.Message(MessageHud.MessageType.Center, why);
            else if (KwSeaAsk.Send(player, aim.Ship, Built(ship, plan, aim.Ship.Revision), plan.Top))
                KwLadderTool.Quiet();
            return true;
        }

        /// <summary>The ladder the aim would build, see-through on the ship; nothing over a ladder (its click edits it).</summary>
        public static bool Hover(KwSeaAim aim)
        {
            KwSeaLadderShip ship = Model(aim.Ship);
            if (Aimed(ship, aim) >= 0)
                return false;
            KwLadderPlan? plan = KwSeaLadderPlanner.Plan(ship, aim.From, aim.Way, aim.Hit ? aim.Point : (Vector3?)null, out _, out string tell);
            KwLadderTool.Told(tell);
            if (plan == null)
                return false;
            KwBpPiece piece = KwSeaPiece.Make(KwSeaLadders.Made, plan.Top, plan.Turn, KwLadder.Save(plan.Length), null);
            KwSeaGhost.Show(aim.Ship, "ladder|" + plan, new[] { piece }, false);
            return true;
        }

        public static void Unhover() => KwLadderTool.Told("");

        /// <summary>The ladder the hammer's remove aims at: its climb box hit, or the nearest ladder's box to the point hit.</summary>
        public static KwSeaPick? Pick(KwSeaLadders kind, KwSeaAim aim)
        {
            KwSeaLadderShip ship = Model(aim.Ship);
            int hit = Aimed(ship, aim);
            if (hit >= 0 || !aim.Hit)
                return hit >= 0 ? new KwSeaPick(kind, hit, 0, 0f, "ladder", aim.Point) : null;
            KwSeaPick? best = null;
            foreach ((int index, KwBpPiece piece, float length) in ship.Ladders)
            {
                float off = KwSeaLadderPlanner.Off(piece, length, aim.Point);
                if (best == null || off < best.Distance)
                    best = new KwSeaPick(kind, index, 0, off, "ladder", aim.Point);
            }
            return best;
        }

        // The ship as the tool sees it, made again when the ship has been redrawn since.
        private static KwSeaLadderShip Model(KwSeaShip ship)
        {
            if (model == null || modelShip != ship || modelVersion != ship.Version)
                (model, modelShip, modelVersion) = (new KwSeaLadderShip(ship.Blueprint), ship, ship.Version);
            return model;
        }

        // The ladder aimed at: its climb box hit, else one whose box the point hit is in; -1 for none.
        private static int Aimed(KwSeaLadderShip ship, KwSeaAim aim)
        {
            KwLadderClimb? climb = aim.Collider != null ? aim.Collider.GetComponent<KwLadderClimb>() : null;
            if (climb != null && climb.Piece != null)
                return ship.Blueprint.Pieces.IndexOf(climb.Piece);
            return aim.Hit ? KwSeaLadderPlanner.Ladder(ship, aim.Point) : -1;
        }

        /// <summary>The edit that builds a planned ladder, asked on a revision: the gap in any roof railing its top comes up on first, then the ladder.</summary>
        public static KwSeaEdit Built(KwSeaLadderShip seen, KwLadderPlan plan, int revision)
        {
            List<KwSeaPart> parts = Gaps(seen, plan.Top);
            int gaps = parts.Count;
            parts.Add(KwSeaPart.Add(KwSeaPiece.Make(KwSeaLadders.Made, plan.Top, plan.Turn, KwLadder.Save(plan.Length), null)));
            string where = plan.Note.Length > 0 ? ", " + char.ToLowerInvariant(plan.Note[0]) + plan.Note.Substring(1) : "";
            string done = $"Ladder built ({plan.Length:0.0} m){where}" + (gaps > 0 ? "; the railing above it has a gap for it" : "");
            return new KwSeaEdit(KwSeaLadders.Kind, revision, done, parts);
        }

        // Every roof railing whose outline the ladder's top comes up on and that has no gap there yet, with one (KwLadderGaps).
        private static List<KwSeaPart> Gaps(KwSeaLadderShip seen, Vector3 top)
        {
            List<KwSeaPart> parts = new List<KwSeaPart>();
            foreach ((int index, KwBpPiece piece, KwRoofRail rail, string saved) in seen.RoofRails)
            {
                string? text = KwLadderGaps.WithGap(rail, piece.Position, piece.Rotation, top)?.ToString();
                if (text != null && text != saved)
                    parts.Add(KwSeaPart.Change(index, KwSeaPiece.With(piece, text, null)));
            }
            return parts;
        }

        // A ladder's length window, with a Remove button; the rules at its top checked first.
        private static void Edit(KwSeaLadders kind, Player player, KwSeaShip ship, int index)
        {
            KwBpPiece piece = ship.Blueprint.Pieces[index];
            KwLadderEdit edit = new KwLadderEdit();
            KwPartValues? values = edit.Read(KwSeaPiece.Text(piece), KwBiomeTiers.Meadows);
            string? why = KwSeaRules.Why(player, ship, ship.World(piece.Position)) ?? (values == null ? "This ladder cannot be read to change it" : null);
            if (why != null)
            {
                player.Message(MessageHud.MessageType.Center, why);
                return;
            }
            void Remove() => KwSeaAsk.Send(player, ship, kind.Removal(ship, new KwSeaPick(kind, index, 0, 0f, "ladder", piece.Position))!, piece.Position);
            KwSeaWindow.Open(player, ship, index, edit, values!, "Edit " + edit.Words(1), KwSeaLadderLook.Renderers(ship, piece), Remove,
                kind.Name, "Ladder changed", piece.Position);
        }
    }
}
