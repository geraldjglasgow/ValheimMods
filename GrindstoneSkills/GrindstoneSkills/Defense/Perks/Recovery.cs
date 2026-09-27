using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Out-of-combat regeneration, on the local player's own client: every Regeneration Interval, a player who has not
    /// attacked, blocked or taken damage for Out Of Combat Delay seconds heals Regeneration percent of their max health
    /// at level 100. The game's own health regeneration modifiers (SEMan.ModifyHealthRegen: rested, meads, anything
    /// that stops regeneration) scale it as they scale food regeneration.
    /// <para>Combat is the player's own attacks (Humanoid.m_lastCombatTimer, reset when an attack starts), plus every
    /// hit with an attacker and all damage taken, which <see cref="IncomingHit"/> and <see cref="DamageIntake"/> mark
    /// here. Blocks are hits with an attacker, so they count.</para>
    /// </summary>
    public static class Recovery
    {
        private static float lastCombat = float.NegativeInfinity;
        private static float timer;

        public static void MarkCombat() => lastCombat = Time.time;

        /// <summary>The local player has been out of combat for the delay.</summary>
        public static bool OutOfCombat(Player player)
        {
            float delay = Mathf.Max(0f, DefenseSettings.OutOfCombatDelay.Value);
            return Time.time - lastCombat >= delay && player.m_lastCombatTimer >= delay;
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Update))]
        private static class Tick
        {
            [HarmonyPostfix]
            private static void Postfix(Player __instance)
            {
                if (!DefenseSkill.IsLocal(__instance))
                    return;
                timer += Time.deltaTime;
                if (timer < DefenseSettings.RegenerationInterval.Value)
                    return;
                timer = 0f;
                HookGuard.Run("defense regeneration", () => Heal(__instance));
            }
        }

        private static void Heal(Player player)
        {
            float share = DefenseSkill.LocalShare(DefenseSettings.Regeneration.Value);
            if (share <= 0f || player.IsDead() || player.GetHealth() >= player.GetMaxHealth() || !OutOfCombat(player))
                return;
            float multiplier = 1f;
            player.GetSEMan().ModifyHealthRegen(ref multiplier);
            player.Heal(player.GetMaxHealth() * share * multiplier);
        }
    }
}
