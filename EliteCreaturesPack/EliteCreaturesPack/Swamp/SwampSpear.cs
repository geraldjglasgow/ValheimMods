using System.Linq;
using EliteCreaturesPack.Core;
using UnityEngine;

namespace EliteCreaturesPack.Swamp
{
    /// <summary>The player's vanilla spear thrust retargeted onto the Draugr humanoid avatar.</summary>
    public static class SwampSpear
    {
        public static void Animate(GameObject creature, ZNetScene scene)
        {
            Animator animator = creature.GetComponentInChildren<Animator>(true);
            RuntimeAnimatorController controller = animator.runtimeAnimatorController;
            AnimationClip? swing = controller.animationClips.FirstOrDefault(clip => clip.name == "Standing Melee Attack Horizontal");
            Animator? player = scene.GetPrefab("Player")?.GetComponentInChildren<Animator>(true);
            AnimationClip? thrust = player?.runtimeAnimatorController.animationClips.FirstOrDefault(clip => clip.name == "Javelin Stab");
            if (swing == null || thrust == null)
            {
                Log.Warn("Reed Stalker spear animation unavailable; using the Draugr's melee swing.");
                return;
            }
            AnimationClip copy = Object.Instantiate(thrust);
            copy.name = "ecp_reed_thrust";
            copy.events = thrust.events.Select(e => Early(e, thrust.length)).ToArray();
            var overrides = new AnimatorOverrideController(controller) { name = "ecp_reed_animator" };
            overrides[swing] = copy;
            animator.runtimeAnimatorController = overrides;
        }

        private static AnimationEvent Early(AnimationEvent animationEvent, float length)
        {
            if (animationEvent.functionName == "OnAttackTrigger" || animationEvent.functionName == "Hit")
                animationEvent.time = Mathf.Min(animationEvent.time, length * 0.75f);
            return animationEvent;
        }
    }
}
