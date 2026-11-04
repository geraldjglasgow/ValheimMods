using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// One of the crane operator's fists on its grips (<see cref="KwCraneHands"/>): on the grip it is given, moving with it
    /// (a lever leaning, the crank's handle going round); given another, it moves over in a fifth of a second, lifting a
    /// little on the way. Worked out in the turret's frame, so a hand moving over goes round with the turret.
    /// </summary>
    public sealed class KwCraneHand
    {
        private const float Over = 0.2f, Lift = 0.06f;
        private Transform? on;
        private Vector3 from, last;
        private float moved = 1f;

        /// <summary>The fist's target in the world this frame, on its way to (or on) the grip.</summary>
        public Vector3 Fist(Transform grip, Transform turret, float dt)
        {
            Vector3 at = turret.InverseTransformPoint(grip.position);
            if (grip != on)
                (from, moved, on) = (on == null ? at : last, on == null ? 1f : 0f, grip);
            moved = Mathf.MoveTowards(moved, 1f, dt / Over);
            float t = Mathf.SmoothStep(0f, 1f, moved);
            last = Vector3.Lerp(from, at, t) + Vector3.up * (Lift * Mathf.Sin(Mathf.PI * t));
            return turret.TransformPoint(last);
        }

        /// <summary>A new operator: the fist takes its grip at once.</summary>
        public void Reset() => on = null;
    }

    /// <summary>
    /// The crane's works and its operator's hands, on a screen near enough to see them (a dedicated server has none),
    /// after the crane has been posed and the animator has posed him. The works turn from the crane's pose and its crate
    /// (<see cref="KwCraneWorks"/>). The operator is the one the crane's ZDO names (<c>kwc_operator</c>; on his own game,
    /// himself while he works it), looked up four times a second; standing where the crane puts him (its
    /// <c>operator</c> point, facing along the boom), his hands go onto the grips by two-bone IK and his upper body leans
    /// over the console (<see cref="KwHelmPose"/>, the helmsman's, the LimbIk library): the left on the slew lever and the
    /// right on the boom lever; the left over to the crank's handle while the head goes up or down, following it round;
    /// the right over to the hook latch while it flicks, then back. All from what every peer draws: nothing is sent.
    /// </summary>
    public sealed class KwCraneHands : MonoBehaviour
    {
        // Within how far of the camera it is drawn (m); how often the operator is looked up; how fast the hands take hold
        // and let go (per second); within how far of the operator's point he counts as there, and beyond how far not at all.
        private const float Near = 50f, LookEvery = 0.25f, Ease = 5f, OnSpot = 0.3f, OffSpot = 0.8f;

        // His lean over the console (degrees): at the levers, at the latch, bowed over the crank (its handle is low); how
        // fast it follows (per second).
        private const float RestLean = 8f, HookLean = 15f, CrankLean = 45f, LeanEase = 6f;

        private readonly KwCraneHand left = new KwCraneHand(), right = new KwCraneHand();
        private ZNetView? view;
        private KwCrane? crane;
        private KwCraneRig? rig;
        private KwCraneWorks? works;
        private KwHelmPose? pose;
        private Player? tried;
        private float weight, lookAt, lean;
        private bool holding, broken;

        private void Start()
        {
            (view, crane, rig) = (GetComponent<ZNetView>(), GetComponent<KwCrane>(), GetComponent<KwCraneRig>());
            works = rig != null ? KwCraneWorks.Of(transform) : null;
            if (view == null || view.GetZDO() == null || crane == null || works == null)
                enabled = false;
        }

        // A throw is reported once and stops this crane's hands, rather than once a frame.
        private void LateUpdate()
        {
            if (broken)
                return;
            broken = true;
            Guard.Run("Broadside crane hands", static h => h.Tick(), this);
            broken = false;
        }

        private void Tick()
        {
            if (!view!.IsValid() || rig!.Posed == null)
                return;
            if (!Seen())
            {
                works!.Pause();
                return;
            }
            float dt = Time.deltaTime;
            works!.Pose(rig.Posed, crane!.Crate, dt);
            Look();
            Hands(dt);
        }

        private bool Seen()
        {
            Camera? eye = Utils.GetMainCamera();
            return eye != null && (eye.transform.position - transform.position).sqrMagnitude < Near * Near;
        }

        // Who works it, four times a second: a new operator's hands take hold afresh; one gone lets go as quickly.
        private void Look()
        {
            if (Time.time < lookAt)
                return;
            lookAt = Time.time + LookEvery;
            Player? who = crane!.Operating ? Player.m_localPlayer : Named(crane.Operator);
            holding = who != null;
            if (who == null || who == tried)
                return;
            (tried, pose, weight, lean) = (who, KwHelmPose.Of(who), 0f, RestLean);
            left.Reset();
            right.Reset();
        }

        // The player a ZDO names, if loaded here; never the local player, whose own game knows whether he works it.
        private static Player? Named(ZDOID id)
        {
            if (id.IsNone() || ZNetScene.instance == null)
                return null;
            GameObject? found = ZNetScene.instance.FindInstance(id);
            Player? player = found != null ? found.GetComponent<Player>() : null;
            return player != Player.m_localPlayer ? player : null;
        }

        // His hands toward the grips by how near he stands to his point, or letting go once he no longer works it.
        private void Hands(float dt)
        {
            if (pose == null)
                return;
            Player player = pose.Player;
            if (player == null)
            {
                (pose, tried) = (null, null);
                return;
            }
            weight = Mathf.MoveTowards(weight, holding ? AtPoint(player) : 0f, Ease * dt);
            if (weight > 0.001f)
                pose.Pose(Hold(dt), weight);
            else if (!holding)
                (pose, tried) = (null, null);
        }

        // 1 while he stands on the crane's operator point, 0 once he is well off it.
        private float AtPoint(Player player)
        {
            Vector3 off = player.transform.position - rig!.Operator!.position;
            off.y = 0f;
            return 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(OnSpot, OffSpot, off.magnitude));
        }

        // This frame's fists and lean: the left on the crank while hoisting, else the slew lever; the right on the latch
        // while it is worked, else the boom lever.
        private KwHelmHold Hold(float dt)
        {
            Transform turret = works!.Turret;
            bool crank = works.Hoisting, hook = works.Latching;
            Vector3 l = left.Fist(crank ? works.GripHoist : works.GripSlew, turret, dt);
            Vector3 r = right.Fist(hook ? works.GripHook : works.GripBoom, turret, dt);
            float want = crank ? CrankLean : hook ? HookLean : RestLean;
            lean = Mathf.Lerp(lean, want, 1f - Mathf.Exp(-LeanEase * dt));
            return new KwHelmHold(l, r, lean, 0f);
        }
    }
}
