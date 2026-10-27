using BundlePrefabs;
using LimbIk;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// One arm of a helmsman, posed after the animator: the fist's grip point (the game's hand attach) put on a target by
    /// two-bone IK (the LimbIk library's <see cref="TwoBoneIk"/>), the elbow out to its side and down.
    /// </summary>
    public sealed class KwHelmArm
    {
        private readonly Transform upper, lower, hand, grip;
        private readonly float side;

        public KwHelmArm(Transform upper, Transform lower, Transform hand, Transform grip, float side) =>
            (this.upper, this.lower, this.hand, this.grip, this.side) = (upper, lower, hand, grip, side);

        /// <summary>Where the fist grips now.</summary>
        public Vector3 Grip => grip.position;

        /// <summary>The fist toward a target by a weight (1: on it, as far as the arm reaches).</summary>
        public void Reach(Vector3 target, float weight, Transform body)
        {
            Vector3 goal = Vector3.Lerp(grip.position, target, weight);
            Vector3 hint = Elbow(goal, body);
            for (int pass = 0; pass < 2; pass++)
                TwoBoneIk.Solve(upper, lower, hand, goal - (grip.position - hand.position), hint);
        }

        // Where the elbow points: out to his side, back and a little down, square to the line from the shoulder to the
        // fist. A fixed point beside the shoulder lay near that line whenever the fist came back to the hip, so the elbow
        // flipped from side to side and the arm seemed to stretch and snap.
        private Vector3 Elbow(Vector3 goal, Transform body)
        {
            Vector3 shoulder = upper.position, line = goal - shoulder;
            Vector3 wanted = body.right * (side * 0.7f) - body.forward * 0.5f + Vector3.down * 0.3f;
            Vector3 square = Vector3.ProjectOnPlane(wanted, line.normalized);
            if (square.sqrMagnitude < 0.01f)
                square = body.right * side;
            return shoulder + line * 0.5f + square.normalized * 0.3f;
        }
    }

    /// <summary>A player's two arms as the helmsman's pose takes them (<see cref="KwHelmPose"/>).</summary>
    public sealed class KwHelmArms
    {
        private static readonly string[] Bones =
            { "LeftArm", "LeftForeArm", "LeftHand", "LeftHand_Attach", "RightArm", "RightForeArm", "RightHand", "RightHand_Attach" };

        public readonly KwHelmArm Left, Right;

        private KwHelmArms(KwHelmArm left, KwHelmArm right) => (Left, Right) = (left, right);

        /// <summary>A player's arm bones, or null when the model lacks one.</summary>
        public static KwHelmArms? Of(Player player)
        {
            Transform visual = player.m_visual != null ? player.m_visual.transform : player.transform;
            Transform[] b = new Transform[Bones.Length];
            for (int i = 0; i < Bones.Length; i++)
            {
                Transform? bone = GameMaterials.Find(visual, Bones[i]);
                if (bone == null)
                    return null;
                b[i] = bone;
            }
            return new KwHelmArms(new KwHelmArm(b[0], b[1], b[2], b[3], -1f), new KwHelmArm(b[4], b[5], b[6], b[7], 1f));
        }
    }
}
