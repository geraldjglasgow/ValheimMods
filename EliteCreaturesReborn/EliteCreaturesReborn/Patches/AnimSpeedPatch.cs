using EliteCreaturesReborn.Runtime;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// Applies the attack-speed part of star scaling and Mad. The game resets a creature's animator speed to 1 each fixed
    /// step, so a postfix multiplies it by the creature's swing factor - which speeds attacks along with the rest of its
    /// animation, matching the movement scaling applied elsewhere. Runs on every machine (animation is local); the factor
    /// comes from synced traits, so every client animates it the same.
    /// </summary>
    [HarmonyPatch(typeof(CharacterAnimEvent), "CustomFixedUpdate")]
    public static class AnimSpeedPatch
    {
        private static void Postfix(CharacterAnimEvent __instance) =>
            Guard.Run("CharacterAnimEvent.CustomFixedUpdate", () => Adjust(__instance));

        private static void Adjust(CharacterAnimEvent animEvent)
        {
            Traverse traverse = Traverse.Create(animEvent);
            Character character = traverse.Field("m_character").GetValue<Character>();
            Animator animator = traverse.Field("m_animator").GetValue<Animator>();
            if (character == null || animator == null)
            {
                return;
            }
            EliteController controller = character.GetComponent<EliteController>();
            if (controller == null || !controller.Ready)
            {
                return;
            }
            float factor = controller.SwingSpeedFactor;
            if (!Mathf.Approximately(factor, 1f))
            {
                animator.speed *= factor;
            }
        }
    }
}
