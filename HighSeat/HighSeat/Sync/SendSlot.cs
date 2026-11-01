using System.Collections.Generic;

namespace HighSeat
{
    /// <summary>
    /// One player's place in a batch of sends (<see cref="PeerSend"/>): who, their byte budget for this round (worked out
    /// on the main thread from their socket) and whether they read deltas, and the objects to send them in order, written
    /// by whichever thread prepares them (<see cref="PrepPool"/>) and read by the main thread once all are done. The
    /// slots are made once and reused; the list is emptied after every send, so no ZDO reference outlives the frame.
    /// </summary>
    internal sealed class SendSlot
    {
        public PeerSync Peer = null!;
        public int Budget;

        /// <summary>True when the player reads deltas (<see cref="DeltaPeers"/>), looked up on the main thread.</summary>
        public bool Deltas;

        /// <summary>Objects in the list that are new to the player (no record of them held), counted as it is prepared.</summary>
        public int Fresh;

        /// <summary>True once the list is complete; a thread that failed leaves it false for the main thread to redo.</summary>
        public bool Prepared;

        public readonly List<ZDO> ToSend = new List<ZDO>();
    }
}
