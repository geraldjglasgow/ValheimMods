using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Long Breath, on the local player's own client, which simulates the player. Player.OnSwimming runs each physics frame
    /// while the player swims (on the owner only); with no stamina left it adds the frame's time to m_drownDamageTimer, and
    /// each time that passes 1 s it resets it and hits the player for a twentieth of max health (HitType.Drowning, through
    /// Character.Damage). The game never resets the timer when stamina comes back.
    /// <list type="bullet">
    /// <item>Delay: for the first <see cref="DelayAt100"/> x the Swim factor seconds of a spell without stamina, a prefix
    /// takes the frame's time back off the timer before the game adds it, so the game's own count stands still and goes on
    /// where it was once the delay ends. A spell ends when stamina returns, or after <see cref="SpellGap"/> s without a
    /// breathless swimming frame (standing on the bottom, or out of the water).</item>
    /// <item>Damage: a prefix on Character.Damage scales the local player's drowning hits by 1 - <see cref="SofterAt100"/>
    /// x the Swim factor before the hit is sent, so armour, Defense and the rest see the smaller hit.</item>
    /// <item>Off, or at Swim 0, the timer and the hit are left as the game made them.</item>
    /// </list>
    /// </summary>
    public static class LongBreath
    {
        /// <summary>Seconds drowning starts later at Swim 100.</summary>
        private const float DelayAt100 = 3f;

        /// <summary>How much less a drowning hit hurts at Swim 100.</summary>
        private const float SofterAt100 = 0.5f;

        /// <summary>Seconds without a breathless swimming frame that end a spell, so the next one gets the delay again.</summary>
        private const float SpellGap = 1f;

        private static float breathlessSince = float.NegativeInfinity;
        private static float lastBreathless = float.NegativeInfinity;

        /// <summary>Seconds drowning starts later at a Swim factor (0..1).</summary>
        public static float Delay(float factor) => DelayAt100 * factor;

        /// <summary>The share of a drowning hit left at a Swim factor (0..1).</summary>
        public static float Kept(float factor) => 1f - SofterAt100 * factor;

        [HarmonyPatch(typeof(Player), nameof(Player.OnSwimming))]
        private static class Swim
        {
            [HarmonyPrefix]
            private static void Prefix(Player __instance, float dt)
            {
                if (ReferenceEquals(__instance, Player.m_localPlayer) && GameSkillSettings.Swimming.Value)
                    HookGuard.Run("long breath", static swim => Hold(swim.player, swim.dt), (player: __instance, dt));
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.Damage), typeof(HitData))]
        private static class Drown
        {
            [HarmonyPrefix]
            private static void Prefix(Character __instance, HitData hit)
            {
                if (hit == null || hit.m_hitType != HitData.HitType.Drowning || !LocalSkill.IsLocal(__instance))
                    return;
                if (GameSkillSettings.Swimming.Value)
                    HookGuard.Run("long breath damage", static h => Soften(h), hit);
            }
        }

        private static void Hold(Player player, float dt)
        {
            if (player.HaveStamina())
            {
                lastBreathless = float.NegativeInfinity;
                return;
            }
            float now = Time.time;
            if (now - lastBreathless > SpellGap)
                breathlessSince = now;
            lastBreathless = now;
            if (now - breathlessSince < Delay(LocalSkill.Factor(Skills.SkillType.Swim)))
                player.m_drownDamageTimer -= dt;
        }

        private static void Soften(HitData hit)
        {
            float kept = Kept(LocalSkill.Factor(Skills.SkillType.Swim));
            if (kept < 1f)
                hit.m_damage.Modify(kept);
        }
    }
}
