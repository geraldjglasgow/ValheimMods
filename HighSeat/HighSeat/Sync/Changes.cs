using System.Collections.Generic;
using System.Diagnostics;

namespace HighSeat
{
    /// <summary>
    /// The objects changed on the server since the last send round, as ids (ZDOs are pooled and reused, so no reference
    /// is kept): filled by <see cref="ChangeHooks"/> while the send loop runs and players are on. At a round's start what
    /// was recorded is swapped out (<see cref="ChangeSet"/>; what changes from then on goes to the next round) and handed
    /// out within the loop's frame time, as many as fit each frame, before players are served (all at once it took 2.3 ms
    /// in one frame with 100 players in a crowd, 10,000 additions): each to the players whose area holds the object's
    /// sector (<see cref="Interest"/>), as ZDOMan.FindSectorObjects would find it there: every object to those whose near
    /// area holds the sector, the ones flagged Distant also to those whose distant ring does. A player served before their
    /// share arrived gets it next round, as it stays pending until sent; a hand-out that outlasts the round (a burst) holds
    /// the next round's start back until it is done. The sector is the one the game files the object under
    /// (ZDO.GetSectorIndex, the index of ZDOMan.m_objectsBySector). An object nobody watches is dropped: a player who comes
    /// near later gets it from the full check of the sectors their area takes in. Objects whose owner changed or that
    /// moved to another sector are also recorded apart and handed out first, to let go of the holds the update rate put
    /// on them for the players watching them (<see cref="Holds"/>): an owner change never waits, and an object the game
    /// forgot for a player it moved away from is new to them again. The clock is read every <see cref="ClockWork"/> ids
    /// and players handed to.
    /// </summary>
    internal static class Changes
    {
        private const int ClockWork = 256;

        private static readonly ChangeSet Changed = new ChangeSet();
        private static readonly ChangeSet Urgent = new ChangeSet();

        /// <summary>True while the hooks record: the send loop runs on this server and at least one player is on.</summary>
        public static bool Recording;

        /// <summary>True while ids of the last round are still to be handed out.</summary>
        public static bool HandingOut => Urgent.Left || Changed.Left;

        /// <summary>From the hooks, only while <see cref="Recording"/>.</summary>
        public static void Record(ZDOID id) => Changed.Add(id);

        /// <summary>An owner change or a move to another sector, from the hooks, only while <see cref="Recording"/>.</summary>
        public static void RecordUrgent(ZDOID id) => Urgent.Add(id);

        /// <summary>Records while players are on; with nobody on there is nobody to send to, so nothing is kept.</summary>
        public static void Watch(bool playersOn)
        {
            if (!playersOn)
            {
                Changed.Forget();
                Urgent.Forget();
            }
            Recording = playersOn;
        }

        public static void Reset()
        {
            Recording = false;
            Changed.Clear();
            Urgent.Clear();
        }

        /// <summary>A round starts: what was recorded is to be handed out (only once the last round's is).</summary>
        public static void Swap()
        {
            Urgent.Swap();
            Changed.Swap();
        }

        /// <summary>Hands out what is left until the clock reaches <paramref name="until"/>; true once all is handed out.</summary>
        public static bool HandOut(ZDOMan man, Stopwatch clock, long until)
        {
            int work = 0;
            while (Urgent.Next(out ZDOID id))
            {
                if (Spent(ref work, Release(man, id), clock, until))
                {
                    return false;
                }
            }
            while (Changed.Next(out ZDOID id))
            {
                if (Spent(ref work, Give(man, id), clock, until))
                {
                    return false;
                }
            }
            return true;
        }

        // Adds up the work done and reads the clock once enough has been done since it last was.
        private static bool Spent(ref int work, int done, Stopwatch clock, long until)
        {
            work += done;
            if (work < ClockWork)
            {
                return false;
            }
            work = 0;
            return clock.ElapsedTicks >= until;
        }

        // The players watching an object let go of its hold. Returns the work done: one, and one per player.
        private static int Release(ZDOMan man, ZDOID id)
        {
            List<Interest.Watch> watchers = WatchersOf(man, id);
            if (watchers == null)
            {
                return 1;
            }
            for (int i = 0; i < watchers.Count; i++)
            {
                watchers[i].Peer.Holds.Release(id);
            }
            return 1 + watchers.Count;
        }

        // The players who could be sent an object get it pending. Returns the work done: one, and one per player.
        private static int Give(ZDOMan man, ZDOID id)
        {
            ZDO zdo = man.GetZDO(id);
            List<Interest.Watch> watchers = zdo == null ? null : Interest.At(zdo.GetSectorIndex().Sector);
            if (watchers == null)
            {
                return 1;
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
            return 1 + watchers.Count;
        }

        private static List<Interest.Watch> WatchersOf(ZDOMan man, ZDOID id)
        {
            ZDO zdo = man.GetZDO(id);
            return zdo == null ? null : Interest.At(zdo.GetSectorIndex().Sector);
        }
    }
}
