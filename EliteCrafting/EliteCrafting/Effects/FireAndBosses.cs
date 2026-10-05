using HarmonyLib;
using UnityEngine;

namespace EliteCrafting.Effects
{
    // Damage taken by the local player from bosses and fire, on that player's own client (the game resolves a player's
    // incoming hits, its status effects and its burning ticks on the machine that owns the player). Nothing is sent.

    /// <summary>
    /// Forsaken Ward (<c>boss_damage_taken</c>): a hit whose attacker is a boss (Character.m_boss: its weapons, its
    /// projectiles and its area attacks all carry it) deals X% less, scaled before the game's own pipeline like the
    /// other percent resists (<see cref="IncomingHits"/>), so it multiplies with blocking, resistances and armor.
    /// </summary>
    [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
    internal static class BossDamageTakenPatch
    {
        private static void Prefix(Character __instance, HitData hit)
        {
            float less = AggregateHost.Current[EffectKind.BossDamageTaken];
            if (less <= 0f || !hit.HaveAttacker() || !ReferenceEquals(__instance, Player.m_localPlayer))
            {
                return;
            }
            Character attacker = hit.GetAttacker();
            if (attacker != null && attacker.IsBoss())
            {
                hit.m_damage.Modify(Mathf.Max(0f, 1f - less));
            }
        }
    }

    /// <summary>
    /// Ember Skin (<c>burning_taken</c>): each Burning tick (SE_Burning calls ApplyDamage directly) and each lava hit
    /// (HitType.AshlandsLava, through RPC_Damage) is X% lower. Ahead of the Runic Ward's ApplyDamage prefix, so the ward
    /// absorbs the reduced amount.
    /// </summary>
    [HarmonyPatch(typeof(Character), nameof(Character.ApplyDamage))]
    internal static class BurningTakenPatch
    {
        [HarmonyPriority(Priority.High)]
        private static void Prefix(Character __instance, HitData hit)
        {
            float less = AggregateHost.Current[EffectKind.BurningTaken];
            if (less <= 0f || !ReferenceEquals(__instance, Player.m_localPlayer))
            {
                return;
            }
            if (hit.m_hitType == HitData.HitType.Burning || hit.m_hitType == HitData.HitType.AshlandsLava)
            {
                hit.m_damage.Modify(Mathf.Max(0f, 1f - less));
            }
        }
    }

    /// <summary>
    /// Quench (<c>burning_decay</c>): Burning on the local player ends X% sooner. Its clock runs 1/(1 - X) as fast (the
    /// Purity pattern, <see cref="DebuffDecayPatch"/>, which adds its own share), so it lasts (1 - X) of its time and
    /// the fire it still held is never dealt. X is held below 90% whatever the cap says.
    /// </summary>
    [HarmonyPatch(typeof(StatusEffect), nameof(StatusEffect.UpdateStatusEffect))]
    internal static class BurningDecayPatch
    {
        private const float MostSooner = 0.9f;

        private static void Postfix(StatusEffect __instance, float dt)
        {
            float sooner = AggregateHost.Current[EffectKind.BurningDecay];
            if (sooner <= 0f || !(__instance is SE_Burning) || !ReferenceEquals(__instance.m_character, Player.m_localPlayer))
            {
                return;
            }
            sooner = Mathf.Min(sooner, MostSooner);
            __instance.m_time += dt * sooner / (1f - sooner);
        }
    }
}
