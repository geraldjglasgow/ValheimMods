using UnityEngine;

namespace HighSeatPlus.Crowd
{
    /// <summary>
    /// A far player's animator updated <see cref="StepSeconds"/> apart instead of every frame. Unity animates an enabled
    /// animator every frame; this one is disabled, with its state and parameters kept and nothing written back on disable
    /// (keepAnimatorStateOnDisable, writeDefaultValuesOnDisable), and handed all the time gone by in one Animator.Update
    /// per step. The state machine, transitions, animation events, IK (the game's head look and foot planting) and the
    /// bones are the game's own, only less often. The game keeps setting its parameters from the owner's ZDO
    /// (ZSyncAnimation, every physics step) and its triggers by RPC; each step takes them up. What hangs on the bones
    /// (weapons, shield, helmet, back items, equipped effects) moves with them; the body itself moves every frame as the
    /// game moves it (ZSyncTransform), hits and colliders do not depend on it, and foot steps are the owner's RPC. The
    /// players' steps are spread over the frames by a random first wait, so no frame does them all. Back to full: the
    /// time still owed is given first, then the animator is enabled and runs on from where it is, so there is no pose to
    /// jump from, and its own disable settings come back. Not started for an animator that is not running (not set up
    /// yet, turned off by something else); let go at once if something else turns it on again. A body the game has hidden
    /// (a death) is not stepped. A step runs on the main thread, where Unity would spread every animator over its worker
    /// threads, so the gain is in doing a sixth as many at 60 frames a second; a <see cref="StepSeconds"/> of 0 leaves
    /// animators to Unity and only the cloth is held still.
    /// </summary>
    internal static class SteppedAnimator
    {
        /// <summary>Seconds between two updates of a far player's animator: ten a second.</summary>
        internal const float StepSeconds = 0.1f;

        public static void Start(FarPlayer far)
        {
            Animator animator = far.Animator;
            if (StepSeconds <= 0f || far.Stepping || animator == null || !animator.isActiveAndEnabled || !animator.isInitialized)
            {
                return;
            }
            far.KeptState = animator.keepAnimatorStateOnDisable;
            far.WroteDefaults = animator.writeDefaultValuesOnDisable;
            animator.keepAnimatorStateOnDisable = true;
            animator.writeDefaultValuesOnDisable = false;
            animator.enabled = false;
            far.Stepping = true;
            far.Pending = -Random.value * StepSeconds;
        }

        /// <summary>Every frame while stepped: the frame's time added, and one update once a step's worth is owed.</summary>
        public static void Step(FarPlayer far, float dt)
        {
            far.Pending += dt;
            if (far.Pending < StepSeconds)
            {
                return;
            }
            Animator animator = far.Animator;
            if (animator == null || animator.enabled)
            {
                // Gone with its player, or something else runs it again: leave it to that.
                Release(far, animator);
                return;
            }
            if (animator.gameObject.activeInHierarchy)
            {
                animator.Update(far.Pending);
            }
            far.Pending = 0f;
        }

        public static void Stop(FarPlayer far)
        {
            if (!far.Stepping)
            {
                return;
            }
            Animator animator = far.Animator;
            if (animator != null && !animator.enabled)
            {
                if (far.Pending > 0f && animator.gameObject.activeInHierarchy)
                {
                    animator.Update(far.Pending);
                }
                animator.enabled = true;
            }
            Release(far, animator);
        }

        private static void Release(FarPlayer far, Animator animator)
        {
            far.Stepping = false;
            far.Pending = 0f;
            if (animator != null)
            {
                animator.keepAnimatorStateOnDisable = far.KeptState;
                animator.writeDefaultValuesOnDisable = far.WroteDefaults;
            }
        }
    }
}
