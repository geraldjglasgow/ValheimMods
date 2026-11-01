using System.Collections.Generic;

namespace HighSeatPlus
{
    /// <summary>
    /// Which revisions of each object this game holds exactly as the server sent them. A delta is only right for a copy
    /// that holds every value of the record it starts from, and equal revisions alone do not prove it: this game's own
    /// write to an object it does not own raises its revision without the server's data (ZDO.IncreaseDataRevision on a
    /// client), and the server books a player at the revisions the player itself sent (ZDOMan.RPC_ZDOData), which can
    /// hold other values at those revisions than the server's own copy. So the revisions are noted here only when a
    /// record from the server's delta package was applied by the game: before ZDOMan.RPC_ZDOData runs, what it will do
    /// with each record is worked out from the local copy (<see cref="Predict"/>), as its own tests decide: a missing
    /// object or a newer data revision takes the record's data (noted), a newer owner revision alone takes only the
    /// owner (noted only when the copy was the server's at that data revision), anything else is skipped (unchanged).
    /// A delta is used only when the local copy and this note both stand at the revisions it starts from
    /// (<see cref="Holds"/>). An object destroyed here leaves the book; the book is emptied with every new connection and
    /// when the world closes. One entry per object received, as the game's own table of objects keeps them all.
    /// </summary>
    internal static class HeldCopies
    {
        public enum Outcome : byte
        {
            Data,
            Owner,
            Skip,
        }

        public struct Expect
        {
            public ZDOID Id;
            public uint Data;
            public ushort Owner;
            public Outcome What;
            public uint LocalData;
            public ushort LocalOwner;
        }

        private struct Revisions
        {
            public uint Data;
            public ushort Owner;
        }

        private static readonly Dictionary<ZDOID, Revisions> Held = new Dictionary<ZDOID, Revisions>();

        public static int Count => Held.Count;

        /// <summary>True when the local copy is the server's record at exactly these revisions, untouched since.</summary>
        public static bool Holds(ZDO zdo, uint data, ushort owner)
        {
            return zdo.DataRevision == data && zdo.OwnerRevision == owner && Held.TryGetValue(zdo.m_uid, out Revisions held)
                && held.Data == data && held.Owner == owner;
        }

        /// <summary>What the game's ZDOMan.RPC_ZDOData will do with a record of these revisions, worked out before it runs.</summary>
        public static Expect Predict(ZDOMan man, ZDOID id, uint data, ushort owner)
        {
            ZDO zdo = man.GetZDO(id);
            Expect expect = new Expect { Id = id, Data = data, Owner = owner, What = Outcome.Skip };
            if (zdo == null || data > zdo.DataRevision)
            {
                expect.What = Outcome.Data;
            }
            else if (owner > zdo.OwnerRevision)
            {
                expect.What = Outcome.Owner;
                expect.LocalData = zdo.DataRevision;
                expect.LocalOwner = zdo.OwnerRevision;
            }
            return expect;
        }

        /// <summary>Notes what the game did with the record, once RPC_ZDOData has run.</summary>
        public static void Settle(Expect expect)
        {
            if (expect.What == Outcome.Data)
            {
                Held[expect.Id] = new Revisions { Data = expect.Data, Owner = expect.Owner };
            }
            else if (expect.What == Outcome.Owner)
            {
                bool wasServers = Held.TryGetValue(expect.Id, out Revisions held) && held.Data == expect.LocalData
                    && held.Owner == expect.LocalOwner && expect.Data == expect.LocalData;
                Forget(expect.Id);
                if (wasServers)
                {
                    Held[expect.Id] = new Revisions { Data = expect.Data, Owner = expect.Owner };
                }
            }
        }

        public static void Forget(ZDOID id)
        {
            if (Held.Count > 0)
            {
                Held.Remove(id);
            }
        }

        public static void Clear() => Held.Clear();
    }
}
