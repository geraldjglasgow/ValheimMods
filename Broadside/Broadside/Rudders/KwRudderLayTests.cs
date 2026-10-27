using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// How many of a lay's moving and standing parts would pass through the stern's pieces (<see cref="KwRudderSweep"/>),
    /// each part as the round bar it is, at the helm amidships and hard over each way: the rudder's head and the stock
    /// over the post's top; a tiller to its grip; or the yoke's arms, each link from the yoke's eye to the deck tiller's,
    /// the deck tiller's arms and handle and its pivot post; and the helmsman standing at the grip. Counting stops at a
    /// limit (the best lay so far), since a lay with more is not taken. Kit axes of the rudder.
    /// </summary>
    public sealed class KwRudderLayTests
    {
        private const float Link = 0.025f, DeckBar = 0.04f, Man = 0.22f;
        private static readonly float[] Angles = { -KwRudder.MaxAngle, 0f, KwRudder.MaxAngle };
        private readonly IKwRudderStern sweep;
        private readonly Vector3 stock;
        private readonly float top, t, radius;

        public KwRudderLayTests(IKwRudderStern sweep, KwRudderTier tier, KwRudderFit fit)
        {
            this.sweep = sweep;
            stock = tier.Pivot(fit);
            top = fit.Height;
            t = fit.Thick;
            radius = tier.Stock(fit);
        }

        /// <summary>A tiller's: the head, and the tiller and the helmsman at its grip at each angle.</summary>
        public int Tiller(KwRudderLay lay, int limit)
        {
            int blocked = Head(lay);
            Vector3 from = Yoke(lay), grip = lay.Grip;
            foreach (float angle in Angles)
            {
                if (blocked >= limit)
                    break;
                Vector3 end = from + Turn(grip - from, angle);
                blocked += Count(sweep.Clear(from, end, 0.045f * t)) + Helmsman(end, lay.Deck);
            }
            return blocked;
        }

        /// <summary>A yoke's alone: the head and the yoke's arms at each angle.</summary>
        public int Yoke(KwRudderLay lay, int limit)
        {
            int blocked = Head(lay);
            foreach (float angle in Angles)
            {
                for (int side = -1; side <= 1 && blocked < limit; side += 2)
                    blocked += Count(sweep.Clear(Yoke(lay), Yoke(lay) + Arm(lay, angle, side), 0.04f * t));
            }
            return blocked;
        }

        /// <summary>Lines to a deck tiller: the yoke's, the links, the deck tiller and its post, and the helmsman, at each angle.</summary>
        public int Lines(KwRudderLay lay, int limit)
        {
            int blocked = Yoke(lay, limit) + Post(lay);
            Vector3 yoke = Yoke(lay), pivot = lay.Pivot;
            foreach (float angle in Angles)
            {
                for (int side = -1; side <= 1 && blocked < limit; side += 2)
                {
                    Vector3 arm = Arm(lay, angle, side);
                    blocked += Count(sweep.Clear(yoke + arm, pivot + arm, Link)) + Count(sweep.Clear(pivot, pivot + arm, DeckBar));
                }
                if (blocked >= limit)
                    break;
                Vector3 grip = pivot + Turn(new Vector3(KwRudderLay.Handle, 0f, 0f), angle);
                blocked += Count(sweep.Clear(pivot, grip, DeckBar)) + Helmsman(grip, lay.Deck);
            }
            return blocked;
        }

        /// <summary>How steeply a lay's links fall from the yoke to the deck tiller (rise over run).</summary>
        public float Slope(KwRudderLay lay) => (Yoke(lay).z - lay.Pivot.z) / Mathf.Max(lay.Reach - stock.x, 0.1f);

        // The stock over the post's top up to the head, and the head itself.
        private int Head(KwRudderLay lay)
        {
            float foot = top + lay.Lift, head = 0.1f * t;
            int blocked = Count(sweep.Clear(stock + Up(foot + head), stock + Up(foot + Mathf.Max(head, 0.17f * t - head)), head));
            return lay.Lift > radius + 0.06f ? blocked + Count(sweep.Clear(stock + Up(top + 0.03f + radius), stock + Up(foot + head), radius)) : blocked;
        }

        // The deck tiller's pivot post, from just over the deck to under the deck tiller.
        private int Post(KwRudderLay lay) => Count(sweep.Clear(
            new Vector3(lay.Reach, 0f, lay.Deck + 0.15f), new Vector3(lay.Reach, 0f, lay.Deck + KwRudderLay.GripHeight - 0.08f), 0.07f));

        // The helmsman standing on the deck to port of a grip, from his knees to his head.
        private int Helmsman(Vector3 grip, float deck)
        {
            Vector3 feet = new Vector3(grip.x, grip.y + KwRudderLay.Side, deck);
            return Count(sweep.Clear(feet + Up(0.5f), feet + Up(1.6f), Man));
        }

        // The yoke's middle on the stock, and its arm toward a side at an angle.
        private Vector3 Yoke(KwRudderLay lay) => stock + Up(top + lay.Lift + 0.07f * t);

        private static Vector3 Arm(KwRudderLay lay, float angle, int side) => Turn(new Vector3(0f, side * lay.Half, 0f), angle);

        private static Vector3 Up(float z) => new Vector3(0f, 0f, z);

        private static int Count(bool clear) => clear ? 0 : 1;

        /// <summary>A kit vector turned about the up axis by degrees (toward port positive).</summary>
        public static Vector3 Turn(Vector3 v, float degrees)
        {
            float a = degrees * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
            return new Vector3(v.x * c - v.y * s, v.x * s + v.y * c, v.z);
        }
    }
}
