using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A cabin edit's stack (<see cref="KwCabinEdit.Follow"/>; the user's report, 2026-10-25: a cabin raised grew into the
    /// cabin on its roof): the pieces round the edited cabins read (<see cref="KwStackPart"/>), the stack carried with them
    /// (<see cref="KwCabinLift"/>), and the result given back as the edit's plans. An edited cabin standing on another one
    /// changed (Shift: every cabin) is planned again in place on that cabin's new roof; every other piece that moves, is built
    /// again or goes is a plan of its own (<see cref="KwEditPlan.Follows"/>), built in the same frame, so the whole stack is
    /// one undo step with the edit. The note and the message say what goes with the cabins (<see cref="KwStackWords"/>). Runs
    /// on the editing player's game; the pieces go through the game's networked removal and placement as the edit's own.
    /// </summary>
    public static class KwCabinFollow
    {
        public static void Plan(List<KwEditPlan> plans, List<string> changed, KwEditFollow follow)
        {
            follow.Clear();
            Piece? first = plans.Count > 0 ? plans[0].Target.Piece : null;
            if (first == null)
                return;
            List<KwStackPart> parts = KwStackPart.Near(first.transform.position);
            KwCabinLift lift = KwCabinLift.Edit(parts, Planned(plans, parts), changed);
            foreach (KwStackMove move in lift.Moves)
                Place(move, plans, follow);
            follow.Note = KwStackWords.Note(lift);
            follow.Told = KwStackWords.Told(lift);
        }

        // Each edited cabin read among the parts, as its plan builds it.
        private static Dictionary<KwStackPart, KwCabinSpec> Planned(List<KwEditPlan> plans, List<KwStackPart> parts)
        {
            Dictionary<KwStackPart, KwCabinSpec> planned = new Dictionary<KwStackPart, KwCabinSpec>();
            foreach (KwEditPlan plan in plans)
            {
                KwStackPart? part = parts.Find(p => p.Piece == plan.Target.Piece);
                if (part?.Cabin != null && KwCabinSpec.TryParse(plan.Text, out KwCabinSpec spec))
                    planned[part] = spec;
            }
            return planned;
        }

        // A move given back: the edit's own plan of that piece planned again, or a plan of its own that follows.
        private static void Place(KwStackMove move, List<KwEditPlan> plans, KwEditFollow follow)
        {
            Piece? piece = move.Part.Piece;
            if (piece == null || piece.m_nview == null || !piece.m_nview.IsValid())
                return;
            int mine = plans.FindIndex(plan => plan.Target.Piece == piece);
            if (mine >= 0)
            {
                KwEditPlan plan = plans[mine];
                plans[mine] = new KwEditPlan(plan.Target, move.Text ?? plan.Target.Saved, plan.Tier, move.Shift, move.Taken);
                return;
            }
            KwEditTarget target = new KwEditTarget(piece, move.Part.Text, KwBiomeTiers.Of(piece.m_nview.GetZDO()), new KwPartValues());
            follow.Moved.Add(new KwEditPlan(target, move.Text ?? move.Part.Text, null, move.Shift, move.Taken)
            {
                Follows = true,
                Tool = KwEditPlan.ToolOf(move.Part.Prefab),
            });
        }
    }

    /// <summary>
    /// What the cabin edit's window and message say of its stack (<see cref="KwCabinLift"/>): the cabins carried and how far
    /// they rise, those whose walls moved to fit their new roof (and what the fit changed), those that no longer fit; what
    /// goes with the roofs and what is taken away.
    /// </summary>
    public static class KwStackWords
    {
        /// <summary>The note line: "The cabin on its roof moves up 0.5 m with it. What stands on the roofs goes with them: 1 roof railing, 1 ladder."</summary>
        public static string Note(KwCabinLift lift)
        {
            List<string> lines = new List<string>();
            if (lift.Carried.Count > 0)
                lines.Add(Carried(lift));
            List<KwStackMove> riders = lift.Moves.FindAll(m => m.Part.Cabin == null);
            List<KwStackMove> going = riders.FindAll(m => !m.Taken), taken = riders.FindAll(m => m.Taken);
            if (going.Count > 0)
                lines.Add("What stands on the roofs goes with them: " + Count(going) + ".");
            List<KwStackMove> lost = lift.Moves.FindAll(m => m.Part.Cabin != null && m.Taken);
            if (lost.Count > 0)
                lines.Add(Many(lost.Count, "cabin") + " on it no longer fit" + (lost.Count == 1 ? "s" : "") + " there: taken away with what stands on it.");
            if (taken.Count > 0)
                lines.Add("Taken away, no longer on its roof: " + Count(taken) + ".");
            return string.Join(" ", lines);
        }

        /// <summary>The message's line after OK: "With it: 1 cabin, 1 roof railing, 1 ladder; taken away: 1 mast".</summary>
        public static string Told(KwCabinLift lift)
        {
            List<KwStackMove> moved = lift.Moves.FindAll(m => !m.Taken), taken = lift.Moves.FindAll(m => m.Taken);
            string with = moved.Count > 0 ? "With it: " + Count(moved) : "";
            string away = taken.Count > 0 ? "taken away: " + Count(taken) : "";
            return with.Length > 0 && away.Length > 0 ? with + "; " + away : with.Length > 0 ? with : away.Length > 0 ? char.ToUpperInvariant(away[0]) + away.Substring(1) : "";
        }

        // "The cabin on its roof moves up 0.5 m with it", "2 cabins on its roof move with it, 1 with its walls moved to fit the
        // new roof", and what the fit changed on them ("Front inset limited to 0.1 m: ...").
        private static string Carried(KwCabinLift lift)
        {
            KwStackPart first = lift.Carried[0];
            float rise = lift.Now[first].Top - first.At.Top;
            bool even = lift.Carried.TrueForAll(c => Mathf.Abs(lift.Now[c].Top - c.At.Top - rise) < 0.005f);
            string how = even && Mathf.Abs(rise) >= 0.005f ? $" {(rise > 0f ? "up" : "down")} {Mathf.Abs(rise):0.##} m" : "";
            string who = lift.Carried.Count == 1 ? "The cabin on its roof moves" : $"{lift.Carried.Count} cabins on its roof move";
            int refit = lift.Refitted.Count;
            string walls = refit == 0 ? "" : lift.Carried.Count == 1 ? ", its walls moved to fit the new roof" : $", {refit} with walls moved to fit the new roofs";
            List<string> fit = new List<string>();
            foreach (KwStackPart cabin in lift.Carried)
                fit.AddRange(Fitted(cabin.Cabin!, lift.Now[cabin].Spec));
            return who + how + " with it" + walls + "." + (fit.Count > 0 ? " Above: " + string.Join(" ", fit) : "");
        }

        // What the fit changed on a carried cabin that it did not before.
        private static List<string> Fitted(KwCabinSpec was, KwCabinSpec now)
        {
            List<string> notes = new List<string>();
            foreach (string note in now.Fitted)
            {
                if (!notes.Contains(note) && !was.Fitted.Contains(note))
                    notes.Add(note);
            }
            return notes;
        }

        // "1 roof railing, 2 ladders, 1 mast": the moves by kind.
        private static string Count(List<KwStackMove> moves)
        {
            Dictionary<string, int> kinds = new Dictionary<string, int>();
            foreach (KwStackMove move in moves)
            {
                string kind = Kind(move.Part);
                kinds[kind] = kinds.TryGetValue(kind, out int n) ? n + 1 : 1;
            }
            List<string> words = new List<string>();
            foreach (KeyValuePair<string, int> kind in kinds)
                words.Add(Many(kind.Value, kind.Key));
            return string.Join(", ", words);
        }

        private static string Kind(KwStackPart part) =>
            part.Cabin != null ? "cabin" : part.RoofRail != null ? "roof railing" : part.IsLadder ? "ladder"
            : part.Prefab == KwSailTool.Made ? "mast" : "piece";

        private static string Many(int count, string kind) => count == 1 ? "1 " + kind : $"{count} {kind}s";
    }
}
