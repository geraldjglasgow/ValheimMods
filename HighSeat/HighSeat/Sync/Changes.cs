using System.Collections.Generic;

namespace HighSeat
{
    /// <summary>
    /// The objects changed on the server since the last send round, as ids (ZDOs are pooled and reused, so no reference
    /// is kept): filled by <see cref="ChangeHooks"/> while the send loop runs and players are on, and handed out at the
    /// start of each round to the players whose area holds the object's sector (<see cref="Interest"/>), as
    /// ZDOMan.FindSectorObjects would find it there: every object to those whose near area holds the sector, the ones
    /// flagged Distant also to those whose distant ring does. The sector is the one the game files the object under
    /// (ZDO.GetSectorIndex, the index of ZDOMan.m_objectsBySector). An object nobody watches is dropped: a player who
    /// comes near later gets it from the full check of the sectors their area takes in.
    /// </summary>
    internal static class Changes
    {
        /// <summary>Ids of the objects changed since the last round started.</summary>
        private static readonly HashSet<ZDOID> Changed = new HashSet<ZDOID>();

        /// <summary>True while the hooks record: the send loop runs on this server and at least one player is on.</summary>
        public static bool Recording;

        // The last id recorded: the game writes one object several times in a row (a move sets position, rotation and
        // velocity), so a repeat costs one compare instead of a set lookup. Forgotten whenever the set is emptied.
        private static ZDOID _last = ZDOID.None;

        /// <summary>From the hooks, only while <see cref="Recording"/>.</summary>
        public static void Record(ZDOID id)
        {
            if (id != _last)
            {
                _last = id;
                Changed.Add(id);
            }
        }

        /// <summary>Records while players are on; with nobody on there is nobody to send to, so nothing is kept.</summary>
        public static void Watch(bool playersOn)
        {
            if (!playersOn && Changed.Count > 0)
            {
                Forget();
            }
            Recording = playersOn;
        }

        public static void Reset()
        {
            Recording = false;
            Forget();
        }

        private static void Forget()
        {
            Changed.Clear();
            _last = ZDOID.None;
        }

        /// <summary>Hands every object changed since the last round to the players who could be sent it, then forgets them.</summary>
        public static void HandOut(ZDOMan man)
        {
            foreach (ZDOID id in Changed)
            {
                ZDO zdo = man.GetZDO(id);
                if (zdo != null)
                {
                    HandOut(zdo, id);
                }
            }
            Forget();
        }

        private static void HandOut(ZDO zdo, ZDOID id)
        {
            List<Interest.Watch> watchers = Interest.At(zdo.GetSectorIndex().Sector);
            if (watchers == null)
            {
                return;
            }
            bool distant = zdo.Distant;
            for (int i = 0; i < watchers.Count; i++)
            {
                Interest.Watch watch = watchers[i];
                if (watch.Near || distant)
                {
                    watch.Peer.Pending.Add(id);
                }
            }
        }
    }
}
