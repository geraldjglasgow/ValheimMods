using LimbIk;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// A player's left arm and hand in the spellbook's held pose (features/casters.md, "The spellbook", "Held open"), set
    /// after the animator by a weight (0: the animator's pose, 1: the whole pose): the palm put in front-left at about
    /// waist-to-chest height by two-bone IK (the LimbIk library; the elbow down, back and a little out, bent about 90
    /// degrees), the hand turned palm up with its fingers forward and a little across the body, tipped up toward the face,
    /// and the fingers opened (<see cref="BookFingers"/>). The hand's own frame is measured once from its bones: the
    /// knuckles' middle gives the fingers' way, index to little finger the way across, their cross the palm's normal (on
    /// the game's rig the hand bone's +Y, +X and +Z, read in game 2026-11-02), and the palm's middle on its skin.
    /// </summary>
    public sealed class BookHand
    {
        // Metres from the shoulder for the game's arm (upper arm 0.313, forearm 0.259 m, read in game 2026-11-02), scaled
        // by each player's own: the palm this far across the body, ahead of and below the shoulder.
        private const float GameArm = 0.572f;
        private const float Inward = 0.10f, Ahead = 0.33f, Below = 0.25f;
        // The point the elbow turns toward: below, behind and outside the shoulder.
        private const float ElbowDown = 0.35f, ElbowBack = 0.10f, ElbowOut = 0.12f;
        // The fingers turned this many degrees across the body and tipped up toward the face; the palm rolled toward it.
        private const float Turn = 20f, Tilt = 20f, Roll = 0.25f;
        // Where the book's spine rests: this share of the way from the wrist to the knuckles (0.6, the palm's middle, until
        // the user's "move the book up further on the hand", 2026-11-02), this far out on the palm's side.
        private const float PalmAlong = 0.95f, PalmDepth = 0.02f;
        private static readonly string[] Bones =
            { "LeftArm", "LeftForeArm", "LeftHand", "LeftHandIndex1", "LeftHandMiddle1", "LeftHandRing1", "LeftHandPinky1" };

        private readonly Transform upper, lower, hand;
        private readonly float scale;
        private readonly Quaternion frame;
        private BookFingers fingers;

        private BookHand(Transform[] b)
        {
            (upper, lower, hand) = (b[0], b[1], b[2]);
            float arm = (b[1].position - b[0].position).magnitude + (b[2].position - b[1].position).magnitude;
            scale = Mathf.Max(0.1f, arm / GameArm);
            Vector3 knuckles = (Local(b[3]) + Local(b[4]) + Local(b[5]) + Local(b[6])) * 0.25f;
            Along = knuckles.normalized;
            Normal = Vector3.Cross(Local(b[3]) - Local(b[6]), Along).normalized;
            Palm = knuckles * PalmAlong + Normal * PalmDepth;
            frame = Quaternion.Inverse(Quaternion.LookRotation(Along, Normal));
        }

        /// <summary>The hand bone.</summary>
        public Transform Hand => hand;

        /// <summary>The fingers' way in the hand's frame (its rotation, metres).</summary>
        public Vector3 Along { get; }

        /// <summary>The palm's normal in the hand's frame.</summary>
        public Vector3 Normal { get; }

        /// <summary>The palm's middle on its skin, metres in the hand's frame.</summary>
        public Vector3 Palm { get; }

        /// <summary>The way across the hand toward the thumb, in the hand's frame.</summary>
        public Vector3 Across => Vector3.Cross(Along, Normal);

        /// <summary>A player's left arm, hand and fingers, or null when the model lacks a bone.</summary>
        public static BookHand Of(Player player)
        {
            Transform root = player.m_visual != null ? player.m_visual.transform : player.transform;
            Transform[] bones = Pick(root, Bones);
            if (bones == null)
                return null;
            var arm = new BookHand(bones);
            arm.fingers = BookFingers.Of(arm.hand, arm.Along, arm.Across, arm.Normal);
            return arm.fingers != null ? arm : null;
        }

        /// <summary>The pose by a weight, after the animator; the body is the player's upright transform.</summary>
        public void Pose(Transform body, float weight)
        {
            Quaternion turned = Turned(body);
            Vector3 shoulder = upper.position;
            Vector3 palm = shoulder + (body.right * Inward + body.forward * Ahead - body.up * Below) * scale;
            Vector3 goal = Vector3.Lerp(hand.position, palm - turned * Palm, weight);
            Vector3 elbow = shoulder - (body.up * ElbowDown + body.forward * ElbowBack + body.right * ElbowOut) * scale;
            TwoBoneIk.Solve(upper, lower, hand, goal, elbow);
            hand.rotation = Quaternion.Slerp(hand.rotation, turned, weight);
            fingers.Open(hand, weight);
        }

        /// <summary>The bones under a root by name, in the names' order, or null when one is missing (one walk).</summary>
        internal static Transform[] Pick(Transform root, string[] names)
        {
            var found = new Transform[names.Length];
            foreach (Transform bone in root.GetComponentsInChildren<Transform>(true))
            {
                int at = System.Array.IndexOf(names, bone.name);
                if (at >= 0 && found[at] == null)
                    found[at] = bone;
            }
            foreach (Transform bone in found)
                if (bone == null)
                    return null;
            return found;
        }

        // The hand's wanted world rotation: fingers forward, turned across and tipped up; palm up, rolled toward the face.
        private Quaternion Turned(Transform body)
        {
            Vector3 up = body.up;
            Vector3 flat = Quaternion.AngleAxis(Turn, up) * body.forward;
            Vector3 fingersTo = Quaternion.AngleAxis(-Tilt, Vector3.Cross(up, flat)) * flat;
            Vector3 palmTo = Vector3.ProjectOnPlane(up + body.right * Roll, fingersTo);
            return Quaternion.LookRotation(fingersTo, palmTo) * frame;
        }

        private Vector3 Local(Transform bone) => Quaternion.Inverse(hand.rotation) * (bone.position - hand.position);
    }
}
