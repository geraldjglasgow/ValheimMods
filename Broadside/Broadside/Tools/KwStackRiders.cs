using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What stands on the cabins a stack moves goes with their roofs (<see cref="KwCabinLift"/>): a roof railing moves to the
    /// new roof's top over the cabin's origin, laid again on the new outline when that changed (its posts' spacing, railed
    /// sides and ladder gaps kept); a piece standing on a roof (a mast) rises with it, and is taken away when the new roof is
    /// no longer under it; a ladder keeps its foot and changes its length to reach its roof's new top, as the ladder tool
    /// would build it there (moving whole when its foot's roof rises too), and is taken away when its top no longer meets the
    /// roof's edge, its foot's roof is no longer under it, or it would pass the ladder's lengths. Everything on a cabin taken
    /// away goes too. Pure numbers.
    /// </summary>
    public static class KwStackRiders
    {
        /// <summary>The riders of every cabin the lift changed or took away, then the ladders round them.</summary>
        public static void Follow(KwCabinLift lift)
        {
            foreach (KwStackPart cabin in lift.Cabins)
            {
                if (lift.Gone(cabin))
                    TakeOn(lift, cabin);
                else if (lift.Now.TryGetValue(cabin, out KwCabinAt set))
                    MoveOn(lift, cabin, set);
            }
            foreach (KwStackPart ladder in lift.Parts.FindAll(part => part.IsLadder))
                lift.Add(Ladder(lift, ladder));
        }

        /// <summary>The riders of every cabin a removal takes; of the ladders, those standing on a roof that goes.</summary>
        public static void Remove(KwCabinLift lift)
        {
            foreach (KwStackPart cabin in lift.Cabins.FindAll(lift.Gone))
                TakeOn(lift, cabin);
            foreach (KwStackPart ladder in lift.Parts.FindAll(part => part.IsLadder))
            {
                KwStackPart? foot = KwCabinStack.FootOn(ladder, lift.Cabins);
                if (foot != null && lift.Gone(foot))
                    lift.Add(KwStackMove.Taking(ladder));
            }
        }

        private static void TakeOn(KwCabinLift lift, KwStackPart cabin)
        {
            KwStackPart? rail = KwCabinStack.Railing(cabin, lift.Parts);
            if (rail != null)
                lift.Add(KwStackMove.Taking(rail));
            foreach (KwStackPart piece in KwCabinStack.Standing(cabin, lift.Parts))
                lift.Add(KwStackMove.Taking(piece));
        }

        // A changed cabin's roof railing and the pieces standing on its roof, with the roof's rise.
        private static void MoveOn(KwCabinLift lift, KwStackPart cabin, KwCabinAt set)
        {
            KwCabinAt was = cabin.At;
            KwStackPart? rail = KwCabinStack.Railing(cabin, lift.Parts);
            if (rail != null)
                lift.Add(Rail(rail, was, set));
            float rise = set.Top - was.Top;
            foreach (KwStackPart piece in KwCabinStack.Standing(cabin, lift.Parts))
            {
                Vector3 to = piece.Position + Vector3.up * rise;
                if (!set.OnRoof(to, 0.05f, KwCabinStack.OnTop))
                    lift.Add(KwStackMove.Taking(piece));
                else if (Mathf.Abs(rise) > 0.0005f)
                    lift.Add(new KwStackMove(piece, to, piece.Text));
            }
        }

        /// <summary>
        /// A roof railing on a cabin's new roof: over the cabin's origin at the new top, its outline the new roof's, its gaps
        /// where they were in the world (snapped onto the new outline, dropped when none is near); taken when the roof has no
        /// outline; null when it would stand as it does.
        /// </summary>
        public static KwStackMove? Rail(KwStackPart rail, KwCabinAt was, KwCabinAt set)
        {
            List<KwRoofSegment> edges = set.Edges();
            if (edges.Count == 0)
                return KwStackMove.Taking(rail);
            Vector3 origin = new Vector3(set.Frame.Origin.x, set.Top, set.Frame.Origin.z);
            bool kept = Same(edges, rail.RoofRail!.Edges) && Flat(origin - rail.Position).sqrMagnitude < 1e-6f;
            string text = kept ? rail.Text : Laid(rail, was, set, edges, origin).ToString();
            return text == rail.Text && (origin - rail.Position).sqrMagnitude < 1e-6f ? null : new KwStackMove(rail, origin, text);
        }

        // The railing laid on a new outline over a new origin: its spacing and sides, each gap where it was in the world
        // snapped onto the new outline (dropped when none is near).
        private static KwRoofRail Laid(KwStackPart rail, KwCabinAt was, KwCabinAt set, List<KwRoofSegment> edges, Vector3 origin)
        {
            KwRoofRail old = rail.RoofRail!, laid = new KwRoofRail(old.Spacing, old.On, edges, new List<Vector2>());
            KwStackFrame from = new KwStackFrame(rail.Position, was.Frame.Forward), to = new KwStackFrame(origin, set.Frame.Forward);
            foreach (Vector2 gap in old.Gaps)
            {
                Vector3 kit = to.ToKit(from.ToWorld(new Vector3(gap.x, gap.y, 0f)));
                Vector2? on = laid.Near(new Vector2(kit.x, kit.y), KwRoofRun.Reach);
                if (on != null)
                    laid = laid.WithGap(on.Value);
            }
            return laid;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        // Two outlines alike: the same edges, each end within a centimetre.
        private static bool Same(List<KwRoofSegment> a, List<KwRoofSegment> b)
        {
            if (a.Count != b.Count)
                return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (a[i].Side != b[i].Side || (a[i].A - b[i].A).magnitude > 0.01f || (a[i].B - b[i].B).magnitude > 0.01f)
                    return false;
            }
            return true;
        }

        // A ladder against a roof the lift changes or standing on one: taken with either roof gone or when it no longer meets
        // it; else its top up or down with its roof, its length by its top's rise less its foot's; null when nothing changes.
        private static KwStackMove? Ladder(KwCabinLift lift, KwStackPart ladder)
        {
            KwStackPart? top = KwCabinStack.TopOn(ladder, lift.Cabins), foot = KwCabinStack.FootOn(ladder, lift.Cabins);
            if ((top != null && lift.Gone(top)) || (foot != null && lift.Gone(foot)))
                return KwStackMove.Taking(ladder);
            (float up, bool topMeets) = End(lift, top, ladder.Position, true);
            (float down, bool footMeets) = End(lift, foot, KwCabinStack.Ends(ladder).Foot, false);
            float length = ladder.Length + up - down;
            if (!topMeets || !footMeets || length < KwLadder.MinLength - 0.001f || length > KwLadder.MaxLength + 0.001f)
                return KwStackMove.Taking(ladder);
            if (Mathf.Abs(up) < 0.0005f && Mathf.Abs(down) < 0.0005f)
                return null;
            return new KwStackMove(ladder, ladder.Position + Vector3.up * up, KwLadder.Save(length));
        }

        // How far a ladder's end rises with the roof it is on (none for a roof the lift leaves), and whether it still meets
        // that roof as changed: its top as near the roof's edge as it was (within 5 cm), its foot inside the roof.
        private static (float Rise, bool Meets) End(KwCabinLift lift, KwStackPart? cabin, Vector3 point, bool top)
        {
            if (cabin == null || !lift.Now.TryGetValue(cabin, out KwCabinAt set))
                return (0f, true);
            KwCabinAt was = cabin.At;
            float rise = set.Top - was.Top;
            Vector3 to = point + Vector3.up * rise;
            bool meets = top
                ? set.OnRoof(to, KwCabinStack.LadderEdge, KwCabinStack.LadderTop) && set.FromEdge(to) <= was.FromEdge(point) + 0.05f
                : set.OnRoof(to, KwCabinStack.LadderFoot, KwCabinStack.LadderFoot);
            return (rise, meets);
        }
    }
}
