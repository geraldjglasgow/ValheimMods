using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Picks how a rudder on the dock is steered (<see cref="KwRudderLay"/>) from the stern it hangs on, measured from the
    /// stern's own pieces (<see cref="KwRudderSweep"/>): the post's height and depth (its fit), whether a cap stands on its
    /// top, the deck before it and its height, and the railings, planking and anything else in the way. The helm stands
    /// at the first deck forward of the post wide enough for the helmsman. A tiller over the post's top is taken when the
    /// post has no cap, its top is under the hand and the tiller swings clear; else a yoke over the post's top with lines
    /// or rods round the post's sides, over or through the rail, to a deck tiller on its own post, the nearest helm, lowest
    /// head and shortest yoke that pass nothing (<see cref="KwRudderLayTests"/>; with none clear, the one through least),
    /// its links falling no steeper than <see cref="Steep"/> where one that clear is found.
    /// With no deck before the post: the tiller over the post's top, or a yoke alone on a capped post.
    /// </summary>
    public static class KwRudderLayFind
    {
        private const float Step = 0.15f, Ahead = 4.5f, Level = 0.3f, LongestLift = 1.2f;

        /// <summary>The steepest the links may fall (rise over run, about 50°) before a gentler lay is looked for.</summary>
        public const float Steep = 1.2f;
        private static readonly float[] Lifts = { 0.02f, 0.15f, 0.3f, 0.45f };
        private static readonly float[] Stations = { 0.1f, 0.4f, 0.7f, 1f, 1.4f, 1.9f, 2.5f };

        public static KwRudderLay Find(Transform rudder, KwRudderTier tier, KwRudderFit fit) => Find(new KwRudderSweep(rudder), tier, fit);

        /// <summary>The lay for a rudder on a stern however it is felt (the game's colliders, or a test's meshes).</summary>
        public static KwRudderLay Find(IKwRudderStern sweep, KwRudderTier tier, KwRudderFit fit)
        {
            KwRudderLayTests tests = new KwRudderLayTests(sweep, tier, fit);
            bool capped = sweep.Capped(new Vector3(fit.PostDepth / 2f, 0f, fit.Height));
            float? start = Start(sweep, fit);
            if (start == null)
                return capped ? Yoke(tests, fit) : KwRudderLay.Plain;
            Best best = new Best();
            if (!capped)
                Tillers(sweep, tests, fit, start.Value, best);
            if (best.Score > 0f)
                Lines(sweep, tests, fit, start.Value, best);
            return best.Lay ?? (capped ? Yoke(tests, fit) : KwRudderLay.Plain);
        }

        // The first place forward of the post where the deck runs under the centre line and both sides of it.
        private static float? Start(IKwRudderStern sweep, KwRudderFit fit)
        {
            for (float x = fit.PostDepth + 0.25f; x <= fit.PostDepth + Ahead; x += Step)
            {
                float? deck = sweep.DeckAt(x, 0f, fit.Height);
                if (deck != null && Near(sweep.DeckAt(x, KwRudderLay.Side, fit.Height), deck) && Near(sweep.DeckAt(x, -KwRudderLay.Side, fit.Height), deck))
                    return x;
            }
            return null;
        }

        // A tiller's grip a little way forward of the first deck: its head as high as the hand, if that is over the post's top.
        private static void Tillers(IKwRudderStern sweep, KwRudderLayTests tests, KwRudderFit fit, float start, Best best)
        {
            for (int i = 0; i < 4 && best.Score > 0f; i++)
            {
                float x = Mathf.Max(start, fit.PostDepth + 0.6f) + 0.3f * i;
                float? deck = Deck(sweep, fit, x, x);
                float lift = deck.HasValue ? deck.Value + KwRudderLay.GripHeight - 0.1f * fit.Thick - fit.Height : -1f;
                if (lift < 0.02f || lift > LongestLift)
                    continue;
                KwRudderLay lay = new KwRudderLay(KwRudderHelm.Tiller, lift, x, deck!.Value, 0f);
                best.Offer(lay, tests.Tiller(lay, best.Limit));
            }
        }

        // A deck tiller's post at each station forward of the first deck, with each head height and yoke length.
        private static void Lines(IKwRudderStern sweep, KwRudderLayTests tests, KwRudderFit fit, float start, Best best)
        {
            float shortest = Mathf.Max(0.42f, fit.PostHalf + 0.1f);
            foreach (float ahead in Stations)
            {
                float x = start + ahead;
                float? deck = Deck(sweep, fit, x, x + KwRudderLay.Handle);
                if (deck == null)
                    continue;
                foreach (float lift in Lifts)
                {
                    foreach (float half in new[] { shortest, shortest + 0.13f })
                    {
                        KwRudderLay lay = new KwRudderLay(KwRudderHelm.Lines, lift, x, deck.Value, half);
                        float steep = tests.Slope(lay) > Steep ? 0.5f : 0f;
                        if (best.Offer(lay, tests.Lines(lay, best.Limit) + steep))
                            return;
                    }
                }
            }
        }

        // A yoke alone on a capped post with no deck before it: the lowest head whose yoke swings clear of the cap.
        private static KwRudderLay Yoke(KwRudderLayTests tests, KwRudderFit fit)
        {
            Best best = new Best();
            foreach (float lift in Lifts)
            {
                KwRudderLay lay = new KwRudderLay(KwRudderHelm.Yoke, lift, 0f, 0f, Mathf.Max(0.42f, fit.PostHalf + 0.1f));
                if (best.Offer(lay, tests.Yoke(lay, best.Limit)))
                    break;
            }
            return best.Lay ?? KwRudderLay.Plain;
        }

        // The deck's top on the centre line at x, if the deck under the helmsman's feet (to port at x2) is about as high.
        private static float? Deck(IKwRudderStern sweep, KwRudderFit fit, float x, float x2)
        {
            float? deck = sweep.DeckAt(x, 0f, fit.Height);
            return deck != null && Near(sweep.DeckAt(x2, KwRudderLay.Side, fit.Height), deck) ? deck : null;
        }

        private static bool Near(float? a, float? b) => a.HasValue && b.HasValue && Mathf.Abs(a.Value - b.Value) < Level;

        // The lay through the fewest pieces so far (the first of equals), half a piece more for steep links; true once one
        // passes through none.
        private sealed class Best
        {
            public KwRudderLay? Lay { get; private set; }
            public float Score { get; private set; } = float.MaxValue;

            /// <summary>Where a lay's tests may stop counting: it cannot beat the best once it reaches this.</summary>
            public int Limit => Score >= int.MaxValue ? int.MaxValue : Mathf.CeilToInt(Score);

            public bool Offer(KwRudderLay lay, float score)
            {
                if (score < Score)
                    (Lay, Score) = (lay, score);
                return Score <= 0f;
            }
        }
    }
}
