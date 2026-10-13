using UnityEngine;

namespace EliteCreaturesReborn.Arena
{
    /// <summary>
    /// The hall's shared face (features/arena.md section 1): where it is, whether it is built on this peer, its named
    /// points, and whether a position is inside it. Written by the hall (Arena/Hall), read by the trial and travel
    /// (Arena/Trial, Arena/Travel). Contract stub: the hall fills the bodies; the signatures stay.
    /// </summary>
    public static class ArenaHall
    {
        /// <summary>The hall's origin: the floor centre on the floor top, high above the world centre. Unrotated; hall
        /// +Z faces the gate.</summary>
        public static readonly Vector3 Origin = new Vector3(0f, 5000f, 0f);

        /// <summary>True while the hall is built on this peer (the Arena setting is on and the world has loaded).</summary>
        public static bool Built { get; internal set; }

        /// <summary>A named point of the built hall (spawn_0..3, portal_0..3, arrive, gate_inside, flame_0..7, seat_root,
        /// seat, spear_rest, spear_top, boss_stand, fire_0..1, light_oculus), in world space. False when the hall is not
        /// built or has no such point.</summary>
        public static bool TryPoint(string name, out Vector3 position, out Quaternion rotation)
        {
            if (Built && Hall.HallPoints.TryGet(name, out Transform point))
            {
                position = point.position;
                rotation = point.rotation;
                return true;
            }
            position = Vector3.zero;
            rotation = Quaternion.identity;
            return false;
        }

        /// <summary>True for a position inside the hall: within the wall ring, from the floor up to the dome.</summary>
        public static bool Contains(Vector3 position)
        {
            Vector3 local = position - Origin;
            return local.y > -5f && local.y < 30f && new Vector2(local.x, local.z).sqrMagnitude < 27.5f * 27.5f;
        }
    }
}
