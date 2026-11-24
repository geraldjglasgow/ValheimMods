using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// What the wearer's animator says about a robe cloth (<see cref="RobeCloth"/>), the same on every client (the game
    /// sends these values for every player): when the skirt is pinned to its plain skinned shape (lying, riding, attached
    /// to a ship's mast or bench, swimming) and when the wearer stands still enough to build the cloth from (on the ground,
    /// upright, not seated, not pinned), since the cloth takes its fabric's lengths from the pose it is built in. Seated on
    /// a chair, a throne or the ground the cloth keeps moving: it drapes over the knees there (the workshop's cloth test,
    /// 2026-11-03), where the skinned skirt stood out round them like a drum.
    /// </summary>
    internal static class RobeClothPose
    {
        private static readonly int[] Pinning = Hashes("attach_bed", "attach_sitship", "attach_dragon", "attach_mast",
            "attach_lox", "attach_asksvin", "lying_down", "inWater");

        private static readonly int[] Seats = Hashes("attach_chair", "attach_throne");

        private static readonly int Sitting = ZSyncAnimation.GetHash("sitting");
        private static readonly int OnGround = ZSyncAnimation.GetHash("onGround");
        private static readonly int Crouching = ZSyncAnimation.GetHash("crouching");

        /// <summary>True while the skirt should keep its plain skinned shape.</summary>
        public static bool Pinned(Animator animator)
        {
            return !Ready(animator) || Any(animator, Pinning);
        }

        /// <summary>True while the wearer stands upright on the ground, not seated, and nothing pins the skirt.</summary>
        public static bool Upright(Animator animator) =>
            !Pinned(animator) && !Seated(animator) && animator.GetBool(OnGround) && !animator.GetBool(Crouching);

        private static bool Seated(Animator animator) =>
            Any(animator, Seats) || animator.GetCurrentAnimatorStateInfo(0).tagHash == Sitting;

        private static bool Any(Animator animator, int[] flags)
        {
            foreach (int flag in flags)
            {
                if (animator.GetBool(flag))
                    return true;
            }
            return false;
        }

        private static bool Ready(Animator animator) =>
            animator != null && animator.isActiveAndEnabled && animator.isInitialized && animator.runtimeAnimatorController != null;

        private static int[] Hashes(params string[] names)
        {
            var hashes = new int[names.Length];
            for (int i = 0; i < names.Length; i++)
                hashes[i] = ZSyncAnimation.GetHash(names[i]);
            return hashes;
        }
    }
}
