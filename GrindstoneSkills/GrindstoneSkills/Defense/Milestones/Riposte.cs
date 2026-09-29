using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Riposte, the level 25 milestone, on the local player's own client. A parry whose guard held arms it for Riposte
    /// Window seconds; the first attack the player starts in that time (Humanoid.StartAttack) is the riposte, and every
    /// melee hit that attack lands deals Riposte Damage percent more.
    /// <list type="bullet">
    /// <item>Hits are built on the attacker's client and sent through Character.Damage, so the prefix there changes the
    /// hit before it leaves. Ranged hits (arrows, bolts, thrown spears) are left alone.</item>
    /// <item>No stagger of its own: the hit's m_staggerMultiplier stays as the attack set it, so the target's owner
    /// (Character.RPC_Damage, which forces a stagger at 100 or more) staggers it only as it would any hit of that
    /// size.</item>
    /// <item>The riposte ends with its attack: a new attack or the next parry replaces it.</item>
    /// </list>
    /// </summary>
    public static class Riposte
    {
        private static float armedUntil = float.NegativeInfinity;
        private static Attack riposte;
        private static bool calledOut;

        /// <summary>Seconds the riposte stays armed, or 0.</summary>
        public static float ArmedFor => Mathf.Max(0f, armedUntil - Time.time);

        public static void OnParry()
        {
            if (!DefenseSkill.LocalReached(DefenseMilestoneSettings.RiposteLevel.Value))
                return;
            armedUntil = Time.time + DefenseMilestoneSettings.RiposteWindow.Value;
            DefenseEffects.Refresh();
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.StartAttack))]
        private static class Start
        {
            [HarmonyPostfix]
            private static void Postfix(Humanoid __instance, bool __result)
            {
                if (!__result || !DefenseSkill.IsLocal(__instance))
                    return;
                riposte = ArmedFor > 0f ? __instance.m_currentAttack : null;
                armedUntil = float.NegativeInfinity;
                calledOut = false;
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
        private static class Hit
        {
            [HarmonyPrefix]
            private static void Prefix(Character __instance, HitData hit)
            {
                if (riposte != null && hit != null && !hit.m_ranged && !DefenseSkill.IsLocal(__instance))
                    HookGuard.Run("riposte", () => Empower(hit));
            }
        }

        private static void Empower(HitData hit)
        {
            Player player = Player.m_localPlayer;
            if (player == null || player.m_currentAttack != riposte || hit.GetAttacker() != player)
                return;
            hit.ApplyModifier(1f + Mathf.Max(0f, DefenseMilestoneSettings.RiposteDamage.Value) / 100f);
            if (!calledOut)
                DefenseCallout.Show(hit.m_point, "Riposte!");
            calledOut = true;
        }
    }
}
