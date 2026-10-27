using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A helmsman's hold on a captain's wheel (<see cref="KwWheel"/>): a fist on each of the spoke handles nearest ten and
    /// two o'clock, just out past the rim, turning with the wheel. A handle that turns out of its hand's reach is let go
    /// and the hand moves over to another, lifting a little off the wheel on the way: while the wheel turns, to the handle
    /// coming toward its place (so it rides that one right across its reach), when still, to the one nearest it. Hand over
    /// hand, the two hands each in their own time. The wheel is drawn from the helm's replicated value on every peer
    /// (<see cref="KwWheelHelm"/> on the dock, <see cref="KwShipWheel"/> on a launched ship), so every peer sees the hands go
    /// round alike. On a launched ship he stands behind the wheel on his own feet, planted once there
    /// (<see cref="KwHelmFeet"/>), facing the way the wheel faces.
    /// </summary>
    public sealed class KwWheelGrip : IKwHelmGrip
    {
        // The handles round the wheel; where on them the fist holds (from the axle) and its face toward the helmsman; how long
        // a hand takes over to the next handle and how far off the wheel it lifts; his lean over the wheel.
        private const int Handles = 8;
        private const float Radius = 0.6f, Face = -0.04f, Regrip = 0.14f, Lift = 0.08f, Lean = 12f;

        // How far either side of its place a hand reaches round the wheel, and how far back against the turn it takes a new
        // handle (degrees); below what speed the wheel counts as still (degrees a second).
        private const float Reach = 33f, Lead = 26f, Turning = 20f;

        // Each hand's place on the wheel, degrees from the top toward his right (the left hand 0, the right 1).
        private static readonly float[] Home = { -42f, 42f };

        private readonly Transform wheel;
        private readonly Transform? stand;
        private readonly int[] held = { -1, -1 };
        private readonly float[] moved = { 1f, 1f };
        private readonly Vector3[] from = new Vector3[2], last = new Vector3[2];
        private float spun, drift;
        private bool fresh = true;

        /// <summary>A hold on a wheel turning about its own forward (the axle), its stand its parent; where he stands behind it.</summary>
        public KwWheelGrip(Transform wheel, Transform? stand = null) => (this.wheel, this.stand) = (wheel, stand);

        public Transform Parts => wheel.parent != null ? wheel.parent : wheel;

        /// <summary>Behind the wheel, where the stand's attach point is.</summary>
        public Vector3 Stand => stand != null ? stand.position : Parts.position;

        /// <summary>The way the wheel faces.</summary>
        public Vector3 Bow => stand != null ? stand.forward : Parts.forward;

        /// <summary>The wheel stays where it is: he never needs to step away from it.</summary>
        public bool Crowded(Player player) => false;

        public KwHelmHold Hold(Player player, float dt)
        {
            Spin(dt);
            return new KwHelmHold(Fist(0, dt), Fist(1, dt), Lean, 0f);
        }

        public void Reset() => (held[0], held[1], fresh) = (-1, -1, true);

        // Which way the handles run round now: 1 toward his right over the top, -1 toward his left, 0 still.
        private void Spin(float dt)
        {
            float now = Angle(0), speed = fresh || dt <= 0f ? 0f : Mathf.DeltaAngle(spun, now) / dt;
            (spun, fresh) = (now, false);
            drift = Mathf.Abs(speed) > Turning ? Mathf.Sign(speed) : 0f;
        }

        // A fist on its handle, in the world, moving over to another once its own turns out of the hand's reach.
        private Vector3 Fist(int hand, float dt)
        {
            if (held[hand] < 0 || Mathf.Abs(Mathf.DeltaAngle(Angle(held[hand]), Home[hand])) > Reach)
                Take(hand);
            moved[hand] = Mathf.MoveTowards(moved[hand], 1f, dt / Regrip);
            float t = Mathf.SmoothStep(0f, 1f, moved[hand]);
            Vector3 at = Vector3.Lerp(from[hand], On(held[hand]), t) + Vector3.back * (Lift * Mathf.Sin(Mathf.PI * t));
            last[hand] = at;
            return Parts.TransformPoint(at);
        }

        // A new handle within the hand's reach that the other hand does not hold, the one nearest a little back against the
        // turn from its place; the fist sets off for it from where it is (a hand just taking hold is on it at once).
        private void Take(int hand)
        {
            float aim = Home[hand] - drift * Lead, bestOff = float.MaxValue;
            int best = held[hand] < 0 ? 0 : held[hand];
            for (int i = 0; i < Handles; i++)
            {
                float angle = Angle(i), off = Mathf.Abs(Mathf.DeltaAngle(angle, aim));
                if (i != held[1 - hand] && Mathf.Abs(Mathf.DeltaAngle(angle, Home[hand])) < Reach && off < bestOff)
                    (best, bestOff) = (i, off);
            }
            bool first = held[hand] < 0;
            (from[hand], moved[hand], held[hand]) = (first ? On(best) : last[hand], first ? 1f : 0f, best);
        }

        // A handle's grip in the stand's frame, as the wheel is turned now.
        private Vector3 On(int i) => wheel.localPosition + wheel.localRotation * Handle(i);

        // How far round from the top toward his right a handle is as the wheel is turned now, in degrees.
        private float Angle(int i)
        {
            Vector3 p = wheel.localRotation * Handle(i);
            return Mathf.Atan2(p.x, p.y) * Mathf.Rad2Deg;
        }

        // A handle's grip in the wheel's own frame: the spokes run out past the rim every eighth of a turn from the top
        // (KwWheelShapes' wheel, kit x along the axle toward the bow).
        private static Vector3 Handle(int i)
        {
            float a = i * Mathf.PI / 4f + Mathf.PI / 2f;
            return KwMesh.ToUnity(new Vector3(Face, Radius * Mathf.Cos(a), Radius * Mathf.Sin(a)));
        }
    }
}
