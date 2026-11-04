using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A hammock planned between two ends (<see cref="KwHammockClick"/>), on the placing player's game: the rope points and
    /// the bars' height (<see cref="KwHammockRules"/>), the floor under the middle of the rope line (its origin, turned with
    /// its x along the span, level, and up the ship's up aboard), its shape in that frame (<see cref="Spec"/>) and the game's
    /// checks (<see cref="KwHammockChecks"/>). Refused with the first reason (<see cref="Why"/>) and still laid out when it
    /// can be, so the preview shows it red. Worked out in the world as the physics has it now (aboard, by the ship's body,
    /// where its colliders are) and kept in the ship's frame, so the preview and the piece placed stand where the ship is
    /// drawn (<see cref="Drawn"/>) however it has moved since.
    /// </summary>
    public sealed class KwHammockPlan
    {
        private Vector3 localAt;
        private Quaternion localTurn = Quaternion.identity;

        private KwHammockPlan(KwShipRiders? ship) => Ship = ship;

        public KwShipRiders? Ship { get; }

        /// <summary>The shape, once laid out; null when the ends could not make one.</summary>
        public KwHammockSpec? Spec { get; private set; }

        /// <summary>Its origin and turn in the world as the checks saw it.</summary>
        public Vector3 At { get; private set; }
        public Quaternion Turn { get; private set; } = Quaternion.identity;

        /// <summary>The level distance between the points clicked.</summary>
        public float Apart { get; private set; }

        /// <summary>Why it may not be hung; null when it may.</summary>
        public string? Why { get; private set; }

        /// <summary>"3.1 m, a post and an iron hook".</summary>
        public string Says => Spec != null ? $"{Apart:0.0} m, {Spec.Says}" : "";

        /// <summary>Where it stands now where the ship is drawn (the world's off a ship): the preview's place and the piece's.</summary>
        public (Vector3 At, Quaternion Turn) Drawn
        {
            get
            {
                if (Ship == null)
                    return (At, Turn);
                (Vector3 bodyAt, Quaternion bodyTurn) = Ship.Pose;
                (Vector3 at, Quaternion turn) = KwShipRiderPose.World(bodyAt, bodyTurn, localAt, localTurn);
                return KwHammockAim.Drawn(Ship, at, turn);
            }
        }

        /// <summary>A point of its frame (kit) in the world as the checks see it.</summary>
        public Vector3 World(Vector3 kit) => At + Turn * KwMesh.ToUnity(kit);

        /// <summary>Refused, unless it already was.</summary>
        public void Refuse(string why) => Why ??= why;

        /// <summary>The hammock two ends make, checked (flashing a ward in the way when asked: the click's, not the preview's).</summary>
        public static KwHammockPlan Make(KwHammockClick a, KwHammockClick b, bool flash)
        {
            KwHammockPlan plan = new KwHammockPlan(a.Ship);
            if (a.Ship != b.Ship || a.Aboard != b.Aboard)
                plan.Refuse(KwHammockWords.TwoShips);
            else if (a.Ship != null && a.Ship.Id == 0L)
                plan.Refuse(KwHammockWords.ShipNotReady);
            else
                plan.Lay(a, b);
            if (plan.Spec != null && plan.Why == null)
                KwHammockChecks.Run(plan, flash);
            return plan;
        }

        // Its rope points, frame, bars and shape; refused past a limit but still laid out, but for ends on one spot.
        private void Lay(KwHammockClick a, KwHammockClick b)
        {
            Vector3 up = a.Up, at = a.At, bt = b.At;
            Apart = KwHammockRules.Flat(bt - at, up).magnitude;
            if (Apart < KwHammockRules.MinApart)
                Refuse(KwHammockWords.TooClose(Apart));
            else if (Apart > KwHammockRules.MaxApart)
                Refuse(KwHammockWords.TooFar(Apart));
            if (Apart < 0.5f)
                return;
            (Vector3 ra, Vector3 rb) = KwHammockRules.Ropes((at, a.Face, a.Post), (bt, b.Face, b.Post), up);
            float bars = Bars(ra, rb, up, out Vector3 floor);
            Turn = Quaternion.LookRotation(KwHammockRules.Flat(rb - ra, up).normalized, up);
            At = floor;
            Spec = new KwHammockSpec(Mount(a.Post, at, a.Face, ra), Mount(b.Post, bt, b.Face, rb), bars - KwHammockRules.Height(floor, up));
            (localAt, localTurn) = Ship != null ? KwShipRiderPose.Local(Ship.Pose.At, Ship.Pose.Turn, At, Turn) : (At, Turn);
        }

        // The bars' height (along up): the standard height over the floor under the middle, as the ropes allow; the floor's
        // point (made up under the bars when there is none, refused).
        private float Bars(Vector3 ra, Vector3 rb, Vector3 up, out Vector3 floor)
        {
            (float low, float high) = KwHammockRules.Bars(ra, rb, up);
            if (low > high)
            {
                float rise = Mathf.Abs(KwHammockRules.Height(ra - rb, up));
                Refuse(KwHammockWords.Uneven(rise, rise - (low - high)));
                low = high;
            }
            Vector3 middle = (ra + rb) * 0.5f;
            if (KwHammockChecks.Floor(middle, up, out floor))
                return Mathf.Clamp(KwHammockRules.Height(floor, up) + KwHammockRules.Standard, low, high);
            Refuse(KwHammockWords.NoFloor);
            floor = middle - up * (KwHammockRules.Height(middle, up) - high + KwHammockRules.Standard);
            return high;
        }

        // An end in the hammock's frame: its rope point, its point, the way out of its surface (up for a post).
        private KwHammockMount Mount(bool post, Vector3 at, Vector3 face, Vector3 rope)
        {
            Quaternion back = Quaternion.Inverse(Turn);
            Vector3 kitFace = post ? new Vector3(0f, 0f, 1f) : KwHammockShape.Kit(back * face).normalized;
            return new KwHammockMount(post, KwHammockShape.Kit(back * (rope - At)), KwHammockShape.Kit(back * (at - At)), kitFace);
        }
    }
}
