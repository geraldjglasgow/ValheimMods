using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A dock started on a dock section: within <see cref="KwPierAim.EndZone"/> of its end (when no loaded section continues
    /// it there), straight on from that end; within it of its start (when no other dock meets it there), straight on from its
    /// start, away from it; anywhere else, square off the side aimed at, from the deck's edge, its centre at the nearest
    /// half-metre along (kept at least 0.75 m from the section's ends). It starts at that section's deck height (its width
    /// and material are the tool's). Plain maths on the section's saved shape and where it stands.
    /// </summary>
    public static class KwPierJoin
    {
        private const float Step = 0.5f, Inset = 0.75f;

        public static KwPierStart? Of(KwPier pier, Vector3 point)
        {
            KwPierDeck? deck = KwPierDeck.Of(pier);
            if (deck == null)
                return null;
            Vector3 local = point - deck.Start;
            float along = Vector3.Dot(local, deck.Forward), across = Vector3.Dot(local, deck.Right);
            KwPierStart start = new KwPierStart { Joins = pier, OnDock = true };
            if (along > deck.Length - KwPierAim.EndZone && !deck.ContinuedAtEnd())
                (start.At, start.Way, start.Says) = (deck.End, deck.Forward, "continuing this dock");
            else if (along < KwPierAim.EndZone && !deck.ContinuedAtStart())
                (start.At, start.Way, start.Says) = (deck.Start, -deck.Forward, "continuing this dock from its start");
            else
                Side(start, deck, along, across >= 0f ? 1f : -1f);
            return start;
        }

        // Square off a side, from its edge.
        private static void Side(KwPierStart start, KwPierDeck deck, float along, float side)
        {
            float at = Mathf.Round(along / Step) * Step;
            at = deck.Length > 2f * Inset ? Mathf.Clamp(at, Inset, deck.Length - Inset) : deck.Length * 0.5f;
            (start.At, start.Way, start.Says) = (deck.At(side * deck.Width * 0.5f, at), deck.Right * side, "branching off this dock");
        }
    }
}
