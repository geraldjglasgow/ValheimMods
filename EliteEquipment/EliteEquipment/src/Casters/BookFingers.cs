using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The left hand's fingers opened under the spellbook (features/casters.md, "Held open"): each finger's three segments
    /// turned, from the knuckle out, onto a way fixed in the hand's frame, after the animator and by a weight. The four
    /// fingers lie nearly straight along the hand, fanned a little as their knuckles are, each segment curled a few degrees
    /// more toward the palm (a gently cupped, open hand); the thumb lies out to the side in the palm's plane. A segment's
    /// way is from its joint to the next (the third to the tip bone the game's rig has after it).
    /// </summary>
    public sealed class BookFingers
    {
        private const int Fingers = 5, Segments = 3, Chain = Segments + 1;
        // How much of the knuckles' own fan the fingers keep.
        private const float Fan = 0.4f;
        // The thumb's way: this much along the fingers, this much out across the hand.
        private const float ThumbAlong = 0.6f, ThumbOut = 0.8f;
        private static readonly string[] Names = Joints();
        // Degrees toward the palm, per segment from the knuckle out (the thumb last in the names).
        private static readonly float[] Curl = { 4f, 8f, 12f };
        private static readonly float[] ThumbCurl = { 0f, 4f, 8f };

        private readonly Transform[] joints;
        private readonly Vector3[] ways;

        private BookFingers(Transform[] joints, Vector3[] ways) => (this.joints, this.ways) = (joints, ways);

        /// <summary>A hand's finger bones with their open ways, or null when the rig lacks one.</summary>
        public static BookFingers Of(Transform hand, Vector3 along, Vector3 across, Vector3 normal)
        {
            Transform[] found = BookHand.Pick(hand, Names);
            if (found == null)
                return null;
            var joints = new Transform[Fingers * Chain];
            for (int i = 0; i < found.Length; i++)
                joints[i / Segments * Chain + i % Segments] = found[i];
            for (int f = 0; f < Fingers; f++)
            {
                Transform last = joints[f * Chain + Segments - 1];
                if (last.childCount == 0)
                    return null;
                joints[f * Chain + Segments] = last.GetChild(0);
            }
            return new BookFingers(joints, Ways(hand, joints, along, across, normal));
        }

        /// <summary>Each segment turned toward its open way by the weight (the hand already posed).</summary>
        public void Open(Transform hand, float weight)
        {
            Quaternion frame = hand.rotation;
            for (int i = 0; i < ways.Length; i++)
            {
                int at = i / Segments * Chain + i % Segments;
                Transform joint = joints[at];
                Quaternion turn = Quaternion.FromToRotation(joints[at + 1].position - joint.position, frame * ways[i]);
                joint.rotation = Quaternion.Slerp(Quaternion.identity, turn, weight) * joint.rotation;
            }
        }

        private static string[] Joints()
        {
            string[] fingers = { "Index", "Middle", "Ring", "Pinky", "Thumb" };
            var names = new string[Fingers * Segments];
            for (int i = 0; i < names.Length; i++)
                names[i] = "LeftHand" + fingers[i / Segments] + (i % Segments + 1);
            return names;
        }

        private static Vector3[] Ways(Transform hand, Transform[] joints, Vector3 along, Vector3 across, Vector3 normal)
        {
            var ways = new Vector3[Fingers * Segments];
            for (int f = 0; f < Fingers; f++)
            {
                bool thumb = f == Fingers - 1;
                Vector3 spread = thumb
                    ? (along * ThumbAlong + across * ThumbOut).normalized
                    : Fanned(hand, joints[f * Chain], along, across);
                float[] curl = thumb ? ThumbCurl : Curl;
                for (int s = 0; s < Segments; s++)
                    ways[f * Segments + s] = Vector3.RotateTowards(spread, normal, curl[s] * Mathf.Deg2Rad, 0f);
            }
            return ways;
        }

        // A finger's way: along the hand, turned toward its knuckle's side by part of the knuckles' fan.
        private static Vector3 Fanned(Transform hand, Transform knuckle, Vector3 along, Vector3 across)
        {
            Vector3 at = Quaternion.Inverse(hand.rotation) * (knuckle.position - hand.position);
            float side = Vector3.Dot(at, across) / Mathf.Max(0.01f, Vector3.Dot(at, along));
            return (along + across * (side * Fan)).normalized;
        }
    }
}
