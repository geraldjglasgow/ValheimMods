using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The cannons' patch on the game's projectile hit (<c>Projectile.OnHit</c>, run only on the game that flies the
    /// projectile: the gunner's), acting on the cannons' own shots alone: a shot's own hit on a building piece or a ship
    /// (anything with the game's wear, <c>WearNTear</c>) is its shot's <see cref="KwShot.Structures"/> times as hard as on
    /// a creature or a player, and a shot under the water (<see cref="KwShotWater"/>) hits by the speed it has left (its
    /// damage times its share of the energy it went in with), but on a launched Broadside ship, which weighs a heavy shot
    /// by its speed at impact itself (its <see cref="KwShipHitRouter"/>). The projectile's damage is
    /// changed for that one hit and put back after it. The hit then goes the game's own way, to the piece's or ship's
    /// owner, who takes off its resistances and its health, as for any hit. The burst the shot leaves hits all alike. (A
    /// shot passing through the body and the cannon it was fired from is the MannedWeapons library's own patch on the
    /// same method, <c>WeaponShot.InstallPassThrough</c>.)
    /// </summary>
    public static class KwShotHits
    {
        // shot prefab name hash -> times as hard against structures (only shots other than 1)
        private static readonly Dictionary<int, float> Structures = new Dictionary<int, float>();

        /// <summary>A shot's projectile prefab hits structures so many times as hard.</summary>
        public static void Set(GameObject prefab, float structures)
        {
            int hash = prefab.name.GetStableHashCode();
            if (Mathf.Approximately(structures, 1f))
                Structures.Remove(hash);
            else
                Structures[hash] = structures;
        }

        // Times a cannon's shot hits structures harder; 0 for any other projectile.
        private static float Times(Projectile projectile)
        {
            ZDO? zdo = projectile.m_nview != null ? projectile.m_nview.GetZDO() : null;
            return zdo != null && Structures.Count > 0 && Structures.TryGetValue(zdo.GetPrefab(), out float times) ? times : 0f;
        }

        // The damage the projectile had before this hit changed it (boxed), or null when unchanged: raised on a structure,
        // cut under the water to the energy it has left (not on a ship that reads its speed).
        private static object? Change(Projectile projectile, Collider collider)
        {
            float times = Times(projectile);
            KwShotWater? water = projectile.GetComponent<KwShotWater>();
            bool wet = water != null && water.Under;
            if (times <= 0f && !wet)
                return null;
            GameObject? hit = Projectile.FindHitObject(collider);
            float hard = times > 0f && hit != null && hit.GetComponent<WearNTear>() != null ? times : 1f;
            float scale = hard * (wet && !ReadsSpeed(hit) ? water!.Share : 1f);
            if (Mathf.Approximately(scale, 1f))
                return null;
            HitData.DamageTypes before = projectile.m_damage;
            projectile.m_damage.Modify(scale);
            return before;
        }

        // A launched Broadside ship: its hit router weighs a heavy shot by its speed at impact itself (KwShipShot).
        private static bool ReadsSpeed(GameObject? hit) => hit != null && hit.GetComponent<KwShipHitRouter>() != null;

        [HarmonyPatch(typeof(Projectile), nameof(Projectile.OnHit))]
        private static class Hit
        {
            [HarmonyPrefix]
            private static void Prefix(Projectile __instance, Collider collider, out object? __state) =>
                __state = collider != null ? Change(__instance, collider) : null;

            [HarmonyPostfix]
            private static void Postfix(Projectile __instance, object? __state)
            {
                if (__state is HitData.DamageTypes before)
                    __instance.m_damage = before;
            }
        }
    }
}
