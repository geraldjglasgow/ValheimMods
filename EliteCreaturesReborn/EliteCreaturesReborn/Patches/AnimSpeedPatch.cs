using EliteCreaturesReborn.Runtime;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// Applies the attack-speed part of star scaling and Mad. The game resets a creature's animator speed to 1 each fixed
    /// step, but only while it is idle or moving - during an attack, a minor action, an emote or a stagger it leaves the
    /// value alone. So the scaled speed is written absolutely inside that same window and the attack inherits it for its
    /// whole swing; multiplying the live value would compound once per fixed step and run away. Runs on every machine
    /// (animation is local); the factor comes from synced traits, so every client animates it the same.
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
            if (Mathf.Approximately(factor, 1f))
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
    }
}
