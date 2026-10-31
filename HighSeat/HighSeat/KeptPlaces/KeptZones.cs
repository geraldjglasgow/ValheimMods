using System.Collections.Generic;

namespace HighSeat
{
    /// <summary>
    /// The zones the server holds (a zone is also a sector: the game uses one 64 m grid for both). Kept: each fresh
    /// death spot's own zone, newest first (its player comes back for the tombstone within minutes), then each busy
    /// place's zone and the zones within PlaceRadius of it (3x3), busiest first; that order decides whose ground is built
    /// and whose objects are made first. A zone that stops being kept is not dropped at once: it moves to Leaving, which
    /// <see cref="KeptSectors"/> empties one zone a pass, so letting several places go never destroys all of their
    /// objects in one frame. While nothing may be kept (the server busy, the setting off) every kept zone leaves this way,
    /// the least important first. Only this class changes the two lists; the others read them.
    /// </summary>
    internal static class KeptZones
    {
        /// <summary>A busy place is its zone and the zones within this many of it.</summary>
        public const int PlaceRadius = 1;

        public static readonly List<Vector2s> Kept = new List<Vector2s>();
        public static readonly List<Vector2s> Leaving = new List<Vector2s>();
        private static readonly HashSet<Vector2s> KeptSet = new HashSet<Vector2s>();
        private static readonly List<Vector2s> Was = new List<Vector2s>();

        public static bool Holds(Vector2s zone) => KeptSet.Contains(zone) || Leaving.Contains(zone);

        public static void Reset()
        {
            Kept.Clear();
            KeptSet.Clear();
            Leaving.Clear();
        }

        /// <summary>The kept zones again, from the death spots and the busy places; those no longer kept start leaving.</summary>
        public static void Rebuild()
        {
            Was.Clear();
            Was.AddRange(Kept);
            Kept.Clear();
            KeptSet.Clear();
            for (int i = 0; i < DeathSpots.Count; i++)
            {
                Keep(DeathSpots.ZoneAt(i));
            }
            foreach (Vector2s place in BusyPlaces.Places)
            {
                KeepAround(place);
            }
            foreach (Vector2s zone in Was)
            {
                if (!KeptSet.Contains(zone))
                {
                    Leaving.Add(zone);
                }
            }
            Was.Clear();
            StopLeaving();
        }

        // The place's own zone first, then the ones round it.
        private static void KeepAround(Vector2s center)
        {
            Keep(center);
            for (int dy = -PlaceRadius; dy <= PlaceRadius; dy++)
            {
                for (int dx = -PlaceRadius; dx <= PlaceRadius; dx++)
                {
                    Keep(new Vector2s(center.x + dx, center.y + dy));
                }
            }
        }

        // A zone past the edge of the game's sector table is never kept: its index is 0, where the game files the objects
        // that lie outside every zone.
        private static void Keep(Vector2s zone)
        {
            if (ZoneSystem.SectorToIndex(zone).Sector != 0 && KeptSet.Add(zone))
            {
                Kept.Add(zone);
            }
        }

        // A leaving zone that is kept again stays, with its objects.
        private static void StopLeaving()
        {
            for (int i = Leaving.Count - 1; i >= 0; i--)
            {
                if (KeptSet.Contains(Leaving[i]))
                {
                    Leaving.RemoveAt(i);
                }
            }
        }

        /// <summary>Every kept zone starts leaving, the least important first.</summary>
        public static void LetGo()
        {
            for (int i = Kept.Count - 1; i >= 0; i--)
            {
                Leaving.Add(Kept[i]);
            }
            Kept.Clear();
            KeptSet.Clear();
        }

        /// <summary>Takes the first leaving zone off the list: from now on nothing in it is held.</summary>
        public static bool ReleaseFirst(out Vector2s zone)
        {
            if (Leaving.Count == 0)
            {
                zone = default;
                return false;
            }
            zone = Leaving[0];
            Leaving.RemoveAt(0);
            return true;
        }
    }
}
