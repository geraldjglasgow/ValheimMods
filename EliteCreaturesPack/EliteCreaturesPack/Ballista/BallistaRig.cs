using BundlePrefabs;
using EliteCreaturesPack.Core;
using UnityEngine;

namespace EliteCreaturesPack.Ballista
{
    /// <summary>
    /// One arm of a player's rig, posed after the animator: its fist's grip point (the game's hand attach) put on a
    /// target by two-bone IK (<see cref="TwoBoneIk"/>), the elbow out to its side and down, the wrist as the clip has it.
    /// </summary>
    public sealed class BallistaArm
    {
        private readonly Transform upper, lower, hand, grip;
        private readonly float side;

        public BallistaArm(Transform upper, Transform lower, Transform hand, Transform grip, float side) =>
            (this.upper, this.lower, this.hand, this.grip, this.side) = (upper, lower, hand, grip, side);

        /// <summary>Where the fist grips now.</summary>
        public Vector3 Grip => grip.position;

        /// <summary>The fist toward `target` by `weight` (1: on it, as far as the arm reaches).</summary>
        public void Reach(Vector3 target, float weight, Transform body)
        {
            Vector3 goal = Vector3.Lerp(grip.position, target, weight);
            Vector3 hint = upper.position + body.right * (side * 0.35f) + Vector3.down * 0.35f - body.forward * 0.15f;
            for (int pass = 0; pass < 2; pass++)
            {
                TwoBoneIk.Solve(upper, lower, hand, goal - (grip.position - hand.position), hint);
            }
        }
    }

    /// <summary>
    /// The bones of a player the ballista poses: the spine (leaned forward in two halves), the hips (where a missile is
    /// taken from), and both arms. A lean is applied only to a pose the animator wrote this frame, never twice to the
    /// same pose (an animator updating with physics skips frames).
    /// </summary>
    public sealed class BallistaRig
    {
        public readonly BallistaArm Left, Right;
        public readonly Transform Hips;
        private readonly Transform spine, chest;
        private Quaternion leftAs;

        private BallistaRig(Transform hips, Transform spine, Transform chest, BallistaArm left, BallistaArm right) =>
            (Hips, this.spine, this.chest, Left, Right) = (hips, spine, chest, left, right);

        /// <summary>The rig of a player, or null when the player's model lacks one of its bones.</summary>
        public static BallistaRig? Of(Player player)
        {
            Transform visual = player.m_visual != null ? player.m_visual.transform : player.transform;
            Transform?[] b = { Find(visual, "Hips"), Find(visual, "Spine"), Find(visual, "Spine1"),
                Find(visual, "LeftArm"), Find(visual, "LeftForeArm"), Find(visual, "LeftHand"), Find(visual, "LeftHand_Attach"),
                Find(visual, "RightArm"), Find(visual, "RightForeArm"), Find(visual, "RightHand"), Find(visual, "RightHand_Attach") };
            foreach (Transform? bone in b)
            {
                if (bone == null)
                {
                    return null;
                }
            }
            var left = new BallistaArm(b[3]!, b[4]!, b[5]!, b[6]!, -1f);
            var right = new BallistaArm(b[7]!, b[8]!, b[9]!, b[10]!, 1f);
            return new BallistaRig(b[0]!, b[1]!, b[2]!, left, right);
        }

        /// <summary>The upper body leaned forward by `degrees` about the body's right, half at the waist, half at the chest.</summary>
        public void Lean(float degrees, Transform body)
        {
            if (Mathf.Abs(degrees) < 0.01f)
            {
                leftAs = default;
                return;
            }
            if (Quaternion.Angle(spine.localRotation, leftAs) < 0.001f)
            {
                return;   // the animator did not pose this frame: the lean is still on
            }
            Quaternion half = Quaternion.AngleAxis(degrees * 0.5f, body.right);
            spine.rotation = half * spine.rotation;
            chest.rotation = half * chest.rotation;
            leftAs = spine.localRotation;
        }

        private static Transform? Find(Transform root, string name) => GameMaterials.Find(root, name);
    }
}
