using HarmonyLib;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// One cast of the Rootbound Wand's roots (<see cref="WandRoots"/>), on the caster's machine, where the game fires a
    /// player's attack. As the game sets up the spawner it fired (<c>SpawnAbility.Setup</c>, read 2026-11-02: it starts
    /// raising at once, the hit the attack built handed over and dropped), the charge at the trigger
    /// (<see cref="WindUp.Thrown"/>) sets how many roots: 1 for a tap, up to <see cref="Most"/> at full charge
    /// (1 + 4 times the charge, rounded). The spawner moves to what the crosshair is on (<see cref="TipSpawn.AimPoint"/>),
    /// kept <see cref="Closest"/> to <see cref="Reach"/> m out from the caster, and the game raises them there as it raises
    /// the Elder's: one on the spot, more at random within <see cref="Spread"/> m, on the ground, a tenth of a second apart.
    /// As each comes up the game calls <c>SpawnAbility.SetupAoe</c> (right after making it; for the Elder's roots it would
    /// put an area on the creature, here there is none), where the root is handed the cast instead: the caster, the attack's
    /// hit (the wand's damage with its quality, the charge bonus and the skill) and the way it faces, away from the caster.
    /// </summary>
    public sealed class RootCast : MonoBehaviour
    {
        private const int Most = 5;
        private const float Spread = 3f;
        private const float Closest = 2f;
        private const float Reach = 30f;

        private Player caster;
        private HitData hit;
        private Quaternion facing = Quaternion.identity;

        private void Begin(SpawnAbility ability, Player player, HitData cast)
        {
            int count = 1 + Mathf.RoundToInt((Most - 1) * Mathf.Clamp01(WindUp.Thrown));
            ability.m_minToSpawn = count;
            ability.m_maxToSpawn = count;
            ability.m_spawnRadius = count > 1 ? Spread : 0f;
            ability.transform.position = Target(player, out Vector3 way);
            facing = Quaternion.LookRotation(way);
            caster = player;
            hit = cast;
        }

        /// <summary>Where the roots come up: the crosshair's point, or along it at the nearest or farthest allowed.</summary>
        private static Vector3 Target(Player player, out Vector3 way)
        {
            Vector3 from = player.transform.position;
            Vector3 aim = TipSpawn.AimPoint(player);
            Vector3 flat = Vector3.ProjectOnPlane(aim - from, Vector3.up);
            way = flat.sqrMagnitude > 0.01f ? flat.normalized : Vector3.ProjectOnPlane(player.transform.forward, Vector3.up).normalized;
            float distance = flat.magnitude;
            if (distance >= Closest && distance <= Reach)
                return aim;
            return from + way * Mathf.Clamp(distance, Closest, Reach);
        }

        private void Hand(WandRoot root)
        {
            if (root != null && caster != null)
                root.Arm(caster, hit, facing);
        }

        [HarmonyPatch(typeof(SpawnAbility), nameof(SpawnAbility.Setup))]
        private static class Fired
        {
            [HarmonyPrefix]
            private static void Prefix(SpawnAbility __instance, Character owner, HitData hitData)
            {
                RootCast cast = __instance.GetComponent<RootCast>();
                if (cast != null && owner is Player player)
                    CasterSafe.Run("roots", () => cast.Begin(__instance, player, hitData));
            }
        }

        [HarmonyPatch(typeof(SpawnAbility), nameof(SpawnAbility.SetupAoe))]
        private static class Risen
        {
            /// <summary>The wand's spawner hands the root it has just made its cast, and puts no area on it.</summary>
            [HarmonyPrefix]
            private static bool Prefix(SpawnAbility __instance)
            {
                RootCast cast = __instance.GetComponent<RootCast>();
                if (cast == null)
                    return true;
                WandRoot root = WandRoot.TakeNewest();
                CasterSafe.Run("root", () => cast.Hand(root));
                return false;
            }
        }
    }
}
