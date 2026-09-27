using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Cheaper blocks and dodges, on the local player's own client.
    /// <list type="bullet">
    /// <item>Blocks: Humanoid.BlockAttack passes its stamina cost through SEMan.ModifyBlockStaminaUsage, which the
    /// inventory's equipment readout also calls with a modifier rather than a cost; so the cost is lowered only while
    /// <see cref="BlockHooks.InBlock"/>, and only when positive (a parry that gives stamina back stays as it is).</item>
    /// <item>Dodges: Player.GetDodgeStaminaUse already takes the game's Dodge skill off; this takes Dodge Stamina
    /// Reduction off what is left.</item>
    /// </list>
    /// </summary>
    public static class StaminaPerks
    {
        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyBlockStaminaUsage))]
        private static class Block
        {
            [HarmonyPostfix]
            private static void Postfix(SEMan __instance, ref float staminaUse)
            {
                if (BlockHooks.InBlock && staminaUse > 0f && DefenseSkill.IsLocal(__instance.m_character))
                    staminaUse *= 1f - Mathf.Clamp01(DefenseSkill.LocalShare(DefenseSettings.BlockStamina.Value));
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.GetDodgeStaminaUse))]
        private static class Dodge
        {
            [HarmonyPostfix]
            private static void Postfix(Player __instance, ref float __result)
            {
                if (DefenseSkill.IsLocal(__instance))
                    __result *= 1f - Mathf.Clamp01(DefenseSkill.LocalShare(DefenseSettings.DodgeStamina.Value));
            }
        }
    }
}
