using BundlePrefabs;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A helmsman posed after the animator: his upper body turned toward what he holds and leaned over it, half at the
    /// waist and half at the chest, and his fists put on their targets by two-bone IK (<see cref="KwHelmArms"/>,
    /// <see cref="KwHelmArm"/>). The turn goes only on a pose the animator wrote this frame, never
    /// twice on the same pose (an animator updating with physics skips frames); the arms are solved afresh every frame.
    /// </summary>
    public sealed class KwHelmPose
    {
        private readonly KwHelmArms arms;
        private readonly Transform spine, chest;
        private Quaternion posed;
        private bool turned;

        private KwHelmPose(Player player, KwHelmArms arms, Transform spine, Transform chest) =>
            (Player, this.arms, this.spine, this.chest) = (player, arms, spine, chest);

        public Player Player { get; }

        /// <summary>A player's bones, or null when the model lacks one.</summary>
        public static KwHelmPose? Of(Player player)
        {
            KwHelmArms? arms = KwHelmArms.Of(player);
            Transform visual = player.m_visual != null ? player.m_visual.transform : player.transform;
            Transform? spine = GameMaterials.Find(visual, "Spine"), chest = GameMaterials.Find(visual, "Spine1");
            return arms != null && spine != null && chest != null ? new KwHelmPose(player, arms, spine, chest) : null;
        }

        /// <summary>The hold's turn, lean and fists, each by a weight (1: all the way; 0: the animator's pose).</summary>
        public void Pose(KwHelmHold hold, float weight)
        {
            Transform body = Player.transform;
            Turn(hold.Lean * weight, hold.Twist * weight, body);
            if (hold.Left.HasValue)
                arms.Left.Reach(hold.Left.Value, weight, body);
            if (hold.Right.HasValue)
                arms.Right.Reach(hold.Right.Value, weight, body);
        }

        // The upper body turned to the right about the body's up and leaned forward about its right, half each at the waist
        // and at the chest, on a fresh pose only.
        private void Turn(float lean, float twist, Transform body)
        {
            if (Mathf.Abs(lean) < 0.01f && Mathf.Abs(twist) < 0.01f)
            {
                turned = false;
                return;
            }
            if (turned && Quaternion.Angle(spine.localRotation, posed) < 0.001f)
                return;
            Quaternion half = Quaternion.AngleAxis(twist * 0.5f, body.up) * Quaternion.AngleAxis(lean * 0.5f, body.right);
            spine.rotation = half * spine.rotation;
            chest.rotation = half * chest.rotation;
            (posed, turned) = (spine.localRotation, true);
        }
    }
}
