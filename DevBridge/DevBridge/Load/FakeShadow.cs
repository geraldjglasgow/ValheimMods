using System.Collections.Generic;
using UnityEngine;

namespace DevBridge.Load
{
    /// <summary>
    /// A verifying fake player's copy of the world (/load?players=N&amp;verify=1): every package the server sends it, in
    /// the game's "ZDOData" or HighSeat's delta format (<see cref="FakeDeltas"/>), is read the moment its socket is
    /// handed it and laid into its own objects (<see cref="ShadowObject"/>) the way a player's game takes them: a whole
    /// record as ZDOMan.RPC_ZDOData takes it (a missing object or a newer data revision takes everything in it, a newer
    /// owner revision alone only the owner, anything else nothing); a delta only when the copy stands at exactly the
    /// revisions it starts from (it then counts as that record with the delta's data), else the object is asked for
    /// whole, as HighSeatPlus asks: the ask is sent on the fake's next tick, after the send that carried the delta has
    /// booked it, as a real player's ask arrives later. The drops change nothing kept (the game only moves the object
    /// out of its sectors). Counts what it took, for the report (<see cref="ShadowVerify"/>). The package is read from
    /// a copy, so the server's own buffer is never moved; anything unreadable is counted, never thrown into the send.
    /// </summary>
    internal sealed class FakeShadow
    {
        internal readonly Dictionary<ZDOID, ShadowObject> Objects = new Dictionary<ZDOID, ShadowObject>();

        /// <summary>Objects to ask for whole on the next tick, and those asked for, by when.</summary>
        internal readonly List<ZDOID> ToAsk = new List<ZDOID>();

        internal readonly Dictionary<ZDOID, float> Asked = new Dictionary<ZDOID, float>();

        internal int GamePackages;
        internal int DeltaPackages;
        internal int WholeRecords;
        internal int DeltaRecords;
        internal int BaseMismatches;
        internal int AsksSent;
        internal int Unreadable;
        internal string LastError;

        /// <summary>A package the server sent the fake, read from a copy in the format its method hash names.</summary>
        internal void Receive(ZPackage sent, int hash)
        {
            try
            {
                var pkg = new ZPackage(sent.GetArray());
                pkg.ReadInt();
                if (ZRpc.m_DEBUG) pkg.ReadString();
                ZPackage payload = pkg.ReadPackage();
                if (hash == FakeDeltas.DataHash) ShadowDecode.Deltas(this, payload);
                else ShadowDecode.Game(this, payload);
            }
            catch (System.Exception error)
            {
                Unreadable++;
                LastError = error.GetType().Name + ": " + error.Message;
            }
        }

        /// <summary>A whole record (or a delta that fits, with its data), taken as ZDOMan.RPC_ZDOData takes it.</summary>
        internal void Take(ZDOID id, ushort owner, uint data, long ownerId, byte[] position, byte[] objectData)
        {
            Asked.Remove(id);
            if (Objects.TryGetValue(id, out ShadowObject copy) && data <= copy.Data)
            {
                if (owner <= copy.Owner) return;
                copy.OwnerId = ownerId;
                copy.Owner = owner;
                return;
            }
            if (copy == null) Objects[id] = copy = new ShadowObject();
            copy.Owner = owner;
            copy.Data = data;
            copy.OwnerId = ownerId;
            copy.Position = position;
            copy.Apply(objectData);
        }

        /// <summary>A delta that does not fit the copy: the object is asked for whole on the next tick.</summary>
        internal void Ask(ZDOID id)
        {
            BaseMismatches++;
            if (!Awaiting(id)) ToAsk.Add(id);
            Asked[id] = Time.time;
        }

        /// <summary>True while an ask for the object waits for its answer (no older than two seconds).</summary>
        internal bool Awaiting(ZDOID id) => Asked.TryGetValue(id, out float when) && Time.time - when < 2f;
    }
}
