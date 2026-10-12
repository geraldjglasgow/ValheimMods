using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Counter, the level 50 perk, on the local player's own client: a perfect dodge arms it for 2 s, the first melee
    /// attack the player starts in that time is the counter, and every melee hit that attack lands deals 25% more.
    /// Read from the game code 2026-10-11:
    /// <list type="bullet">
    /// <item>A hit that meets a rolling player calls Player.HitWhileDodging on the attacker's machine, which sends
    /// RPC_HitWhileDodging to the player's own client; the first one of a roll (m_beenHitWhileDodging still false) is the
    /// perfect dodge, with its stamina back, adrenaline and 1 Dodge experience. The postfix arms Counter when the game
    /// counted one.</item>
    /// <item>The counter is chosen in Humanoid.StartAttack, as Defense's Riposte is: a melee attack (Attack.AttackType
    /// Horizontal, Vertical or Area) started while armed becomes the counter and disarms it; a bow, staff or throw
    /// neither uses it up nor gains from it. The next attack, a chain's next swing included, is not the counter.</item>
    /// <item>Hits are built on the attacker's client and pass through <see cref="OutgoingHits"/> before they are sent,
    /// so the bonus travels with the hit and the target's owner applies it by the game's own rules. Ranged hits
    /// (HitData.m_ranged) are left alone. No stagger of its own, and separate from Riposte: both can apply.</item>
    /// </list>
    /// </summary>
    public static class DodgeCounter
    {
        /// <summary>The Dodge level Counter needs.</summary>
        private const float Level = 50f;

        /// <summary>Seconds after a perfect dodge in which a melee attack started is the counter.</summary>
        private const float Window = 2f;

        /// <summary>The counter's extra damage, as a share.</summary>
        private const float Bonus = 0.25f;

        private static float armedUntil = float.NegativeInfinity;
        private static Attack counter;
        private static bool calledOut;

        public static float UnlockLevel => Level;
        public static float WindowSeconds => Window;
        public static float DamageBonus => Bonus;

        [HarmonyPatch(typeof(Player), nameof(Player.RPC_HitWhileDodging))]
        private static class PerfectDodge
        {
            [HarmonyPrefix]
            private static void Prefix(Player __instance, out bool __state) => __state = __instance.m_beenHitWhileDodging;

            [HarmonyPostfix]
            private static void Postfix(Player __instance, bool __state)
            {
                if (!__state && __instance.m_beenHitWhileDodging && ReferenceEquals(__instance, Player.m_localPlayer))
                    HookGuard.Run("counter armed", static () => Arm());
            }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.StartAttack))]
        private static class Start
        {
            [HarmonyPostfix]
            private static void Postfix(Humanoid __instance, bool __result)
            {
                if (__result && ReferenceEquals(__instance, Player.m_localPlayer))
                    HookGuard.Run("counter attack", static humanoid => Started(humanoid.m_currentAttack), __instance);
            }
        }

        /// <summary>A hit the local player deals to someone else, from <see cref="OutgoingHits"/>.</summary>
        public static void OnHit(Character target, HitData hit)
        {
            if (counter == null || hit.m_ranged || !DodgeSkill.Active)
                return;
            Player player = Player.m_localPlayer;
            if (player == null || player.m_currentAttack != counter)
                return;
            hit.ApplyModifier(1f + Bonus);
            if (!calledOut)
                FloatingText.Show(hit.m_point, "Counter!");
            calledOut = true;
        }

        private static void Arm()
        {
            if (DodgeSkill.Reached(Level))
                armedUntil = Time.time + Window;
        }

        private static void Started(Attack attack)
        {
            if (!IsMelee(attack))
            {
                counter = null;
                return;
            }
            counter = Time.time < armedUntil ? attack : null;
            armedUntil = float.NegativeInfinity;
            calledOut = false;
        }

        private static bool IsMelee(Attack attack) => attack != null && (attack.m_attackType == Attack.AttackType.Horizontal
            || attack.m_attackType == Attack.AttackType.Vertical || attack.m_attackType == Attack.AttackType.Area);
    }
}
