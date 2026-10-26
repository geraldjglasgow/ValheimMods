using MannedWeapons;
using UnityEngine;

namespace EliteCreaturesPack.Ballista
{
    /// <summary>
    /// A shot, on the holder's machine: the flying missile starts where the laid one lay, along the groove, owned by the
    /// holder (their hit, their Crossbows skill), as the game's ballista builds its shot from the missile's numbers
    /// (<see cref="WeaponShot"/>); it is a networked object, so every peer sees it fly. The library then writes the string
    /// slack and counts the shot, which is every peer's cue for the fire sound and the arms springing forward
    /// (<see cref="BallistaLook"/>).
    /// </summary>
    public static class BallistaShot
    {
        private const float Noise = 10f;

        /// <summary>Launches the missile; false when the missile or its flight was never built.</summary>
        public static bool Fire(BallistaParts parts, Player holder)
        {
            ItemDrop? missile = BallistaMissile.Item != null ? BallistaMissile.Item.GetComponent<ItemDrop>() : null;
            if (BallistaMissile.Shot == null || missile == null)
            {
                return false;
            }
            Vector3 forward = parts.Pitch.forward;
            Vector3 nose = parts.Seat.position + forward * BallistaMissile.NoseToCentre;
            ItemDrop.ItemData data = missile.m_itemData;
            WeaponShot.Launch(BallistaMissile.Shot, nose, Quaternion.LookRotation(forward, parts.Pitch.up),
                forward * data.m_shared.m_attack.m_projectileVel, holder, WeaponShot.Hit(data), data, Noise);
            return true;
        }
    }
}
