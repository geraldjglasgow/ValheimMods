using HarmonyLib;
using Translations;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Second Wind, Running's milestone at <see cref="UnlockLevel"/>: when the local player runs their stamina out, they
    /// get <see cref="StaminaBack"/> back at once and keep running, with "Second wind!" over their head; then it rests
    /// for <see cref="Cooldown"/> seconds.
    /// <para>Player.CheckRun spends the run's stamina each physics tick and returns false when it runs out. A prefix notes
    /// whether the player had stamina before the call and a postfix sees it gone: that is the tick the run ended for want
    /// of stamina. When base.CheckRun refuses (no run key, no move input, crouching, encumbered, dodging) nothing is
    /// spent, so nothing fires. The stamina is given with Player.AddStamina, which the game caps at max stamina.</para>
    /// <para>Runs on the player's own client, which owns the player and its stamina; the callout is drawn there only.
    /// Every other player returns at the first test. The cooldown is a timer on this client. Off, or below the level,
    /// CheckRun's result is left as the game gave it.</para>
    /// </summary>
    public static class SecondWind
    {
        private const float UnlockLevel = 75f;
        private const float StaminaBack = 30f;
        private const float Cooldown = 180f;

        private static float readyAt = float.NegativeInfinity;

        /// <summary>The Run level that unlocks Second Wind.</summary>
        public static float Level => UnlockLevel;

        /// <summary>The stamina it gives back.</summary>
        public static float Stamina => StaminaBack;

        /// <summary>Seconds between two second winds.</summary>
        public static float Rest => Cooldown;

        /// <summary>Seconds until it can fire again; 0 when ready.</summary>
        public static float ReadyIn => Mathf.Max(0f, readyAt - Time.time);

        [HarmonyPatch(typeof(Player), nameof(Player.CheckRun))]
        private static class Run
        {
            [HarmonyPrefix]
            private static void Prefix(Player __instance, out bool __state) =>
                __state = LocalSkill.IsLocal(__instance) && __instance.HaveStamina();

            [HarmonyPostfix]
            private static void Postfix(Player __instance, bool __state, ref bool __result)
            {
                if (!__state || __result || __instance.HaveStamina() || !GameSkillSettings.Running.Value)
                    return;
                if (HookGuard.Run("second wind", static player => TryFire(player), __instance, false))
                    __result = true;
            }
        }

        /// <summary>Gives the stamina back when the perk is unlocked and rested; true when it fired.</summary>
        private static bool TryFire(Player player)
        {
            if (Time.time < readyAt || !LocalSkill.Reached(Skills.SkillType.Run, UnlockLevel))
                return false;
            readyAt = Time.time + Cooldown;
            player.AddStamina(StaminaBack);
            FloatingText.Show(player.GetTopPoint() + Vector3.up * 0.3f, Lang.Get("gs_run_secondwind_callout"));
            return true;
        }
    }
}
