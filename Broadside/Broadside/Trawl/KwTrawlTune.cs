using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The trawl's numbers and timings (<c>Trawl/TRAWL.md</c>, "Numbers"), plain arithmetic: how much tow rope is out, how
    /// full the drum's roll of net is, where the davit stands and how big the net is drawn at each moment of each step, and
    /// how well the net fishes at a speed. Every peer works its drawing out from the winch's step and how long it has been
    /// in it (<see cref="KwTrawlState"/>), so all draw the same and nothing more is sent.
    /// </summary>
    public static class KwTrawlTune
    {
        /// <summary>Paying out and hauling in, seconds.</summary>
        public const float CastTime = 5f, HaulTime = 6f;

        /// <summary>
        /// Landing, seconds into it: the davit swung in over the deck, the bag opened (the fish fall), the bag emptied,
        /// the net wound onto the drum, the davit swung back out (the net stowed).
        /// </summary>
        public const float SwungIn = 2f, DropAt = 2.4f, Emptied = 3.4f, Wound = 5f, LandTime = 6.6f;

        /// <summary>The tow rope paid out (m); what is left out with the net hanging at the davit; the rope at which the net is whole.</summary>
        public const float RopeOut = 14f, RopeHang = 0.3f, Unbunched = 6f;

        /// <summary>The tow point's depth under the surface (m): slow, the net sinks; at <see cref="Stream"/> m/s and over it streams.</summary>
        public const float DeepSlow = 3.5f, DeepFast = 1.2f, Stream = 5f;

        /// <summary>The speed band (m/s): none under Slack, full from Trawling to Lifting, falling to SkipShare at Skipping and over.</summary>
        public const float Slack = 0.5f, Trawling = 1.5f, Lifting = 4.5f, Skipping = 8f, SkipShare = 0.25f;

        /// <summary>Seconds of trawling in the band per fish.</summary>
        public const float SecondsPerFish = 30f;

        /// <summary>The most fish one net holds.</summary>
        public const int Capacity = 12;

        /// <summary>Each net in the water adds this share of the ship's own drag along it (x1.4: about 15% off its top speed).</summary>
        public const float DragPerNet = 0.4f;

        /// <summary>Water needed astern to cast (m), and the room kept between the net and the sea floor.</summary>
        public const float MinWater = 3f, Clearance = 0.3f;

        /// <summary>The roll of net on the drum: its radius bare and full (m).</summary>
        public const float RollEmpty = 0.11f, RollFull = 0.23f;

        /// <summary>The share of a full rate the net fishes at a speed (m/s).</summary>
        public static float Share(float speed)
        {
            if (speed <= Slack)
                return 0f;
            if (speed < Trawling)
                return Ease((speed - Slack) / (Trawling - Slack));
            if (speed <= Lifting)
                return 1f;
            return Mathf.Lerp(1f, SkipShare, Mathf.Clamp01((speed - Lifting) / (Skipping - Lifting)));
        }

        /// <summary>The tow rope out (m) at a moment of a step.</summary>
        public static float Rope(KwTrawlStep step, float t) => step switch
        {
            KwTrawlStep.Casting => Mathf.Lerp(RopeHang, RopeOut, Ease(t / CastTime)),
            KwTrawlStep.Out => RopeOut,
            KwTrawlStep.Hauling => Mathf.Lerp(RopeOut, RopeHang, Ease(t / HaulTime)),
            KwTrawlStep.Landing => RopeHang,
            _ => 0f,
        };

        /// <summary>How full the drum's roll of net is, 0 (bare: the net out) to 1 (stowed).</summary>
        public static float Roll(KwTrawlStep step, float t) => step switch
        {
            KwTrawlStep.Casting => 1f - Ease(t / CastTime),
            KwTrawlStep.Out => 0f,
            KwTrawlStep.Hauling => 0.15f * Mathf.Clamp01(t / HaulTime),
            KwTrawlStep.Landing => Mathf.Lerp(0.15f, 1f, Ease((t - Emptied) / (Wound - Emptied))),
            _ => 1f,
        };

        /// <summary>The davit's swing (degrees about its post): out over the water at 0, in over the deck at 180 while landing.</summary>
        public static float Swing(KwTrawlStep step, float t)
        {
            if (step != KwTrawlStep.Landing)
                return 0f;
            if (t < SwungIn)
                return 180f * Ease(t / SwungIn);
            return t < Wound ? 180f : 180f * (1f - Ease((t - Wound) / (LandTime - Wound)));
        }

        /// <summary>How much of the net is drawn: 0 none (on the drum), 1 all; it goes onto the drum while landing.</summary>
        public static float Shown(KwTrawlStep step, float t)
        {
            if (step == KwTrawlStep.Stowed)
                return 0f;
            return step != KwTrawlStep.Landing || t < Emptied ? 1f : 1f - Ease((t - Emptied) / (Wound - Emptied));
        }

        /// <summary>The emptied bag's shake (degrees), dying away after it opens.</summary>
        public static float Wobble(float t)
        {
            if (t < DropAt || t > Emptied)
                return 0f;
            float into = t - DropAt;
            return 12f * Mathf.Sin(into * 20f) * (1f - into / (Emptied - DropAt));
        }

        /// <summary>The net's scale (x, y, z) by the rope out: bunched up hanging at the davit, whole once the rope is out past <see cref="Unbunched"/>.</summary>
        public static Vector3 Bunch(float rope)
        {
            float e = Ease((rope - RopeHang) / (Unbunched - RopeHang));
            return new Vector3(Mathf.Lerp(0.18f, 1f, e), Mathf.Lerp(0.2f, 1f, e), Mathf.Lerp(0.12f, 1f, e));
        }

        public static float Ease(float x) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(x));
    }
}
