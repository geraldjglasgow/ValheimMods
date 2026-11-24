using UnityEngine;

namespace DevBridge.Load
{
    /// <summary>
    /// One fake player: its connection (<see cref="FakeSocket"/>), the game's own peer made on it, its path and body, when
    /// it next moves and next reports its position, and the marks a /load measure takes of what it was sent.
    /// </summary>
    internal sealed class FakePlayer
    {
        internal FakePlayer(int index, long uid, string host, string mode, FakePath path)
        {
            Index = index;
            Name = "Fake " + index;
            Uid = uid;
            Mode = mode;
            Path = path;
            Socket = new FakeSocket(host, "DevBridge fake " + index);
            Peer = new ZNetPeer(Socket, server: false);
            Joined = Time.time;
        }

        internal int Index { get; }

        internal string Name { get; }

        /// <summary>The peer's session id: its uid on the server, and the user part of its body's ZDO id.</summary>
        internal long Uid { get; }

        internal string Mode { get; }

        internal FakePath Path { get; }

        internal FakeSocket Socket { get; }

        internal ZNetPeer Peer { get; }

        internal float Joined { get; }

        /// <summary>The server's record of what this peer holds (ZDOMan's own), found once it joined.</summary>
        internal ZDOMan.ZDOPeer Holdings;

        /// <summary>True when it said hello in HighSeat's delta format (deltas=1, <see cref="FakeDeltas"/>).</summary>
        internal bool Deltas;

        internal ZDO Body;
        internal ZDOID BodyId = ZDOID.None;
        internal float NextMove;
        internal float NextRefPos;
        internal readonly SendMarks Marks = new SendMarks();
    }

    /// <summary>What a fake had been sent when a measure first saw it (<see cref="Since"/>) and when its last second ended.</summary>
    internal sealed class SendMarks
    {
        internal int Session;
        internal float Since;
        internal long Sent;
        internal long World;
        internal int Sends;
        internal long LastSent;
        internal long LastWorld;
        internal int LastSends;
    }
}
