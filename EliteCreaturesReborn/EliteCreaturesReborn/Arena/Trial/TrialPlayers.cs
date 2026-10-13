using System;
using System.Collections.Generic;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// The players of the trial: everyone who was in the hall as Hel woke and everyone who came in while it ran
    /// (features/arena.md section 3, "Start"), by their peers, kept in the core's ZDO (<see cref="ArenaKeys.Players"/>) so
    /// its messages reach them all - the fallen and those who left included - whoever owns the core at the end. Written
    /// only when a new player joins; read every step, so the decoded list is kept against the array it came from.
    /// </summary>
    internal static class TrialPlayers
    {
        private static readonly int Key = ArenaKeys.Players.GetStableHashCode();
        private static readonly List<long> Peers = new List<long>();
        private static byte[]? _readFrom;

        /// <summary>The trial's players, in a list reused by the next call. Read only.</summary>
        public static List<long> All(ZDO core)
        {
            Load(core);
            return Peers;
        }

        /// <summary>A new trial: only the players in the hall now. Core's owner only.</summary>
        public static void Reset(ZDO core, List<long> inHall)
        {
            Peers.Clear();
            Peers.AddRange(inHall);
            Save(core);
        }

        /// <summary>The players in the hall now join the trial; the list is written only when one is new.</summary>
        public static void Join(ZDO core, List<long> inHall)
        {
            Load(core);
            bool changed = false;
            foreach (long peer in inHall)
            {
                if (!Peers.Contains(peer))
                {
                    Peers.Add(peer);
                    changed = true;
                }
            }
            if (changed)
            {
                Save(core);
            }
        }

        private static void Save(ZDO core)
        {
            ZPackage pkg = new ZPackage();
            pkg.Write(Peers.Count);
            foreach (long peer in Peers)
            {
                pkg.Write(peer);
            }
            byte[] bytes = pkg.GetArray();
            core.Set(Key, bytes);
            _readFrom = bytes;
        }

        private static void Load(ZDO core)
        {
            byte[]? bytes = core.GetByteArray(Key);
            if (ReferenceEquals(bytes, _readFrom))
            {
                return;
            }
            _readFrom = bytes;
            Peers.Clear();
            if (bytes != null)
            {
                Decode(bytes);
            }
        }

        private static void Decode(byte[] bytes)
        {
            try
            {
                ZPackage pkg = new ZPackage(bytes);
                for (int n = pkg.ReadInt(); n > 0; n--)
                {
                    Peers.Add(pkg.ReadLong());
                }
            }
            catch (Exception)
            {
                Peers.Clear(); // a damaged list names nobody rather than failing every step
            }
        }
    }
}
