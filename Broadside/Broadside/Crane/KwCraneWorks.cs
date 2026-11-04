using BundlePrefabs;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The crane's works (the workshop model's controls and gears, 2026-11-03), turned on a screen from the pose the crane
    /// is drawn in (<see cref="KwCraneRig.Posed"/>: the ZDO's <c>kwc_turn</c>, <c>kwc_slide</c>, <c>kwc_drop</c> as every
    /// peer eases toward them) and the crate on its hook (<c>kwc_crate</c>), so every screen shows them alike and nothing
    /// is sent. The gears follow what they drive: the drum's pinion the drum, the crank and its wheel a third of the drum
    /// the other way (the wheel's 21 teeth driving the pinion's 7, a step-up since 2026-11-03 so the operator cranks slowly
    /// while the pinion whirls), the slew pinion five times the turret's turn (40:8, measured on the turret), the boom's
    /// pinion rolling along the rack as the boom slides. The levers lean with what the crane is doing: the slew lever
    /// forward while the turret turns right (the game's Right key) and back while it turns left, the boom lever forward while
    /// the boom slides out (Forward) and back while it comes in, each up to 20° by the speed, eased, upright when it stops;
    /// the hook latch flicks over and back as a crate is hooked (forward) or let go (back). The contract's names, pivots,
    /// axes and signs are the workshop's (<c>CargoCrane/HANDOFF.md</c>): every part turns about its own local axis from rest,
    /// "push forward" (the grip toward the boom) being +X.
    /// </summary>
    public sealed class KwCraneWorks
    {
        private const string TurnName = "turn";
        private const string SlewLever = "ctl_slew", BoomLever = "ctl_boom", HookLatch = "ctl_hook", CrankArm = "ctl_hoist";
        private const string DrumGear = "gear_drum", CrankGear = "gear_crank", SlewGear = "gear_slew", BoomGear = "gear_boom";
        private static readonly string[] Parts = { SlewLever, BoomLever, HookLatch, CrankArm, DrumGear, CrankGear, SlewGear, BoomGear };
        private const int Slew = 0, Boom = 1, Hook = 2, Crank = 3, DrumPinion = 4, CrankWheel = 5, SlewPinion = 6, BoomPinion = 7;

        // Each grip and the control it rides (the bundle puts every point under its root).
        private static readonly string[] GripNames = { "grip_slew", "grip_boom", "grip_hoist", "grip_hook" };
        private static readonly string[] GripOn = { SlewLever, BoomLever, CrankArm, HookLatch };

        // The gear trains (HANDOFF): the crank to the drum (7:21 the other way; -3 until 2026-11-03), the slew pinion to the
        // turret, the boom's pinion per metre slid (-360 / (2 pi 0.20)).
        private const float CrankRatio = -7f / 21f, SlewRatio = 5f, BoomPerMetre = -286.4789f;

        // A lever's lean at full speed and how quickly it follows (per second); a step bigger than these in one frame is a
        // jump (a pose taken at once, a crate's sling drawn tight), not a movement.
        private const float LeverMost = 20f, LeverEase = 10f, JumpTurn = 10f, JumpLength = 0.5f;

        // The head counts as hoisting above this speed (m/s), and for a while after it stops (s).
        private const float HoistAbove = 0.05f, HoistLinger = 0.35f;

        // The latch's flick after the crate changes (s): the hand reaches it, it goes over, it is held, it is back.
        private const float LatchMost = 25f, FlickStart = 0.2f, FlickOver = 0.35f, FlickHeld = 0.5f, FlickEnd = 0.65f;

        private readonly Transform turret;
        private readonly Transform[] part = new Transform[Parts.Length];
        private readonly Quaternion[] rest = new Quaternion[Parts.Length];
        private float lastTurn, lastSlide, lastDrop, slewLean, boomLean, hoistUntil = -99f, flickAt = -99f, flickSign;
        private ZDOID lastCrate;
        private bool fresh = true, crateKnown;

        private KwCraneWorks(Transform turret, Transform[] found, Transform[] grips)
        {
            this.turret = turret;
            for (int i = 0; i < found.Length; i++)
                (part[i], rest[i]) = (found[i], found[i].localRotation);
            (GripSlew, GripBoom, GripHoist, GripHook) = (grips[0], grips[1], grips[2], grips[3]);
        }

        /// <summary>The turret every part rides, and where the operator's fists go (each moving with its control).</summary>
        public Transform Turret => turret;
        public Transform GripSlew { get; }
        public Transform GripBoom { get; }
        public Transform GripHoist { get; }
        public Transform GripHook { get; }

        /// <summary>Whether the head is going up or down (the crank turning under the operator's left hand).</summary>
        public bool Hoisting => Time.time < hoistUntil;

        /// <summary>Whether the latch is being worked (the operator's right hand on it), from the crate changing until it is back.</summary>
        public bool Latching => Time.time - flickAt < FlickEnd;

        /// <summary>On the prefab, once: every control and gear under the turret, each grip under its control (all keeping their world pose).</summary>
        public static void Arrange(Transform model, Transform turret)
        {
            foreach (string name in Parts)
                GameMaterials.Find(model, name)?.SetParent(turret, true);
            for (int i = 0; i < GripNames.Length; i++)
            {
                Transform? control = GameMaterials.Find(model, GripOn[i]);
                if (control != null)
                    GameMaterials.Find(model, GripNames[i])?.SetParent(control, true);
            }
        }

        /// <summary>A crane's works, or null when its model has none of them (an older bundle) or lacks one.</summary>
        public static KwCraneWorks? Of(Transform root)
        {
            Transform? turret = GameMaterials.Find(root, TurnName);
            Transform[] found = new Transform[Parts.Length], grips = new Transform[GripNames.Length];
            for (int i = 0; i < Parts.Length; i++)
            {
                Transform? one = GameMaterials.Find(root, Parts[i]);
                if (one == null)
                    return null;
                found[i] = one;
            }
            for (int i = 0; i < GripNames.Length; i++)
            {
                Transform? one = GameMaterials.Find(root, GripNames[i]);
                if (one == null)
                    return null;
                grips[i] = one;
            }
            return turret != null ? new KwCraneWorks(turret, found, grips) : null;
        }

        /// <summary>Every part turned for this frame's pose, the levers' lean by how it changed, the latch by the crate on the hook.</summary>
        public void Pose(KwCranePose pose, ZDOID crate, float dt)
        {
            Gears(pose);
            Levers(pose, dt);
            Latch(crate);
        }

        /// <summary>Not drawn for a while (too far to see): the next frame starts afresh, nothing taken for a movement.</summary>
        public void Pause() => fresh = true;

        // The gear trains, as drawn now: angles from the pose, wrapped to a turn so large ones keep their precision.
        private void Gears(KwCranePose pose)
        {
            float drum = KwCraneRig.DrumAngle(pose);
            Turn(DrumPinion, drum, Vector3.right);
            Turn(CrankWheel, CrankRatio * drum, Vector3.right);
            Turn(Crank, CrankRatio * drum, Vector3.right);              // the crank, on the wheel's shaft
            Turn(SlewPinion, SlewRatio * pose.Turn, Vector3.up);
            Turn(BoomPinion, BoomPerMetre * pose.Slide, Vector3.right);
        }

        private void Turn(int i, float degrees, Vector3 axis) =>
            part[i].localRotation = rest[i] * Quaternion.AngleAxis(Mathf.Repeat(degrees, 360f), axis);

        // The slew and boom levers toward their lean for the speed now, the head's speed kept for the hoist; a jump counts as still.
        private void Levers(KwCranePose pose, float dt)
        {
            float turned = Mathf.DeltaAngle(lastTurn, pose.Turn), slid = pose.Slide - lastSlide, dropped = pose.Drop - lastDrop;
            bool still = fresh || dt <= 0f || Mathf.Abs(turned) > JumpTurn || Mathf.Abs(slid) > JumpLength || Mathf.Abs(dropped) > JumpLength;
            (lastTurn, lastSlide, lastDrop, fresh) = (pose.Turn, pose.Slide, pose.Drop, false);
            float turnRate = still ? 0f : turned / dt, slideRate = still ? 0f : slid / dt;
            if (!still && Mathf.Abs(dropped / dt) > HoistAbove)
                hoistUntil = Time.time + HoistLinger;
            float t = 1f - Mathf.Exp(-LeverEase * Mathf.Max(0f, dt));
            slewLean = Mathf.Lerp(slewLean, LeverMost * Mathf.Clamp(turnRate / KwCargo.TurnSpeed, -1f, 1f), t);
            boomLean = Mathf.Lerp(boomLean, LeverMost * Mathf.Clamp(slideRate / KwCargo.SlideSpeed, -1f, 1f), t);
            Turn(Slew, slewLean, Vector3.right);
            Turn(Boom, boomLean, Vector3.right);
        }

        // A crate hooked (forward) or let go (back) since the last frame flicks the latch; the first look only notes it.
        private void Latch(ZDOID crate)
        {
            if (!crateKnown || crate != lastCrate)
            {
                if (crateKnown)
                    (flickAt, flickSign) = (Time.time, crate.IsNone() ? -1f : 1f);
                (lastCrate, crateKnown) = (crate, true);
            }
            Turn(Hook, LatchAngle(Time.time - flickAt), Vector3.right);
        }

        // Upright until the hand is on the knob, over and held, back upright.
        private float LatchAngle(float t)
        {
            if (t < FlickStart || t > FlickEnd)
                return 0f;
            float over = t < FlickHeld ? Mathf.InverseLerp(FlickStart, FlickOver, t) : 1f - Mathf.InverseLerp(FlickHeld, FlickEnd, t);
            return flickSign * LatchMost * Mathf.SmoothStep(0f, 1f, over);
        }
    }
}
