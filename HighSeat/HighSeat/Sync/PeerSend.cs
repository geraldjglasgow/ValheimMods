using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// The players' turns in a round, the game's ZDOMan.SendZDOs for several players at a time. A player with nothing
    /// waiting, force-sent or to drop costs one test. Otherwise their byte budget is this round's share of HighSeat's
    /// send rate (<see cref="SendRate"/>: 500 KB/s is <see cref="RoundBytes"/> bytes a round; crossplay players keep the
    /// game's own <see cref="CrossplayBytes"/>, as HighSeat leaves their PlayFab rate alone), no more than an even share
    /// of the server's whole <see cref="ServerBytesPerSecond"/> (a ceiling well above what 100 players in one crowd
    /// need), less what still waits in their socket; as in the game a player with less than <see cref="MinBytes"/> left
    /// is passed over this round. The players are taken in batches of as many as fit the frame's time left
    /// (<see cref="BatchCost"/>, at most <see cref="MaxBatch"/>; one batch of one player at least each frame, so a round
    /// always ends). Each batch's send lists are prepared together (<see cref="PrepPool"/>, on worker threads where the
    /// machine has the cores; what is sent, see <see cref="SendPrep"/>), and with them the records and deltas the
    /// packages will need that nothing made serves (<see cref="Ahead"/>, <see cref="Premade"/>), then, on the main thread
    /// and in the players' order, the force-sent are put in front (the game's own ZDOMan.AddForceSendZdos: Wayfare sends
    /// far areas through it) and each package is written and sent (<see cref="Package"/>). A package changes nothing
    /// another player's preparation reads (only that player's own books, the shared records and their socket), and no
    /// game code runs in between, so preparing a batch first and sending it after sends exactly what one player after the
    /// other would; what was made ahead is what the package would have made, as the object, the kept records and the
    /// history are the same then. What does not fit stays pending. What was made ahead and the slots' lists are let go
    /// after the batch's sends.
    /// </summary>
    internal static class PeerSend
    {
        private const int RoundBytes = 500 * 1024 / 20;
        private const int CrossplayBytes = 10240;
        private const int ServerBytesPerSecond = 64 * 1024 * 1024;
        private const int MinBytes = 2048;
        private const int MaxBatch = 32;

        private static readonly SendSlot[] Slots = MakeSlots();
        private static int _count;
        private static int _share = RoundBytes;

        /// <summary>At a round's start: each player's even share of the server's ceiling for this round.</summary>
        public static void Share(int players)
        {
            _share = players > 0 ? ServerBytesPerSecond / 20 / players : RoundBytes;
        }

        /// <summary>
        /// Serves the round's players from <paramref name="next"/> on, batch by batch, until the frame's time
        /// (<paramref name="until"/> ticks of <paramref name="clock"/>) is spent; returns how far it got.
        /// </summary>
        public static int Serve(ZDOMan man, List<PeerSync> order, int next, Stopwatch clock, long until)
        {
            bool first = true;
            while (next < order.Count)
            {
                int fit = BatchCost.Players(until - clock.ElapsedTicks, PrepPool.Lanes, MaxBatch);
                if (fit == 0 && !first)
                {
                    break;
                }
                next = Fill(order, next, Math.Max(fit, 1));
                first = false;
                if (_count > 0)
                {
                    Run(man, clock);
                }
            }
            return next;
        }

        /// <summary>Lets go of every ZDO reference held for a send (the loop stopped).</summary>
        public static void Clear()
        {
            _count = Slots.Length;
            ClearBatch();
            PrepPool.Clear();
        }

        // After a batch, sent or not: no ZDO reference is kept past the frame (after a hang the slots are left to the
        // stuck thread: the loop never runs again).
        private static void ClearBatch()
        {
            for (int i = 0; i < _count && !PrepPool.Hung; i++)
            {
                Slots[i].ToSend.Clear();
                Slots[i].Peer = null!;
            }
            _count = 0;
            if (!PrepPool.Hung)
            {
                Premade.Clear();
            }
        }

        private static SendSlot[] MakeSlots()
        {
            SendSlot[] slots = new SendSlot[MaxBatch];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = new SendSlot();
            }
            return slots;
        }

        // The next players with something to send and room in their socket, up to want of them.
        private static int Fill(List<PeerSync> order, int next, int want)
        {
            _count = 0;
            while (next < order.Count && _count < want)
            {
                PeerSync ps = order[next++];
                if (ps.Gone || !ps.HasWork)
                {
                    continue;
                }
                int budget = Budget(ps.Peer.m_peer.m_socket);
                if (budget < MinBytes)
                {
                    #if DEBUG
                    SyncStats.Skipped();
                    #endif
                    continue;
                }
                SendSlot slot = Slots[_count++];
                slot.Peer = ps;
                slot.Budget = budget;
                slot.Deltas = DeltaPeers.Of(ps.Peer.m_peer) != null;
                slot.Prepared = false;
            }
            return next;
        }

        // Below MinBytes (or under 0) when the socket's queue leaves too little, as ZDOMan.SendZDOs checks it.
        private static int Budget(ISocket socket)
        {
            int full = socket is ZPlayFabSocket ? CrossplayBytes : RoundBytes;
            return Mathf.Min(full, _share) - socket.GetSendQueueSize();
        }

        // Prepares the batch, then sends it in order; the time of each part goes to the batch size and the numbers.
        private static void Run(ZDOMan man, Stopwatch clock)
        {
            try
            {
                long start = clock.ElapsedTicks;
                int lanes = PrepPool.LanesFor(_count);
                PrepPool.Prepare(man, Slots, _count, Time.time);
                long prepared = clock.ElapsedTicks;
                for (int i = 0; i < _count; i++)
                {
                    Send(man, Slots[i]);
                }
                long sent = clock.ElapsedTicks;
                BatchCost.Measured(_count, lanes, prepared - start, sent - prepared);
                #if DEBUG
                SyncStats.Batch(lanes, prepared - start, sent - prepared, PrepPool.TakeHoldHits());
                #endif
            }
            finally
            {
                ClearBatch();
            }
        }

        private static void Send(ZDOMan man, SendSlot slot)
        {
            if (slot.Peer.Gone)
            {
                return;
            }
            man.AddForceSendZdos(slot.Peer.Peer, slot.ToSend);
            Package.Send(man, slot.Peer, slot.ToSend, slot.Budget);
        }
    }
}
