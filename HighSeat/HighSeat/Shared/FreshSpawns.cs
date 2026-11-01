using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// A creature the server has just made near one player goes to that player
    /// once its spawn-time code has run on the server, so every roll on spawn is the server's (Elite Creatures Reborn rolls
    /// stars and mutations in its controller's Start, on the owner). The order, in the game: the spawner's Instantiate
    /// runs every Awake (the ZDO is made, owned by the server; this is noted in <see cref="CreatureWake"/>), then the
    /// spawner's own lines right after it (level, hunting, raid tag, patrol point); every Start runs before the creature's
    /// first update, in that frame or the next (default gear, best weapon, rolls); its first FixedUpdate and Update follow.
    /// The hand-over waits <see cref="HoldFrames"/> frames after the frame it was made (checked every frame, after the
    /// game's ZNetScene.Update), so all of that has run on the server. Then, by the players' positions that frame: one
    /// player only (not the host, who is the server) - it is theirs, sent to them first thing (<see cref="Handover"/>);
    /// two or more - it stays, shared; none - it stays, as 0.2.3 keeps it. Between the hand-over and the player's game
    /// receiving it nobody moves it, so a new creature stands still for that moment (the next send to that player, plus
    /// their ping); before it, the server runs it for those few frames without the lone player's buildings and trees,
    /// too short to walk anywhere. A creature loaded from its ZDO (ZNetView.m_useInitZDO set by ZNetScene.CreateObject)
    /// or made in a zone's ghost pass is not new.
    /// </summary>
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Update))]
    internal static class FreshSpawns
    {
        /// <summary>Frames a creature the server made stays with it before it may go to a lone player.</summary>
        public const int HoldFrames = 3;

        private struct Fresh
        {
            public ZDOID Id;
            public int Frame;
        }

        private static readonly List<Fresh> Queue = new List<Fresh>();
        private static readonly HashSet<ZDOID> Queued = new HashSet<ZDOID>();

        /// <summary>True while a creature the server made waits for its hand-over.</summary>
        public static bool Pending(ZDOID id) => Queued.Count > 0 && Queued.Contains(id);

        /// <summary>A creature waking on the server: noted when the server made it just now and owns it.</summary>
        public static void Note(BaseAI ai)
        {
            if (!ServerRun.Active || ZNetView.m_useInitZDO || ZNetView.m_ghostInit)
            {
                return;
            }
            ZDO zdo = ai.m_nview != null ? ai.m_nview.GetZDO() : null;
            if (zdo != null && zdo.IsOwner() && Queued.Add(zdo.m_uid))
            {
                Queue.Add(new Fresh { Id = zdo.m_uid, Frame = Time.frameCount });
            }
        }

        private static void Postfix()
        {
            if (Queue.Count == 0)
            {
                return;
            }
            if (!ServerRun.Active)
            {
                Forget();
                return;
            }
            int due = Time.frameCount - HoldFrames;
            int kept = 0;
            for (int i = 0; i < Queue.Count; i++)
            {
                if (Queue[i].Frame > due)
                {
                    Queue[kept++] = Queue[i];
                    continue;
                }
                Queued.Remove(Queue[i].Id);
                Settle(Queue[i].Id);
            }
            Queue.RemoveRange(kept, Queue.Count - kept);
        }

        private static void Forget()
        {
            Queue.Clear();
            Queued.Clear();
        }

        private static void Settle(ZDOID id)
        {
            ZDO zdo = ZDOMan.instance.GetZDO(id);
            if (zdo == null || !zdo.IsOwner())
            {
                return;
            }
            if (NearPlayers.CountLive(zdo.GetPosition(), out long single) == 1 && single != ZDOMan.GetSessionID())
            {
                Handover.Give(zdo, single);
            }
        }
    }
}
