using MannedWeapons;
using UnityEngine;

namespace EliteCreaturesPack.Ballista
{
    /// <summary>
    /// A shot, on the holder's machine: the flying missile starts where the laid one lay, at its nose in the groove, owned
    /// by the holder (their hit, their Crossbows skill), as the game's ballista builds its shot from the missile's numbers
    /// (<see cref="WeaponShot"/>); it is a networked object, so every peer sees it fly. It leaves toward what the holder's
    /// crosshair is on as it leaves (<see cref="WeaponShot.Way"/>; the library has laid the ballista there from the nose,
    /// <see cref="BallistaControl.Flight"/>). The library then writes the string slack and counts the shot, which is every
    /// peer's cue for the fire sound and the arms springing forward (<see cref="BallistaLook"/>).
    /// </summary>
    public static class BallistaShot
    {
        private const float Noise = 10f;

        /// <summary>Where the flying missile starts: its nose, as the laid one lies in the groove.</summary>
        public static Vector3 Nose(BallistaParts parts) => parts.Seat.position + parts.Pitch.forward * BallistaMissile.NoseToCentre;

        /// <summary>Launches the missile; false when the missile or its flight was never built.</summary>
        public static bool Fire(BallistaControl ballista, BallistaParts parts, Player holder)
        {
            ItemDrop? missile = BallistaMissile.Item != null ? BallistaMissile.Item.GetComponent<ItemDrop>() : null;
            if (BallistaMissile.Shot == null || missile == null)
            {
                return false;
            }
            Vector3 nose = Nose(parts);
            Vector3 forward = WeaponShot.Way(ballista, holder, nose, parts.Pitch.forward, Vector3.zero);
            ItemDrop.ItemData data = missile.m_itemData;
            WeaponShot.Launch(BallistaMissile.Shot, nose, Quaternion.LookRotation(forward, parts.Pitch.up),
                forward * data.m_shared.m_attack.m_projectileVel, holder, WeaponShot.Hit(data), data, Noise);
            return true;
        }
    }
}
