using System;
using System.Collections.Generic;
using EliteCreaturesReborn.Util;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// "Ready" (features/arena.md section 3, "Ready"): during a break, or the countdown before wave 1, a player of the trial
    /// alive in the arena presses F (<see cref="ArenaReadyKey"/>) and the core's owner adds them to the break's ready set
    /// in the core's ZDO (<see cref="ArenaKeys.Ready"/>), or takes them out again on a second press. When every player of
    /// the trial alive in the arena at that moment is in it - the dead, the respawning and those away do not count and do
    /// not hold it up - the break ends at once and the next wave comes; the set is emptied as every wave begins. Each vote
    /// tells the trial's players "Ready: 2 of 3", the last one "All ready - the dead come!". The set is read every frame
    /// by the HUD line during a break, so the decoded set is kept against the very array it came from.
    /// </summary>
    internal static class ArenaReady
    {
        private static readonly int Key = ArenaKeys.Ready.GetStableHashCode();
        private static readonly HashSet<long> Set = new HashSet<long>();
        private static byte[]? _readFrom;

        /// <summary>A player's vote, on the core's owner: taken during the countdown or a break from a player alive in the
        /// arena, turned over (in, or out again), told to the trial's players; the wave comes when all are in.</summary>
        public static void Toggle(ArenaCore core, long voter)
        {
            ZDO? zdo = core.Zdo;
            List<long> inHall = HallPlayers.PeersNow();
            if (zdo == null || !Resting(zdo) || !inHall.Contains(voter))
            {
                return;
            }
            Load(zdo);
            if (!Set.Remove(voter))
            {
                Set.Add(voter);
            }
            Save(zdo);
            if (!GoIfAll(core, inHall))
            {
                ArenaNet.Tell(zdo, ArenaWords.ReadyVote, Count(zdo, inHall).ToString(), inHall.Count.ToString());
            }
        }

        /// <summary>The owner's step during a rest: when every player alive in the arena is ready, the wait ends now.
        /// True when it did (and the trial's players read so).</summary>
        public static bool GoIfAll(ArenaCore core, List<long> inHall)
        {
            ZDO? zdo = core.Zdo;
            if (zdo == null || inHall.Count == 0 || Count(zdo, inHall) < inHall.Count)
            {
                return false;
            }
            ArenaState state = core.State;
            state.Next = NetTime.NowMs();
            ArenaNet.Tell(zdo, ArenaWords.AllReady);
            return true;
        }

        /// <summary>A wave begins (or a trial): nobody is ready for the next break yet. Core's owner only.</summary>
        public static void Clear(ZDO zdo)
        {
            Load(zdo);
            if (Set.Count > 0 || zdo.GetByteArray(Key) == null)
            {
                Set.Clear();
                Save(zdo);
            }
        }

        /// <summary>The players among <paramref name="inHall"/> who are ready.</summary>
        public static int Count(ZDO zdo, List<long> inHall)
        {
            Load(zdo);
            int ready = 0;
            for (int i = 0; i < inHall.Count; i++)
            {
                ready += Set.Contains(inHall[i]) ? 1 : 0;
            }
            return ready;
        }

        /// <summary>True when this peer has said it is ready.</summary>
        public static bool Has(ZDO zdo, long peer)
        {
            Load(zdo);
            return Set.Contains(peer);
        }

        public static bool Resting(ZDO zdo)
        {
            ArenaPhase phase = new ArenaState(zdo).Phase;
            return phase == ArenaPhase.Countdown || phase == ArenaPhase.Break;
        }

        private static void Save(ZDO zdo)
        {
            ZPackage pkg = new ZPackage();
            pkg.Write(Set.Count);
            foreach (long peer in Set)
            {
                pkg.Write(peer);
            }
            byte[] bytes = pkg.GetArray();
            zdo.Set(Key, bytes);
            _readFrom = bytes;
        }

        private static void Load(ZDO zdo)
        {
            byte[]? bytes = zdo.GetByteArray(Key);
            if (ReferenceEquals(bytes, _readFrom))
            {
                return;
            }
            _readFrom = bytes;
            Set.Clear();
            try
            {
                ZPackage? pkg = bytes != null ? new ZPackage(bytes) : null;
                for (int n = pkg != null ? pkg.ReadInt() : 0; n > 0; n--)
                {
                    Set.Add(pkg!.ReadLong());
                }
            }
            catch (Exception)
            {
                Set.Clear(); // a damaged set counts nobody ready rather than failing every frame
            }
        }
    }
}
