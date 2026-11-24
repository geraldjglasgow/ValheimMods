using HarmonyLib;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// One cast of the Frostsilver Wand's ice shards (<see cref="IceShards"/>), on the caster's machine, where the game fires a
    /// player's attack. As the game fires the attack's projectiles (<c>Attack.FireProjectileBurst</c>, read 2026-11-02: for
    /// each of <c>m_projectiles</c> it makes the projectile at the spawn point, builds the hit from the weapon, its quality,
    /// the attack's multiplier and the skill, and sets it up), the charge at the trigger (<see cref="WindUp.Thrown"/>) sets
    /// how many: 1 for a tap, up to <see cref="Most"/> at full charge (1 + 4 times the charge, rounded). Each leaves the
    /// wand's tip toward the crosshair (<see cref="TipSpawn"/>); as the game sets each up (<c>Projectile.Setup</c>) it is
    /// turned into an even horizontal fan <see cref="Gap"/> degrees apart, centred on the aim (one alone flies straight at
    /// it). Each carries the whole hit the game built for it, so each deals the wand's bolt damage and raises Elemental
    /// Magic when it hits someone, as a bolt does; the eitr is the attack's, paid once a cast however many fly.
    /// </summary>
    public static class IceVolley
    {
        private const int Most = 5;
        private const float Gap = 8f;

        private static int count;
        private static int next;

        /// <summary>The local player's wand attack fires the shards: how many, from the charge it was thrown with.</summary>
        private static void Open(Attack attack)
        {
            if (!CastersSettings.On || IceShards.Shard == null || attack.m_attackProjectile != IceShards.Shard
                || attack.m_character == null || attack.m_character != Player.m_localPlayer)
                return;
            attack.m_projectiles = 1 + Mathf.RoundToInt((Most - 1) * Mathf.Clamp01(WindUp.Thrown));
            count = attack.m_projectiles;
            next = 0;
        }

        private static void Close()
        {
            count = 0;
            next = 0;
        }

        /// <summary>The next shard of the open volley turned to its place in the fan.</summary>
        private static void Turn(Projectile shard, ref Vector3 velocity)
        {
            float angle = (next - (count - 1) * 0.5f) * Gap;
            next++;
            velocity = Quaternion.AngleAxis(angle, Vector3.up) * velocity;
            if (velocity.sqrMagnitude > 0.0001f)
                shard.transform.rotation = Quaternion.LookRotation(velocity);
        }

        [HarmonyPatch(typeof(Attack), nameof(Attack.FireProjectileBurst))]
        private static class Burst
        {
            [HarmonyPrefix]
            private static void Prefix(Attack __instance) => CasterSafe.Run("ice shards", () => Open(__instance));

            /// <summary>The volley closes as the burst ends, however it ends.</summary>
            [HarmonyFinalizer]
            private static void Finalizer() => Close();
        }

        [HarmonyPatch(typeof(Projectile), nameof(Projectile.Setup))]
        private static class Fan
        {
            [HarmonyPrefix]
            private static void Prefix(Projectile __instance, ref Vector3 velocity)
            {
                if (count <= 1 || next >= count || !IceShards.Is(__instance))
                    return;
                Turn(__instance, ref velocity);
            }
        }
    }
}
