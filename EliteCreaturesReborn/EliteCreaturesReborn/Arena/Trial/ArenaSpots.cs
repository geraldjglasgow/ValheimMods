using UnityEngine;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// Where the trial's creatures come in, read from the built hall (features/arena.md section 3, "Waves"): each archway's
    /// recess, under the portal at its <c>portal_k</c> point (the recess's back face, 2.5 m up, facing the hall; the
    /// recess is about 1.4 m deep, 4 m wide and 5 m tall, with a floor of its own), and the rune circle at the floor's
    /// centre for a boss. Positions sit on the floor the game finds there, so they hold on the dedicated server, which
    /// builds the hall's colliders too; the portals stand on the <c>portal_N</c> points themselves.
    /// </summary>
    internal static class ArenaSpots
    {
        /// <summary>Metres from the recess's back face to where a creature appears: inside the recess, clear of the wall,
        /// so it runs out into the hall.</summary>
        private const float InRecess = 0.8f;

        /// <summary>Metres over the floor a creature is made at, so it never starts in the floor.</summary>
        private const float Lift = 0.2f;

        /// <summary>A place and the way to face there.</summary>
        public readonly struct Spot
        {
            public Spot(Vector3 position, Quaternion facing)
            {
                Position = position;
                Facing = facing;
            }

            public Vector3 Position { get; }

            public Quaternion Facing { get; }
        }

        /// <summary>A creature's spot in archway <paramref name="lane"/>'s recess, facing into the hall; the rune circle
        /// when the hall has no such point.</summary>
        public static Spot Archway(int lane)
        {
            if (!BackFace(lane, out Vector3 back, out Quaternion facing))
            {
                return RuneCircle();
            }
            return new Spot(OnFloor(back + facing * Vector3.forward * InRecess), facing);
        }

        /// <summary>The rune circle at the floor's centre, facing the gate: a boss's spot (the throne's own boss_stand is up
        /// on the dais).</summary>
        public static Spot RuneCircle() => new Spot(OnFloor(ArenaHall.Origin + Vector3.up), Quaternion.identity);

        /// <summary>Archway <paramref name="lane"/>'s portal anchor: the walls' <c>portal_N</c> point itself, on the
        /// recess's back face 2.5 m up, facing the hall. False when the hall has no such point.</summary>
        public static bool PortalAnchor(int lane, out Spot anchor)
        {
            bool known = BackFace(lane, out Vector3 point, out Quaternion facing);
            anchor = new Spot(point, facing);
            return known;
        }

        private static bool BackFace(int lane, out Vector3 point, out Quaternion facing)
        {
            facing = Quaternion.identity;
            point = Vector3.zero;
            return lane >= 0 && lane < ArenaKeys.PortalPoints.Count
                && ArenaHall.TryPoint(ArenaKeys.PortalPoints[lane], out point, out facing);
        }

        // On the floor found under the point (the game's own floor test, the hall's colliders being solid), a little up.
        private static Vector3 OnFloor(Vector3 at)
        {
            if (ZoneSystem.instance != null && ZoneSystem.instance.FindFloor(at, out float floor))
            {
                at.y = floor;
            }
            at.y += Lift;
            return at;
        }
    }
}
