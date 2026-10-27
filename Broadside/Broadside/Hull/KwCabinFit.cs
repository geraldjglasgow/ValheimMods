using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The cabin a saved text or the Cabin tool's window asks for (<see cref="KwCabinSpec"/>), made to fit the hull it
    /// stands in, so every text builds something (the user's report, 2026-10-25: a side or back inset over a pointed stern
    /// was refused, and the cabin vanished from the preview). The height is kept within the tool's limits, every inset at 0
    /// or more, and the two ends' insets leave the cabin <see cref="KwCabinOutline.MinLength"/> of deck. An end with a
    /// bulkhead (any end the hull does not close: drawn in, or a side drawn in) moves on in from where it was asked to the
    /// nearest place the cabin is <see cref="KwCabinOutline.MinWide"/> across at its deck and its roof: over a pointed
    /// stern with a walkway the back end stands a little forward and the walkway runs on round it to the stern. Only where
    /// no place along the cabin would do (its sides drawn in so far they meet) are the sides' insets limited, the larger
    /// first, to the most that fits. What was changed is said in the window's words (<see cref="KwCabinSpec.Fitted"/>), and
    /// the text keeps what was asked, so bringing the sides back to the hull brings the end back to the stern. A cabin no
    /// place of which is wide or long enough even with no insets says why (<see cref="KwCabinSpec.Unbuilt"/>) and is drawn
    /// as a plain frame (<see cref="KwCabinShape"/>). Pure numbers, the same on every peer.
    /// </summary>
    public static class KwCabinFit
    {
        // How far an end steps in at a time as it looks for room; how many halvings find its place then, and the sides' limit.
        private const float Step = 0.05f;
        private const int Halvings = 6, Tries = 12;

        /// <summary>The cabin that is built for what was asked: fitted, with what was changed, or why it cannot be built.</summary>
        public static KwCabinSpec Of(KwCabinSpec asked)
        {
            List<string> notes = new List<string>();
            KwCabinSpec sane = Sane(asked, notes);
            KwCabinSpec? built = Ends(sane, notes) ?? Sides(sane, notes);
            return built != null ? built.Fit(asked, notes, null) : sane.Fit(asked, notes, Why(sane));
        }

        // The height within the tool's limits, every inset 0 or more, the ends' insets leaving the shortest cabin.
        private static KwCabinSpec Sane(KwCabinSpec a, List<string> notes)
        {
            float height = Mathf.Clamp(a.Height, KwCabinSpec.MinHeight, KwCabinSpec.MaxHeight);
            if (Mathf.Abs(height - a.Height) > 0.005f)
                notes.Add($"Height {(height > a.Height ? "raised" : "limited")} to {height:0.##} m.");
            float left = Zero(a.Left.Inset, "left", notes), right = Zero(a.Right.Inset, "right", notes);
            (float back, float front) = Length(a, Zero(a.Back.Inset, "back", notes), Zero(a.Front.Inset, "front", notes), notes);
            return a.Made(a.Floor + height, new KwCabinEnd(a.Back.Hull, back), new KwCabinEnd(a.Front.Hull, front),
                new KwCabinSide(Mathf.Max(0f, a.Left.Base), left), new KwCabinSide(Mathf.Max(0f, a.Right.Base), right));
        }

        // An inset read as 0 when it is less (a text written by hand, or broken).
        private static float Zero(float inset, string name, List<string> notes)
        {
            if (inset >= 0f)
                return inset;
            notes.Add($"The {name} inset reads as 0: insets are 0 or more.");
            return 0f;
        }

        // The two ends' insets cut back, the larger first, so the cabin keeps its shortest length of deck.
        private static (float Back, float Front) Length(KwCabinSpec a, float back, float front, List<string> notes)
        {
            float room = Mathf.Max(0f, a.Length - KwCabinOutline.MinLength);
            if (back + front <= room + 0.001f)
                return (back, front);
            float least = Mathf.Min(back, front), cap = least * 2f > room ? room / 2f : room - least;
            float b = Mathf.Floor(Mathf.Min(back, cap) * 100f) / 100f, f = Mathf.Floor(Mathf.Min(front, cap) * 100f) / 100f;
            if (b < back)
                notes.Add($"Back inset limited to {b:0.##} m: the cabin keeps {KwCabinOutline.MinLength:0.#} m of deck.");
            if (f < front)
                notes.Add($"Front inset limited to {f:0.##} m: the cabin keeps {KwCabinOutline.MinLength:0.#} m of deck.");
            return (b, f);
        }

        // The ends moved in where their bulkheads need room, the cabin between them wide and long enough; null when it is not.
        private static KwCabinSpec? Ends(KwCabinSpec s, List<string>? notes)
        {
            float back = End(s, false), front = End(s, true);
            if (float.IsNaN(back) || float.IsNaN(front) || s.Length - back - front < KwCabinOutline.MinLength - 0.001f)
                return null;
            KwCabinSpec moved = s.Made(s.Top, new KwCabinEnd(s.Back.Hull, back), new KwCabinEnd(s.Front.Hull, front), s.Left, s.Right);
            if (!Clear(moved))
                return null;
            Moved(notes, "back", s.Back.Inset, back);
            Moved(notes, "front", s.Front.Inset, front);
            return moved;
        }

        // How far in an end stands: where it was asked when the hull closes it or its bulkhead has room there, else the
        // nearest place on in (to the centimetre) that is wide enough; NaN for none short of the other end's asked place.
        private static float End(KwCabinSpec s, bool front)
        {
            KwCabinEnd end = front ? s.Front : s.Back;
            if (KwCabinOutline.Closed(s, end))
                return end.Inset;
            float way = front ? -1f : 1f, from = front ? s.Length - end.Inset : end.Inset, room = s.Length - s.Back.Inset - s.Front.Inset;
            float last = from;
            for (int i = 0; i * Step <= room + 1e-4f; i++)
            {
                float x = from + way * Mathf.Min(i * Step, room);
                if (Wide(s, x))
                    return i == 0 ? end.Inset : Mathf.Ceil(Mathf.Abs(Edge(s, last, x) - (front ? s.Length : 0f)) * 100f - 0.001f) / 100f;
                last = x;
            }
            return float.NaN;
        }

        // The place between one too narrow and one wide enough where the cabin becomes wide enough, on the wide side.
        private static float Edge(KwCabinSpec s, float narrow, float wide)
        {
            for (int i = 0; i < Halvings; i++)
            {
                float mid = (narrow + wide) / 2f;
                if (Wide(s, mid))
                    wide = mid;
                else
                    narrow = mid;
            }
            return wide;
        }

        private static bool Wide(KwCabinSpec s, float x) => KwCabinOutline.Across(s, x) >= KwCabinOutline.MinWide - 0.001f;

        // A cabin with a side drawn in is wide enough everywhere along it, between its ends too (both sides at the hull: the hull's own shape).
        private static bool Clear(KwCabinSpec s)
        {
            if (s.Left.AtHull && s.Right.AtHull)
                return true;
            return KwCabinOutline.Xs(s).TrueForAll(x => KwCabinOutline.Across(s, x) >= KwCabinOutline.MinWide - 0.005f);
        }

        private static void Moved(List<string>? notes, string name, float asked, float now)
        {
            if (now > asked + 0.005f)
                notes?.Add($"The hull is too narrow there: the {name} end is drawn in {now:0.##} m to fit.");
        }

        // Drawn in so far no place fits: the sides' insets limited, the larger first, to the most that fits; null when even none fits.
        private static KwCabinSpec? Sides(KwCabinSpec s, List<string> notes)
        {
            float most = Mathf.Max(s.Left.Inset, s.Right.Inset), lo = 0f, hi = most;
            if (most <= 0f || Ends(Capped(s, 0f), null) == null)
                return null;
            for (int i = 0; i < Tries; i++)
            {
                float mid = (lo + hi) / 2f;
                if (Ends(Capped(s, mid), null) != null)
                    lo = mid;
                else
                    hi = mid;
            }
            KwCabinSpec capped = Capped(s, Mathf.Floor(lo * 100f) / 100f);
            Limited(notes, "Left", s.Left.Inset, capped.Left.Inset);
            Limited(notes, "Right", s.Right.Inset, capped.Right.Inset);
            return Ends(capped, notes);
        }

        // The cabin with neither side drawn in more than a limit.
        private static KwCabinSpec Capped(KwCabinSpec s, float most) => s.Made(s.Top, s.Back, s.Front,
            new KwCabinSide(s.Left.Base, Mathf.Min(s.Left.Inset, most)), new KwCabinSide(s.Right.Base, Mathf.Min(s.Right.Inset, most)));

        private static void Limited(List<string> notes, string name, float asked, float now)
        {
            if (now < asked - 0.005f)
                notes.Add($"{name} inset limited to {now:0.##} m here: the hull narrows.");
        }

        // Why no cabin fits: the deck sections too short, or the hull too narrow all along them even with no insets.
        private static string Why(KwCabinSpec s) => s.Length < KwCabinOutline.MinLength
            ? $"The deck here is under {KwCabinOutline.MinLength:0.#} m long: no cabin fits"
            : $"The hull is under {KwCabinOutline.MinWide:0.#} m wide inside here: no cabin fits";
    }
}
