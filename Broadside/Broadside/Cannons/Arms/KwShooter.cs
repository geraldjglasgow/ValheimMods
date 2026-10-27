using MannedWeapons;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Launches a shot's projectiles from a cannon's muzzle, on the gunner's own game, the way the game's ballista launches
    /// its bolts (<see cref="WeaponShot.Launch"/>): each a networked copy of the shot's prefab (<see cref="KwShots"/>) flown
    /// by this peer and seen by every other through its ZDO, carried on by the speed of the ship the cannon rides (its
    /// body), passing through that ship and the cannon itself, with the gunner as the
    /// attacker (their hit, their skill rises, their friends spared) and the shot's damage, push, tool tier and status
    /// effect. Its way is worked out as it leaves the muzzle (<see cref="WeaponShot.Way"/>): onto what the gunner's
    /// crosshair was on as he pulled, the ball's drop and the ship's motion allowed for, so a deck rolling or turning during
    /// the lanyard's pull, or his step aside from the recoil, does not carry it off; a shot the captain called
    /// leaves where the barrel points. Each projectile is then scattered
    /// evenly within the shot's <see cref="KwShot.Spread"/> round that way (<see cref="WeaponShot.Scatter"/>), drawn here
    /// and flown here, so every screen sees the same flight.
    /// </summary>
    public static class KwShooter
    {
        private const float Noise = 30f;

        /// <summary>Launches one pull's projectiles from the muzzle, each scattered round the way it leaves on; false when the shot has no prefab.</summary>
        public static bool Fire(KwShot shot, KwArm arm, Character gunner, ItemDrop.ItemData? ammo)
        {
            GameObject? prefab = KwShots.Prefab(shot);
            if (prefab == null)
                return false;
            Transform muzzle = arm.Muzzle;
            Rigidbody? carrier = arm.Carrier;
            Vector3 carried = carrier != null ? carrier.GetPointVelocity(muzzle.position) : Vector3.zero;
            Vector3 aimed = gunner is Player player ? WeaponShot.Way(arm, player, muzzle.position, muzzle.forward, carried) : muzzle.forward;
            for (int i = 0; i < Mathf.Max(1, shot.Count); i++)
            {
                Vector3 way = WeaponShot.Scatter(aimed, shot.Spread);
                WeaponShot.Launch(prefab, muzzle.position, Quaternion.LookRotation(way), way * shot.Speed + carried, gunner, Hit(shot), ammo, Noise, carrier, arm.transform);
            }
            return true;
        }

        /// <summary>A shot's hit, one per projectile (a projectile keeps the one it is given).</summary>
        public static HitData Hit(KwShot shot)
        {
            HitData hit = new HitData
            {
                m_damage = shot.Damage,
                m_pushForce = shot.Push,
                m_staggerMultiplier = shot.Stagger,
                m_toolTier = (short)shot.ToolTier,
                m_skill = shot.Skill,
                m_statusEffectHash = string.IsNullOrEmpty(shot.Status) ? 0 : shot.Status.GetStableHashCode(),
                m_hitType = HitData.HitType.PlayerHit,
                m_itemWorldLevel = (byte)Game.m_worldLevel,
            };
            return hit;
        }
    }
}
