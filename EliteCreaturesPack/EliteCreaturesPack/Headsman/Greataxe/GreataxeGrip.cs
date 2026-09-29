using UnityEngine;

namespace EliteCreaturesPack.Headsman
{
    /// <summary>
    /// Keeps a player's left fist on the greataxe's haft after each pose (AssetWorkshop Greataxe/GreataxeGrip, the same
    /// solve the preview showed): the game's Battleaxe clips hold the left fist where the Battleaxe's straight haft is,
    /// and the borrowed swings wider still. The fist's grip (LeftHand_Attach) goes to the point of the haft, between the
    /// right fist and the head, nearest where the clip has it among the points the left arm reaches from its shoulder
    /// (the nearest to the shoulder when none is), turned so its +Z runs along the haft, and the forearm and upper arm
    /// follow (two-bone IK, the elbow kept on its side).
    /// </summary>
    public sealed class GreataxeGrip
    {
        /// <summary>The stretch of haft the fist may hold, up the model: from against the right fist to the head's start.</summary>
        private const float Low = 0.16f, High = 0.8f, Step = 0.01f;

        private readonly Transform arm, forearm, hand, grip;

        private GreataxeGrip(Transform arm, Transform forearm, Transform hand, Transform grip) =>
            (this.arm, this.forearm, this.hand, this.grip) = (arm, forearm, hand, grip);

        /// <summary>The grip for a player's rig, or null when it lacks the left arm's bones.</summary>
        public static GreataxeGrip? Of(Transform visual)
        {
            Transform? Bone(string name) => BundlePrefabs.GameMaterials.Find(visual, name);
            Transform? arm = Bone("LeftArm"), forearm = Bone("LeftForeArm"), hand = Bone("LeftHand"), grip = Bone("LeftHand_Attach");
            return arm == null || forearm == null || hand == null || grip == null ? null : new GreataxeGrip(arm, forearm, hand, grip);
        }

        /// <summary>After the animator has posed the player; `axe` the model in the right hand (its own frame, the axe's).</summary>
        public void Apply(Transform axe)
        {
            Vector3 at = grip.position;
            Vector3 on = Chosen(axe, at, out Vector3 along);
            Quaternion turn = Wrapped(along) * hand.rotation;
            Vector3 offset = turn * (Quaternion.Inverse(hand.rotation) * (at - hand.position));
            Vector3 elbow = forearm.position, middle = (arm.position + hand.position) * 0.5f;
            TwoBoneIk.Solve(arm, forearm, hand, on - offset, elbow + (elbow - middle).normalized * 0.3f);
            hand.rotation = turn;
        }

        /// <summary>The turn that lays the grip's +Z along the haft, whichever way along it the clip holds it.</summary>
        private Quaternion Wrapped(Vector3 along)
        {
            Vector3 z = grip.forward;
            return Quaternion.FromToRotation(z, Vector3.Dot(z, along) >= 0f ? along : -along);
        }

        /// <summary>The haft's point the fist takes (of those in reach, the nearest to `point`), and the haft's way there.</summary>
        private Vector3 Chosen(Transform axe, Vector3 point, out Vector3 along)
        {
            Vector3 shoulder = arm.position;
            float reach = 0.98f * ((forearm.position - shoulder).magnitude + (hand.position - forearm.position).magnitude + (grip.position - hand.position).magnitude);
            (Vector3 best, float height, float score) = (Vector3.zero, Low, float.MaxValue);
            for (float h = Low; h <= High; h += Step)
            {
                Vector3 p = axe.TransformPoint(HeadsmanAxe.Spine(h));
                float fromShoulder = (p - shoulder).magnitude;
                float s = fromShoulder <= reach ? (p - point).sqrMagnitude : 1000f + fromShoulder;
                if (s < score)
                {
                    (best, height, score) = (p, h, s);
                }
            }
            along = axe.TransformDirection(HeadsmanAxe.Along(height)).normalized;
            return best;
        }
    }
}
