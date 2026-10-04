using EliteCreaturesReborn.Aspects;
using EliteCreaturesReborn.Runtime;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// Applies the attack-speed part of star scaling and Mad, and Tethered's live boost as the pair's health draws apart
    /// (<see cref="TetherLink.SwingFactor"/>). The game resets a creature's animator speed to 1 each fixed
    /// step, but only while it is idle or moving - during an attack, a minor action, an emote or a stagger it leaves the
    /// value alone. So the scaled speed is written absolutely inside that same window and the attack inherits it for its
    /// whole swing; multiplying the live value would compound once per fixed step and run away. Runs on every machine
    /// (animation is local); the factor comes from synced traits and, for Tethered, from the pair's health in their ZDOs,
    /// so every client animates it the same. Tethered's part changes from step to step, which the absolute write takes
    /// as it comes: the fixed factor times the live one, never times the animator's current speed.
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
            float factor = Factor(character);
            if (animator == null || Mathf.Approximately(factor, 1f))
            {
                return;
            }
            // Mirrors the game's own reset condition. Outside it the animator speed belongs to the game - an attack's
            // authored speed, a freeze frame - and writing there would either fight it or compound onto itself.
            if (!character.InAttack() && !character.InMinorAction() && !character.InEmote() && character.CanMove())
            {
                animator.speed = factor;
            }
        }

        /// <summary>The creature's swing speed from its stars and Mad, times a Tethered boss's gap; 1 for anything else.</summary>
        private static float Factor(Character character)
        {
            EliteController controller = character != null ? character.GetComponent<EliteController>() : null!;
            return controller == null || !controller.Ready
                ? 1f : controller.SwingSpeedFactor * TetherLink.SwingFactor(controller);
        }
    }
}
