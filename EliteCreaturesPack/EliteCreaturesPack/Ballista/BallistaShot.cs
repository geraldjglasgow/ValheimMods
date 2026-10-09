using UnityEngine;

namespace EliteCreaturesPack.Ballista
{
    /// <summary>
    /// A shot, on the holder's machine: the flying missile starts where the laid one lay, along the groove, owned by
    /// the holder (their hit, their Crossbows skill), as the game's ballista builds its shot from the missile's numbers;
    /// it is a networked object, so every peer sees it fly. The ZDO then says the string is slack and counts the shot,
    /// which is every peer's cue for the fire sound and the arms springing forward (<see cref="BallistaLook"/>).
    /// </summary>
    public static class BallistaShot
    {
        private const float Noise = 10f;

        public static void Fire(BallistaControl c, Player p)
        {
            ItemDrop? missile = BallistaMissile.Item != null ? BallistaMissile.Item.GetComponent<ItemDrop>() : null;
            if (BallistaMissile.Shot == null || missile == null || c.Parts == null)
            {
                return;
            }
            Vector3 forward = c.Parts.Pitch.forward;
            Vector3 nose = c.Parts.Seat.position + forward * BallistaMissile.NoseToCentre;
            GameObject shot = Object.Instantiate(BallistaMissile.Shot, nose, Quaternion.LookRotation(forward, c.Parts.Pitch.up));
            ItemDrop.ItemData data = missile.m_itemData;
            shot.GetComponent<IProjectile>()?.Setup(p, forward * data.m_shared.m_attack.m_projectileVel, Noise, Hit(data), null, data);
            ZDO zdo = c.Net.GetZDO();
            zdo.Set(BallistaKeys.Spring, (int)Spring.Slack);
            zdo.Set(BallistaKeys.Act, (int)Act.None);
            zdo.Set(BallistaKeys.Shots, zdo.GetInt(BallistaKeys.Shots) + 1);
            zdo.Set(BallistaKeys.ShotAt, BallistaKeys.Now());
        }

        private static HitData Hit(ItemDrop.ItemData missile)
        {
            ItemDrop.ItemData.SharedData shared = missile.m_shared;
            var hit = new HitData
            {
                m_toolTier = (short)shared.m_toolTier,
                m_pushForce = shared.m_attackForce,
                m_backstabBonus = shared.m_backstabBonus,
                m_staggerMultiplier = shared.m_attack.m_staggerMultiplier,
                m_blockable = shared.m_blockable,
                m_dodgeable = shared.m_dodgeable,
                m_skill = shared.m_skillType,
                m_hitType = HitData.HitType.PlayerHit,
                m_itemWorldLevel = (byte)Game.m_worldLevel,
            };
            hit.m_damage.Add(missile.GetDamage());
            return hit;
        }
    }
}
