using System.Collections.Generic;

namespace HighSeat
{
    /// <summary>
    /// The send loop's state for one player (<see cref="SyncLoop"/>), beside the game's own ZDOMan.ZDOPeer, which keeps
    /// what the player has (m_zdos), what must go at once (m_forceSend) and what to tell them to drop (m_invalidSector).
    /// Here: the objects that may need sending to them, by id (ZDOs are pooled and reused, so no reference is kept from
    /// one frame to the next), a big batch of them set aside in order (<see cref="Backlog"/>), and the area they are
    /// registered for in <see cref="Interest"/>. Made when the player first shows in the game's peer list and let go
    /// when the game removes them (<see cref="PeerLeft"/>).
    /// </summary>
    internal sealed class PeerSync
    {
        public readonly ZDOMan.ZDOPeer Peer;

        /// <summary>Objects that changed in this player's area, or a full check found they lack; checked again at send time.</summary>
        public readonly HashSet<ZDOID> Pending = new HashSet<ZDOID>();

        /// <summary>A big pending set put in send order and set aside, best first, from <see cref="BacklogAt"/> on.</summary>
        public readonly List<ZDOID> Backlog = new List<ZDOID>();

        public int BacklogAt;

        /// <summary>Pending objects the update rate holds back for a later round (<see cref="Holds"/>).</summary>
        public readonly Holds Holds = new Holds();

        /// <summary>True once the player is registered in <see cref="Interest"/> for <see cref="Zone"/> and <see cref="Shape"/>.</summary>
        public bool HasArea;

        public Vector2s Zone;
        public AreaShape Shape = null!;

        /// <summary>Time.time of the last full check of the whole area (<see cref="FullCheck"/>).</summary>
        public float LastFullCheck;

        /// <summary>True once the player left: a round already under way passes over them.</summary>
        public bool Gone;

        /// <summary>The round stamp of the last time the game's peer list held this player (<see cref="SyncPeers"/>).</summary>
        public int Seen;

        /// <summary>Bytes sent to this player in the second under way (<see cref="SyncStats"/>).</summary>
        public int BytesThisSecond;

        public PeerSync(ZDOMan.ZDOPeer peer)
        {
            Peer = peer;
        }

        /// <summary>False when there is nothing to send: nothing pending or set aside, force-sent or to drop.</summary>
        public bool HasWork => Pending.Count > 0 || BacklogAt < Backlog.Count || Peer.m_forceSend.Count > 0
            || Peer.m_invalidSector.Count > 0;

        /// <summary>
        /// Where an object sits in this player's area: <see cref="AreaShape.Near"/>, <see cref="AreaShape.Distant"/>
        /// (only a Distant-flagged object counts there) or <see cref="AreaShape.Out"/>. The sector is the one the game
        /// files the object under (ZDO.GetSectorIndex), so this agrees with what ZDOMan.FindSectorObjects would find.
        /// </summary>
        public byte LevelOf(ZDO zdo)
        {
            uint sector = zdo.GetSectorIndex().Sector;
            if (sector == 0)
            {
                return AreaShape.Out;
            }
            Vector2s at = ZoneSystem.IndexToSector(sector);
            byte level = Shape.At(at.x - Zone.x, at.y - Zone.y);
            return level == AreaShape.Distant && !zdo.Distant ? AreaShape.Out : level;
        }

        /// <summary>Forgets everything waiting (the player left, or the loop stopped).</summary>
        public void Forget()
        {
            Pending.Clear();
            Backlog.Clear();
            BacklogAt = 0;
            Holds.Clear();
        }
    }
}
