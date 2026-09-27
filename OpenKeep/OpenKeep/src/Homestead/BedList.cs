using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// Remembers and forgets the local character's beds. A change made while no local player exists (the respawn
    /// search after death, the loading before the first spawn: the game has destroyed or not yet created the player
    /// whose custom data holds the list) waits here with its scope and is applied in <c>Player.OnSpawned</c>, after
    /// the game has loaded the new player's custom data. The last change for a point wins. A change that waits when
    /// the game quits is lost; the respawn search and the next load of the bed find it again.
    /// </summary>
    public static class BedList
    {
        private struct Change
        {
            public string Scope;
            public Vector3 Point;
            public bool Add;
        }

        private static readonly List<Change> pending = new List<Change>();

        /// <summary>The known beds in this world; empty without a local player.</summary>
        public static List<Vector3> Known()
        {
            return BedStore.Writable(BedStore.Scope()) ? BedStore.Read() : new List<Vector3>();
        }

        public static void Remember(Vector3 point) => Record(point, true);

        public static void Forget(Vector3 point) => Record(point, false);

        /// <summary>The local player spawned: applies the waiting changes of this character and world, drops the rest.</summary>
        public static void Flush()
        {
            string scope = BedStore.Scope();
            List<Change> changes = new List<Change>(pending);
            pending.Clear();
            if (!BedStore.Writable(scope))
                return;
            foreach (Change change in changes)
            {
                if (change.Scope == scope)
                    Apply(change.Point, change.Add);
            }
        }

        private static void Record(Vector3 point, bool add)
        {
            string scope = BedStore.Scope();
            if (scope == null)
                return;
            if (BedStore.Writable(scope))
            {
                Apply(point, add);
                return;
            }
            pending.RemoveAll(c => c.Scope == scope && BedPoints.Same(c.Point, point));
            pending.Add(new Change { Scope = scope, Point = point, Add = add });
        }

        private static void Apply(Vector3 point, bool add)
        {
            List<Vector3> beds = BedStore.Read();
            if (add == BedPoints.Contains(beds, point))
                return;
            if (add)
                beds.Add(point);
            else
                beds.RemoveAll(p => BedPoints.Same(p, point));
            BedStore.Write(beds);
            string verb = add ? "remembered" : "forgotten";
            Plugin.Log.LogInfo($"OpenKeep: bed at {BedPoints.Format(point)} {verb}, {beds.Count} known in this world");
        }
    }
}
