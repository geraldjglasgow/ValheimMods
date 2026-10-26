using MannedWeapons;
using UnityEngine;

namespace EliteCreaturesPack.Ballista
{
    /// <summary>The string and what lies in the groove: the reload's steps done (slack, drawn and caught by the latch, or drawn with a missile laid).</summary>
    public enum Spring
    {
        Slack = 0,
        Drawn = 1,
        Loaded = 2,
    }

    /// <summary>The reload's step under way: nothing, pulling the string back (step 1), or laying a missile (step 2).</summary>
    public enum Act
    {
        None = 0,
        Pulling = 1,
        Loading = 2,
    }

    /// <summary>Where a hand goes while a missile is laid: its handle, the hip (taking a missile), over the groove, in it.</summary>
    public enum HandSpot
    {
        Grip,
        Hip,
        Above,
        Seat,
    }

    /// <summary>
    /// The reload's moments and the shot's spring, as pure functions of the ballista's <see cref="WeaponState"/> (its reload's
    /// steps, <see cref="BallistaDesign"/>): the library runs the reload by their lengths and every peer draws by them
    /// (<see cref="BallistaLook"/>, <see cref="BallistaHands"/>, the holder's spot in <see cref="BallistaControl"/>), so everyone sees the same reload at the same moment.
    /// Pulling (1.4 s): lean in and take the string with both hands, haul it back to the latch with the body as well as
    /// the arms (leaning back) while stepping back, the latch clicks, hands back to the handles. Loading (1.2 s): step in, take a missile from the hip, carry it over the
    /// groove, lay it down into it, hand back to the handle.
    /// </summary>
    public static class BallistaTimeline
    {
        public const float Reach = 0.4f, DrawEnd = 1.2f, PullEnd = 1.4f;
        public const float Fetch = 0.35f, Carry = 0.75f, Lay = 0.95f, LoadEnd = 1.2f;
        public const float InHand = 0.25f;     // the missile shows in the hand, taken from the hip
        public const float Settle = 0.5f;      // after a shot, before the next pull starts
        private const float Snap = 0.05f, Overshoot = 0.18f, Damping = 0.12f, Hertz = 5f, Ring = 0.8f;
        private const float PullLean = 28f, HaulLean = -14f, LoadLean = 22f;

        /// <summary>How far the string is drawn: 0 at rest, 1 at the latch, below 0 while the arms ring forward after a shot.</summary>
        public static float Draw(in WeaponState s)
        {
            if (Doing(s) == Act.Pulling && s.StepTime < PullEnd)
            {
                return Smooth(Mathf.InverseLerp(Reach, DrawEnd, s.StepTime));
            }
            return (Spring)s.Settled != Spring.Slack || Doing(s) == Act.Loading ? 1f : Recoil(s.ShotAge);
        }

        /// <summary>The string after a shot: from the latch to rest in a blink, then the arms ring forward and settle.</summary>
        public static float Recoil(float age)
        {
            if (age < Snap)
            {
                return 1f - age / Snap;
            }
            float t = age - Snap;
            return t > Ring ? 0f : -Overshoot * Mathf.Exp(-t / Damping) * Mathf.Sin(t * 2f * Mathf.PI * Hertz);
        }

        /// <summary>How close to the ballista the holder stands: 0 at `stand` (aiming), 1 at `stand_load` (reaching in).</summary>
        public static float Close(in WeaponState s)
        {
            float t = s.StepTime;
            return Doing(s) switch
            {
                Act.Pulling when t < PullEnd => Smooth(t / (Reach * 0.8f)) * (1f - Smooth(Mathf.InverseLerp(Reach, DrawEnd, t))),
                Act.Loading when t < LoadEnd => Smooth(t / Fetch) * (1f - Smooth(Mathf.InverseLerp(Lay, LoadEnd, t))),
                _ => 0f,
            };
        }

        /// <summary>
        /// The holder's lean in degrees, forward positive: bent in to take the string, then hauling it with the whole body
        /// (from bent forward to leaning back, like a rower) as the arms draw, upright again once the latch has it.
        /// </summary>
        public static float Lean(in WeaponState s)
        {
            float t = s.StepTime;
            return Doing(s) switch
            {
                Act.Pulling when t < Reach => PullLean * Smooth(t / Reach),
                Act.Pulling when t < DrawEnd => Mathf.Lerp(PullLean, HaulLean, Smooth(Mathf.InverseLerp(Reach, DrawEnd, t))),
                Act.Pulling when t < PullEnd => HaulLean * (1f - Smooth(Mathf.InverseLerp(DrawEnd, PullEnd, t))),
                Act.Loading when t < LoadEnd => LoadLean * Smooth(Mathf.InverseLerp(0f, Carry, t)) * (1f - Smooth(Mathf.InverseLerp(Lay, LoadEnd, t))),
                _ => 0f,
            };
        }

        /// <summary>How fully both hands hold the string (pulling), 0 when they are on the handles.</summary>
        public static float OnString(in WeaponState s)
        {
            float t = s.StepTime;
            if (Doing(s) != Act.Pulling || t >= PullEnd)
            {
                return 0f;
            }
            return t < Reach ? Smooth(t / Reach) : t < DrawEnd ? 1f : 1f - Smooth(Mathf.InverseLerp(DrawEnd, PullEnd, t));
        }

        /// <summary>The right hand while a missile is laid: between two spots, and how far along (Grip to Grip otherwise).</summary>
        public static (HandSpot from, HandSpot to, float along) RightHand(in WeaponState s)
        {
            float t = s.StepTime;
            if (Doing(s) != Act.Loading || t >= LoadEnd)
            {
                return (HandSpot.Grip, HandSpot.Grip, 0f);
            }
            return t < Fetch ? (HandSpot.Grip, HandSpot.Hip, Smooth(t / Fetch))
                : t < Carry ? (HandSpot.Hip, HandSpot.Above, Smooth(Mathf.InverseLerp(Fetch, Carry, t)))
                : t < Lay ? (HandSpot.Above, HandSpot.Seat, Smooth(Mathf.InverseLerp(Carry, Lay, t)))
                : (HandSpot.Seat, HandSpot.Grip, Smooth(Mathf.InverseLerp(Lay, LoadEnd, t)));
        }

        /// <summary>Whether the holder has a missile in the hand.</summary>
        public static bool MissileInHand(in WeaponState s) => Doing(s) == Act.Loading && s.StepTime >= InHand && s.StepTime < Lay;

        /// <summary>Whether a missile lies in the groove.</summary>
        public static bool MissileLaid(in WeaponState s) => (Spring)s.Settled == Spring.Loaded || (Doing(s) == Act.Loading && s.StepTime >= Lay);

        /// <summary>The step under way.</summary>
        public static Act Doing(in WeaponState s) => (Act)s.Step;

        public static float Smooth(float t) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
    }
}
