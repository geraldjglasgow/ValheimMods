using HarmonyLib;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// One firebolt of the Ashwood Wand (<see cref="Firebolt"/>), on the caster's machine, where the game fires a player's
    /// attack and sets up what it fired (<c>Projectile.Setup</c>, read 2026-11-02: it takes the attack's hit, which already
    /// carries the wand's damage with its quality, the charge's +30% and the skill). After it, the charge at the trigger
    /// (<see cref="WindUp.Thrown"/>) makes the firebolt <see cref="Smallest"/> (a tap) to <see cref="Largest"/> (full charge,
    /// the staff's own fireball) times the staff's size, its hit width with it (0.1 to 0.2 m); from <see cref="Blasts"/> of a
    /// full charge on it explodes where it lands: the game's own area (<c>Projectile.DoAOE</c>, read 2026-11-02: every
    /// destructible within the radius that the projectile may hit, here the caster's enemies only, buildings skipped) of
    /// <see cref="Radius"/> m with the firebolt's own hit, and the staff's explosion (<see cref="Firebolt.Blast"/>,
    /// networked) instead of the small burst. The game runs the hit on the caster's machine (the owner), so the area and
    /// the effect set here are the ones that land. <see cref="BoltSize"/> leaves firebolts alone: their size is set here.
    /// </summary>
    public static class FireboltShot
    {
        private const float Smallest = 0.5f;
        private const float Largest = 1f;
        public const float Blasts = 0.9f;
        public const float Radius = 2f;

        [HarmonyPatch(typeof(Projectile), nameof(Projectile.Setup))]
        private static class Fire
        {
            [HarmonyPostfix]
            private static void Postfix(Projectile __instance, Character owner)
            {
                if (Fired(__instance, owner))
                    CasterSafe.Run("firebolt charge", () => Charge(__instance));
            }
        }

        /// <summary>A firebolt the local player's wand fired just now.</summary>
        private static bool Fired(Projectile projectile, Character owner) =>
            CastersSettings.On && owner != null && owner == Player.m_localPlayer && Firebolt.Is(projectile)
            && Firebolt.Bolt != null && projectile.m_nview != null;

        private static void Charge(Projectile bolt)
        {
            float charge = Mathf.Clamp01(WindUp.Thrown);
            float size = Mathf.Lerp(Smallest, Largest, charge);
            bolt.m_nview.SetLocalScale(Firebolt.Bolt.transform.localScale * size);
            bolt.m_rayRadius = Firebolt.HitWidth * size / Smallest;
            if (charge < Blasts)
                return;
            bolt.m_aoe = Radius;
            if (Firebolt.Blast.m_effectPrefabs != null && Firebolt.Blast.m_effectPrefabs.Length > 0)
                bolt.m_hitEffects = Firebolt.Blast;
        }
    }
}
