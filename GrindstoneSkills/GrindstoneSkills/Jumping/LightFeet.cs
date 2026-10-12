using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Light Feet, Jumping's perk from level 0: the local player's jumps cost up to <see cref="SavingAt100"/> less
    /// stamina at Jump 100, linear in the level.
    /// <para>The game reads one field, Character.m_jumpStaminaUsage, both for the check and for the cost. Character.Jump
    /// checks HaveStamina(m_jumpStaminaUsage) to decide a full jump from a tired one (weaker, no experience), and
    /// ForceJump calls Player.OnJump, which spends that field after gear (GetEquipmentMovementModifier and the jump
    /// stamina modifier) and SEMan.ModifyJumpStaminaUsage, every term proportional to the field, times the world's
    /// movement stamina rate. Lowering the field for the length of Character.Jump therefore makes the check agree
    /// with the cheaper cost, and every gear and status effect still applies on top. The grappling pull, which Jump pays
    /// from the same field, gets cheaper too. Character.ForceJump gets the same scope for the launches that call it
    /// directly (an Aoe or catapult throw, a grappling point), which the game also charges as a jump; inside Jump the
    /// field is lowered only once. A finalizer puts the field back, so the prefab's value and any other mod's stay.</para>
    /// <para>Runs on the player's own client, which owns the player and spends their stamina. Creatures and other players
    /// return at the first test. Off, or at Jump 0, the field is not touched.</para>
    /// The prefix runs last and the finalizer first, so this save and restore sits inside any other mod's save and
    /// restore of the same field (FeastMaster's, for one): Harmony runs finalizers in prefix order, not reversed.
    /// </summary>
    public static class LightFeet
    {
        private const float SavingAt100 = 0.5f;

        private static bool lowered;

        /// <summary>The share of the game's jump stamina a jump costs at a Jump factor (0..1).</summary>
        public static float CostShare(float factor) => 1f - SavingAt100 * Mathf.Clamp01(factor);

        [HarmonyPatch(typeof(Character), nameof(Character.Jump))]
        private static class JumpCall
        {
            [HarmonyPrefix, HarmonyPriority(Priority.Last)]
            private static void Prefix(Character __instance, out float __state) => __state = Begin(__instance);

            [HarmonyFinalizer, HarmonyPriority(Priority.First)]
            private static void Finalizer(Character __instance, float __state) => End(__instance, __state);
        }

        [HarmonyPatch(typeof(Character), nameof(Character.ForceJump))]
        private static class ForceJumpCall
        {
            [HarmonyPrefix, HarmonyPriority(Priority.Last)]
            private static void Prefix(Character __instance, out float __state) => __state = Begin(__instance);

            [HarmonyFinalizer, HarmonyPriority(Priority.First)]
            private static void Finalizer(Character __instance, float __state) => End(__instance, __state);
        }

        /// <summary>Opens the scope for the local player; returns the field's value to restore, or NaN when untouched.</summary>
        private static float Begin(Character character)
        {
            if (lowered || !LocalSkill.IsLocal(character) || !GameSkillSettings.Jumping.Value)
                return float.NaN;
            return HookGuard.Run("light feet", static target => Lower(target), character, float.NaN);
        }

        private static float Lower(Character character)
        {
            float share = CostShare(LocalSkill.Factor(Skills.SkillType.Jump));
            if (share >= 1f)
                return float.NaN;
            float original = character.m_jumpStaminaUsage;
            character.m_jumpStaminaUsage = original * share;
            lowered = true;
            return original;
        }

        private static void End(Character character, float original)
        {
            if (float.IsNaN(original))
                return;
            character.m_jumpStaminaUsage = original;
            lowered = false;
        }
    }
}
