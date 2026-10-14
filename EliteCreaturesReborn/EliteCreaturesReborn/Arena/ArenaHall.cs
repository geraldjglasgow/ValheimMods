using UnityEngine;

namespace EliteCreaturesReborn.Arena
{
    /// <summary>
    /// The hall's shared face (features/arena.md section 1): where it is, whether it is built on this peer (and building
    /// it on demand), its named points, and whether a position is inside it or near it. Written by the hall (Arena/Hall), read by the trial and travel
    /// (Arena/Trial, Arena/Travel). Contract stub: the hall fills the bodies; the signatures stay.
    /// </summary>
    public static class ArenaHall
    {
        /// <summary>The arena's origin, the one place it is defined: the floor centre on the floor top, unrotated, +Z
        /// toward the gate. Due west, 10,304 m from the world's centre, 5,000 m up: over the far ocean, where the world's
        /// land has faded out (land reaches r 10,000 and fades to the abyss at 10,490; this far out it is nearly always
        /// deep water), and with everything solid (to r 77 round it: r 10,227 to 10,381) inside the game's edge at
        /// r 10,420, whose push and kill only act below y 30 and -10. On a zone's centre (-161 x 64, 0: the game's zone
        /// index is floor((x + 32) / 64), its centre index x 64), so the arena spans exactly 3 x 3 zones. Nobody walks or
        /// sails there: the only way in is a teleport. Until 3.26 it stood at (0, 5000, 0), over the world's spawn.</summary>
        public static readonly Vector3 Origin = new Vector3(-10304f, 5000f, 0f);

        /// <summary>How far round the origin counts as the arena's area: where a player makes the hall wanted on their
        /// machine (and a peer on a dedicated server), across and up or down.</summary>
        public const float AreaRadius = 1200f;
        public const float AreaHeight = 600f;

        /// <summary>True while the hall is built on this peer: the Arena setting is on and someone is in the arena's area
        /// (on a client, the local player; on a dedicated server, any peer), or was within the last minute. It is built
        /// only on demand: never for a player who does not go there.</summary>
        public static bool Built { get; internal set; }

        /// <summary>True for a position in the arena's area (<see cref="AreaRadius"/> across, <see cref="AreaHeight"/> up
        /// or down).</summary>
        public static bool Near(Vector3 position)
        {
            Vector3 local = position - Origin;
            return Mathf.Abs(local.y) < AreaHeight && local.x * local.x + local.z * local.z < AreaRadius * AreaRadius;
        }

        /// <summary>Builds the hall on this peer now, when the Arena setting is on and it is not built yet, so a jump in
        /// lands on a floor (a frame or two of work); true when it stands. Every way in calls it before the jump.</summary>
        public static bool Ensure() => Hall.HallHost.Ensure();

        /// <summary>A named point of the built arena (spawn_0..7, portal_0..7, arrive, gate_inside, flame_0..15 in the
        /// walls; seat_root, seat, spear_rest, spear_top, boss_stand, fire_0..1 in the throne), in world space. False when
        /// the arena is not built or has no such point.</summary>
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

        /// <summary>The wall's inner face: the arena's floor and kerb lie within it (CONTRACT.md).</summary>
        public const float Radius = 66f;

        /// <summary>True for a position inside the arena: within the wall's inner face (r 66), from 5 m under the floor
        /// to 40 m over it (the arena is open above its walls; a flier higher up is outside it).</summary>
        public static bool Contains(Vector3 position)
        {
            Vector3 local = position - Origin;
            return local.y > -5f && local.y < 40f && local.x * local.x + local.z * local.z < Radius * Radius;
        }
    }
}
