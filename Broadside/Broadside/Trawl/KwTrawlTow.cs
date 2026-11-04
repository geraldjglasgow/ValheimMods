using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Where the net is in the water: its tow point (the model's <c>tow_attach</c>), the way it trails from the davit's
    /// sheave and how far its bag droops, worked out each frame on every peer that has the winch (and on its owner, a
    /// dedicated server too, for the catch), from the sheave as drawn, the ship's speed and the rope out. Plain maths, no
    /// physics: the net keeps its place in the water and the rope pulls it along (a towed point): it trails behind
    /// whatever the ship does, cuts the corner of a turn and never stretches the rope. Slow, it sinks deep and hangs
    /// nearer the stern (the rope drooping); faster it rises and streams out to the rope's reach. Every move is eased, so
    /// it follows the ship smoothly. The sea floor is looked at four times a second under the tow point and the bag: the
    /// net is kept clear of it, and while the floor holds it up it is snagged (<see cref="Snagged"/>: no catch).
    /// </summary>
    public sealed class KwTrawlTow
    {
        private const float DepthEase = 1.5f, ReachEase = 2f, PitchEase = 0.8f, FloorEvery = 0.25f, Slackness = 0.45f;
        private const float HangPitch = 90f, SlowPitch = 20f, FastPitch = 5f, Hanging = 4f, SnagRise = 0.3f;

        private Vector3 at, way;
        private float depth, reach, pitch, floor = float.MinValue, nextFloor;
        private bool started;

        /// <summary>The tow point in the world.</summary>
        public Vector3 At => at;

        /// <summary>The way the net trails from the sheave, level, unit length.</summary>
        public Vector3 Way => way;

        /// <summary>How far the bag droops below level (degrees; 90 hanging straight down at the davit).</summary>
        public float Pitch => pitch;

        /// <summary>Whether the sea floor holds the net up.</summary>
        public bool Snagged { get; private set; }

        /// <summary>Whether the net is being followed (it is in the water).</summary>
        public bool Started => started;

        /// <summary>The net out of the water: the next cast begins at the davit.</summary>
        public void Reset() => (started, Snagged, floor) = (false, false, float.MinValue);

        public void Step(Vector3 sheave, Vector3 astern, float speed, float rope, float dt)
        {
            float share = KwTrawlTune.Ease(speed / KwTrawlTune.Stream), water = KwTrawlSea.Water;
            float most = Reach(sheave, rope, water, Depth(share));
            if (!started)
                Begin(sheave, astern, rope, share, most);
            depth += (Depth(share) - depth) * Eased(dt, DepthEase);
            reach += (most * Mathf.Lerp(Slackness, 1f, share) - reach) * Eased(dt, ReachEase);
            Towed(sheave, rope, water);
            pitch += (Droop(share, rope) - pitch) * Eased(dt, PitchEase);
            Floored(rope);
        }

        // The net's first place: at the davit for a cast; already out for one this game first sees out (a late joiner).
        private void Begin(Vector3 sheave, Vector3 astern, float rope, float share, float most)
        {
            way = Level(astern, Vector3.back);
            (depth, pitch, started, nextFloor) = (Depth(share), Droop(share, rope), true, 0f);
            reach = most * Mathf.Lerp(Slackness, 1f, share);
            bool whole = rope >= KwTrawlTune.RopeOut - 0.01f;
            at = whole ? sheave + way * reach : sheave + Vector3.down * KwTrawlTune.RopeHang;
            if (whole)
                at.y = KwTrawlSea.Water - depth;
        }

        // Towed: the way from the sheave to where the net was, its distance no more than the reach, at its depth; the rope
        // never longer than paid out.
        private void Towed(Vector3 sheave, float rope, float water)
        {
            Vector3 flat = at - sheave;
            flat.y = 0f;
            float length = flat.magnitude;
            if (length > 0.3f)
                way = flat / length;
            Vector3 next = sheave + way * Mathf.Min(length, reach);
            next.y = water - depth;
            Vector3 off = next - sheave;
            at = off.sqrMagnitude > rope * rope ? sheave + off.normalized * rope : next;
        }

        // Kept clear of the sea floor under the tow point and the bag: snagged while the floor lifts it.
        private void Floored(float rope)
        {
            if (Time.time >= nextFloor)
            {
                nextFloor = Time.time + FloorEvery;
                floor = Mathf.Max(KwTrawlSea.Floor(at), KwTrawlSea.Floor(at + way * 6f));
            }
            float lowest = floor + KwTrawlTune.Clearance + Below(pitch, KwTrawlTune.Bunch(rope));
            Snagged = at.y < lowest - SnagRise;
            if (at.y < lowest)
                at.y = lowest;
        }

        /// <summary>How far the net reaches under its tow point at a droop and scale (its weights and its tail, the lower).</summary>
        public static float Below(float pitch, Vector3 scale)
        {
            float s = Mathf.Sin(pitch * Mathf.Deg2Rad), c = Mathf.Cos(pitch * Mathf.Deg2Rad);
            float weights = 4.5f * scale.z * s + 1.86f * scale.y * c, tail = 10.9f * scale.z * s + 0.81f * scale.y * c;
            return Mathf.Max(weights, tail);
        }

        private static float Depth(float share) => Mathf.Lerp(KwTrawlTune.DeepSlow, KwTrawlTune.DeepFast, share);

        // How far out the rope lets the net trail at that depth.
        private static float Reach(Vector3 sheave, float rope, float water, float depth)
        {
            float drop = sheave.y - (water - depth);
            return Mathf.Sqrt(Mathf.Max(rope * rope - drop * drop, 0f));
        }

        // The bag's droop: more slow, little streaming; straight down as the rope comes in to the davit.
        private static float Droop(float share, float rope) =>
            Mathf.Lerp(HangPitch, Mathf.Lerp(SlowPitch, FastPitch, share), KwTrawlTune.Ease((rope - KwTrawlTune.RopeHang) / (Hanging - KwTrawlTune.RopeHang)));

        private static Vector3 Level(Vector3 way, Vector3 otherwise)
        {
            way.y = 0f;
            return way.sqrMagnitude > 1e-4f ? way.normalized : otherwise;
        }

        private static float Eased(float dt, float seconds) => 1f - Mathf.Exp(-Mathf.Max(dt, 0f) / seconds);
    }

    /// <summary>
    /// The ship's speed through the water as this peer sees it move (level, metres a second, smoothed over about half a
    /// second): the same on every screen near enough, owner or not, with no velocity read from a body another game runs.
    /// </summary>
    public sealed class KwTrawlSpeed
    {
        private const float Smooth = 0.6f, Jump = 20f;
        private Vector3 last;
        private bool known;

        public float Value { get; private set; }

        public void Step(Vector3 at, float dt)
        {
            if (!known || dt <= 1e-5f || (at - last).sqrMagnitude > Jump * Jump)
            {
                (last, known) = (at, true);
                return;
            }
            Vector3 moved = at - last;
            moved.y = 0f;
            last = at;
            Value += (moved.magnitude / dt - Value) * (1f - Mathf.Exp(-dt / Smooth));
        }
    }
}
