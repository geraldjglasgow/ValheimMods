using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A cabin standing on another's roof, set on that roof again as the cabin under it changes (<see cref="KwCabinLift"/>):
    /// over the lower cabin's new roof as the Cabin tool builds a cabin on a roof (<see cref="KwCabinHull.Over"/>: its deck
    /// the roof's top, its hull the lower cabin's inside, its sides standing on the lower cabin's), with its own height, seed
    /// and openings. Each of its walls stays where it stood in the world, its inset measured again from the lower cabin's new
    /// walls; a wall standing on the lower cabin's own wall (no inset there) stays on it as that wall moves; an inset the player
    /// typed in the same edit (Shift: every cabin) is kept as typed. Where the lower cabin is drawn in past one of its walls
    /// that wall is drawn in with it, onto the new roof's edge, and the fit (<see cref="KwCabinFit"/>) does the rest, so it
    /// never overhangs the roof it stands on. Its openings keep their places on its walls, raised with its deck. Pure numbers.
    /// </summary>
    public static class KwCabinRebase
    {
        // How near an upper wall stands to the lower cabin's wall under it to stand on it, and the least inset that is one.
        private const float Flush = 0.01f, None = 0.005f;

        /// <summary>
        /// The upper cabin (as it stands; asked: its sizes as asked, the edit's own when it is edited too; typed: the window's
        /// keys the player changed) on the lower one that was, now changed: where it now stands, and whether any of its walls
        /// stands somewhere else (moved to fit the new roof).
        /// </summary>
        public static KwCabinAt On(KwCabinAt upper, KwCabinSizes asked, ICollection<string> typed, KwCabinAt was, KwCabinAt now, out bool moved)
        {
            KwCabinRange range = KwCabinHull.Over(now.Spec, now.Frame.Origin, now.Frame.Forward);
            KwStackFrame frame = new KwStackFrame(range.Position, now.Frame.Forward);
            KwCabinSizes sizes = Insets(upper, asked, typed, was, frame, range);
            int seed = upper.Spec.Seed;
            KwCabinAt plain = new KwCabinAt(KwCabinSpec.Over(range, sizes, seed, upper.Spec.Openings), frame);
            KwCabinAt set = new KwCabinAt(KwCabinSpec.Over(range, sizes, seed, Openings(upper, plain)), frame);
            moved = Walls(upper, set);
            return set;
        }

        // The four insets on the new roof, each wall where it stood (its place along the keel line, or across from the hull's
        // inside at the cabins' reference height), the height as asked.
        private static KwCabinSizes Insets(KwCabinAt upper, KwCabinSizes asked, ICollection<string> typed, KwCabinAt was, KwStackFrame frame, KwCabinRange range)
        {
            float start = upper.Start, lower = was.Start, edge = frame.Along(frame.Origin);
            float back = Wall(typed, KwCabinForm.Back, asked.Back, start + asked.Back, lower + was.Spec.X0, edge, 1f);
            float front = Wall(typed, KwCabinForm.Front, asked.Front, start + upper.Spec.Length - asked.Front, lower + was.Spec.X1, edge + range.Length, -1f);
            float left = Wall(typed, KwCabinForm.Left, asked.Left, upper.Spec.Left.Base + asked.Left, was.Spec.Left.Total, range.LeftBase, 1f);
            float right = Wall(typed, KwCabinForm.Right, asked.Right, upper.Spec.Right.Base + asked.Right, was.Spec.Right.Total, range.RightBase, 1f);
            return new KwCabinSizes(asked.Height, back, front, left, right);
        }

        // One wall's inset on the new roof: as typed; 0 when it stood on the lower cabin's wall; else from the new edge it is
        // measured in from to where it stood (way: 1 when the inset runs with the measure, -1 against it), at least 0: the
        // edge drawn in past it draws it in onto the edge.
        private static float Wall(ICollection<string> typed, string key, float asked, float stood, float under, float edge, float way)
        {
            if (typed.Contains(key))
                return asked;
            if (asked < None && Mathf.Abs(stood - under) < Flush)
                return 0f;
            return Mathf.Max(0f, (stood - edge) * way);
        }

        // Whether any of a cabin's walls stands somewhere else, as built: its ends along the keel line, its sides across.
        private static bool Walls(KwCabinAt a, KwCabinAt b) =>
            Mathf.Abs(a.Start + a.Spec.X0 - b.Start - b.Spec.X0) > Flush || Mathf.Abs(a.Start + a.Spec.X1 - b.Start - b.Spec.X1) > Flush
            || Mathf.Abs(a.Spec.Left.Total - b.Spec.Left.Total) > Flush || Mathf.Abs(a.Spec.Right.Total - b.Spec.Right.Total) > Flush;

        // The upper cabin's openings carried to where it now stands: every sill raised with its deck, each one's place along its
        // wall kept where it was in the world (a raised side's u is the cabin's own x, an own wall's from its line's start).
        private static List<KwOpening> Openings(KwCabinAt was, KwCabinAt now)
        {
            float rise = now.Floor - was.Floor, slide = now.Start - was.Start;
            if (was.Spec.Openings.Count == 0)
                return was.Spec.Openings;
            List<KwCabinWall> before = KwCabinWalls.Of(was.Spec), after = KwCabinWalls.Of(now.Spec);
            return was.Spec.Openings.ConvertAll(o => o.With(o.Shape, o.U - Slid(o.Wall, before, after, slide), o.Sill + rise, o.Width, o.Height, o.Frame, o.Cover));
        }

        // How far a wall's u moved under its openings: a raised side's by the frame's slide; an own wall's by its line's start
        // moved along the line (both frames on one keel line); none for a wall it no longer has.
        private static float Slid(string wall, List<KwCabinWall> before, List<KwCabinWall> after, float slide)
        {
            if (wall == KwOpening.RaisedLeft || wall == KwOpening.RaisedRight)
                return slide;
            KwCabinWall? a = before.Find(w => w.Name == wall), b = after.Find(w => w.Name == wall);
            if (a == null || b == null || a.Path.Count < 2)
                return 0f;
            Vector2 way = (a.Path[1] - a.Path[0]).normalized;
            return Vector2.Dot(b.Path[0] + new Vector2(slide, 0f) - a.Path[0], way);
        }
    }
}
