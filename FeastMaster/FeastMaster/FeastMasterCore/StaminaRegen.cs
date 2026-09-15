using HarmonyLib;
using UnityEngine;

namespace FeastMaster
{
    /// <summary>
    /// The regeneration multiplier rules, applied through the game's single hook for status effect multipliers:
    /// Player.UpdateStats calls SEMan.ModifyStaminaRegen(ref multiplier) and multiplies its regen by the result.
    /// Vigor, extra stamina and the sneak bonus are added to the multiplier; the bar-level curve then scales it.
    /// </summary>
    [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyStaminaRegen))]
    public static class StaminaRegenMultiplierPatch
    {
        [HarmonyPostfix]
        public static void Postfix(SEMan __instance, ref float staminaMultiplier)
        {
            if (!(__instance.m_character is Player player))
                return;

            staminaMultiplier += Vigor(player) / 100f;
            staminaMultiplier += ExtraStaminaBonus(player) / 100f;
            staminaMultiplier += SneakBonus(player) / 100f;
            staminaMultiplier *= CurveFactor(player);
            if (player.IsBlocking())
                staminaMultiplier *= Settings.BlockingRegenFactor.Value / Settings.GameBlockingRegenFactor;
        }

        /// <summary>Total Vigor of the active foods in percent. Vigor does not fade with the food.</summary>
        public static float Vigor(Player player)
        {
            float total = 0f;
            foreach (Player.Food food in player.m_foods)
                total += ItemValues.VigorOf(food.m_item);
            return total;
        }

        private static float ExtraStaminaBonus(Player player)
        {
            float perPoint = Settings.RegenPerExtraStaminaPoint.Value;
            if (perPoint == 0f)
                return 0f;
            return Mathf.Max(0f, ExtraStamina(player)) * perPoint;
        }

        /// <summary>Max stamina above the base and the skill bonus, or the active foods' current stamina.</summary>
        private static float ExtraStamina(Player player)
        {
            if (!Settings.CountFoodStaminaOnly.Value)
                return player.GetMaxStamina() - Settings.BaseStamina.Value - BaseValuesPatch.SkillBonus(player);

            float total = 0f;
            foreach (Player.Food food in player.m_foods)
                total += food.m_stamina;
            return total;
        }

        /// <summary>Crouched and standing still: the game's IsSneaking is crouching while moving.</summary>
        private static float SneakBonus(Player player)
        {
            float bonus = Settings.SneakSkillRegenBonus.Value;
            if (bonus == 0f || !player.IsCrouching() || player.IsSneaking())
                return 0f;
            return bonus * player.GetSkillFactor(Skills.SkillType.Sneak);
        }

        public static float CurveFactor(Player player)
        {
            float max = player.GetMaxStamina();
            float fraction = max > 0f ? player.GetStamina() / max : 1f;
            return RegenCurve.Factor(fraction, Settings.RegenCurveStrength.Value, Settings.RegenCurvePivot.Value);
        }
    }

    /// <summary>
    /// The bar-level regeneration curve shared by stamina and eitr: piecewise linear in the bar fraction s,
    /// strength M at s = 0, 1 at the pivot P, 1/M at s = 1. Strength 1 (or below 0) is off.
    /// </summary>
    public static class RegenCurve
    {
        public static float Factor(float fraction, float strength, float pivot)
        {
            if (strength <= 0f || Mathf.Approximately(strength, 1f))
                return 1f;
            fraction = Mathf.Clamp01(fraction);
            pivot = Mathf.Clamp01(pivot);
            if (fraction < pivot)
                return Mathf.Lerp(strength, 1f, fraction / pivot);
            float above = 1f - pivot;
            return above <= 0f ? 1f : Mathf.Lerp(1f, 1f / strength, (fraction - pivot) / above);
        }
    }

    /// <summary>
    /// Regeneration while encumbered or swimming. The game zeroes regeneration in both states; this postfix
    /// applies the game's own formula at the configured fraction, with the same multipliers, the regen delay
    /// timer and Game.m_staminaRegenRate, capped at max stamina. The game's other zero conditions (attacking,
    /// dodging, wall running) still stop regeneration.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.UpdateStats), typeof(float))]
    public static class RestrictedStaminaRegenPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Player __instance, float dt)
        {
            if (__instance.InIntro() || __instance.IsTeleporting() || __instance.InAttack() || __instance.InDodge() || __instance.m_wallRunning)
                return;
            float fraction = Fraction(__instance);
            if (fraction <= 0f)
                return;

            float maxStamina = __instance.GetMaxStamina();
            if (__instance.m_stamina >= maxStamina || __instance.m_staminaRegenTimer > 0f)
                return;

            float regen = Regen(__instance, maxStamina) * (__instance.IsBlocking() ? Settings.GameBlockingRegenFactor : 1f) * fraction;
            __instance.m_stamina = Mathf.Min(maxStamina, __instance.m_stamina + regen * dt * Game.m_staminaRegenRate);
            __instance.m_nview.GetZDO().Set(ZDOVars.s_stamina, __instance.m_stamina);
        }

        /// <summary>
        /// The fraction of normal regen allowed by the player's state; 0 when the game already regenerates.
        /// Encumbered wins over swimming when both apply.
        /// </summary>
        private static float Fraction(Player player)
        {
            if (player.IsEncumbered())
                return Mathf.Clamp01(Settings.EncumberedRegenFraction.Value);
            if (!player.IsSwimming() || player.IsOnGround())
                return 0f;
            return SwimmingRegenDelayPatch.DelayElapsed() ? Mathf.Clamp01(Settings.SwimmingRegenFraction.Value) : 0f;
        }

        private static float Regen(Player player, float maxStamina)
        {
            float regen = player.m_staminaRegen + (1f - player.m_stamina / maxStamina) * player.m_staminaRegen * player.m_staminaRegenTimeMultiplier;
            float multiplier = 1f;
            player.m_seman.ModifyStaminaRegen(ref multiplier);
            return regen * multiplier;
        }
    }

    /// <summary>Remembers the last swimming stroke (the game drains stamina when the swim input exceeds 0.1).</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.OnSwimming))]
    public static class SwimmingRegenDelayPatch
    {
        private static float lastStrokeTime = -1000f;

        [HarmonyPostfix]
        public static void Postfix(Vector3 targetVel)
        {
            if (targetVel.magnitude > 0.1f)
                lastStrokeTime = Time.time;
        }

        public static bool DelayElapsed()
        {
            return Time.time - lastStrokeTime >= Mathf.Max(0f, Settings.SwimmingRegenDelay.Value);
        }
    }
}
