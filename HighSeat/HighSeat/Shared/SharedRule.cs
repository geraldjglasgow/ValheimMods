using System.Collections.Generic;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// The ownership pass for what the server runs (<see cref="Ownership"/> hands each persistent ZDO here).
    /// Spawners go to the server everywhere, as without the setting, so every spawn starts there. A creature in the active
    /// area of two or more players (<see cref="NearPlayers"/>; on a hosted game the host counts) goes to the server; one
    /// in a single player's goes by the game's own rule, which gives it to that player, so their hits need no round trip.
    /// A creature the server took because players shared it stays with it until one player has been the only one near it
    /// for <see cref="AloneSeconds"/> (sampled every pass, so 10 to 12 s), and only then goes to that player, so it does
    /// not flip as players step in and out of range. Any other creature the server owns (made with the world, left from
    /// the setting off, or from before a restart) goes to the one player near it at once, and nothing round it is loaded
    /// in full: holding those for the timer loaded every lone area in full (50 players spread out kept a dedicated server
    /// at 4 fps, measured 2026-11-01). Near nobody it stays, as 0.2.3 keeps it; near only the host it stays, the host
    /// being the server. One the server has just made is left to <see cref="FreshSpawns"/>. While the server is
    /// over its frame budget (<see cref="FrameGuard"/>) it takes on nothing new; giving a creature away is always allowed.
    /// Each pass also marks what must be loaded in full (<see cref="FullSectors"/>): round players in company, round every
    /// creature the server runs, round boss altars.
    /// </summary>
    internal static class SharedRule
    {
        /// <summary>Seconds a single player must have been alone near a server-run creature before it is theirs.</summary>
        public const float AloneSeconds = 10f;

        private struct Alone
        {
            public long Uid;
            public float Since;
            public int Pass;
        }

        private static readonly Dictionary<ZDOID, Alone> Timers = new Dictionary<ZDOID, Alone>();
        private static readonly List<ZDOID> Expired = new List<ZDOID>();
        private static readonly List<bool> InCompany = new List<bool>();
        private static readonly HashSet<ZDOID> Shared = new HashSet<ZDOID>();
        private static readonly List<ZDOID> Gone = new List<ZDOID>();
        private static int _pass = -1;
        private static float _now;

        /// <summary>The start of a pass: once per ownership round (<see cref="OwnershipRounds"/>).</summary>
        public static void BeginPass()
        {
            NearPlayers.Build();
            int last = _pass;
            _pass = OwnershipRounds.Round;
            _now = Time.time;
            DropTimers(last);
            ForgetShared();
            MarkCompany();
            AlwaysAnimate.Prune();
            LoneSectors.Forget(_now);
        }

        /// <summary>True when the server runs it from here on; false leaves it to the game's own rule.</summary>
        public static bool Decide(ZDO zdo, long uid, long server, bool take)
        {
            if (!ServerRun.Is(zdo))
            {
                return false;
            }
            if (!ServerRun.IsCreature(zdo))
            {
                return Spawner(zdo, server, take);
            }
            AlwaysAnimate.Check(zdo);
            int near = NearPlayers.CountAt(zdo.GetPosition(), out long single);
            if (zdo.GetOwner() == server)
            {
                Keep(zdo, near, single, uid);
                return true;
            }
            if (near < 2 || !take)
            {
                return false;
            }
            Handover.Take(zdo, server);
            Shared.Add(zdo.m_uid);
            Mark(zdo);
            return true;
        }

        private static bool Spawner(ZDO zdo, long server, bool take)
        {
            if (ServerRun.IsAltar(zdo))
            {
                FullSectors.MarkAround(zdo.GetSector(), 1, _now);
            }
            return Ownership.Claim(zdo, server, take);
        }

        // A creature the server runs: shared (or near only the host) it stays; one it took for company goes to a lone player
        // once they have been alone with it long enough, any other at once; either way in that player's own call of the
        // pass (every call of the pass sees the creature, theirs included).
        private static void Keep(ZDO zdo, int near, long single, long uid)
        {
            if (FreshSpawns.Pending(zdo.m_uid))
            {
                return;
            }
            if (near >= 2 || (near == 1 && single == ZDOMan.GetSessionID()))
            {
                Shared.Add(zdo.m_uid);
                Mark(zdo);
                return;
            }
            if (!Shared.Contains(zdo.m_uid))
            {
                GiveAlone(zdo, near, single, uid);
                return;
            }
            if (near != 1 || !AloneLongEnough(zdo.m_uid, single) || single != uid)
            {
                Mark(zdo);
                return;
            }
            Timers.Remove(zdo.m_uid);
            Shared.Remove(zdo.m_uid);
            Handover.Give(zdo, uid);
        }

        // One the server never shared: the lone player near it gets it now; near nobody it stays, nothing loaded round it.
        private static void GiveAlone(ZDO zdo, int near, long single, long uid)
        {
            if (near == 1 && single == uid)
            {
                Handover.Give(zdo, uid);
            }
        }

        // Shared creatures that are gone or no longer the server's are forgotten, once a pass.
        private static void ForgetShared()
        {
            long server = ZDOMan.GetSessionID();
            foreach (ZDOID id in Shared)
            {
                ZDO zdo = ZDOMan.instance.GetZDO(id);
                if (zdo == null || zdo.GetOwner() != server)
                {
                    Gone.Add(id);
                }
            }
            foreach (ZDOID id in Gone)
            {
                Shared.Remove(id);
            }
            Gone.Clear();
        }

        // Keeps the creature's timer for the one player near it; true once that same player has been alone with it long enough.
        private static bool AloneLongEnough(ZDOID id, long uid)
        {
            if (Timers.TryGetValue(id, out Alone alone) && alone.Uid == uid)
            {
                alone.Pass = _pass;
                Timers[id] = alone;
                return _now - alone.Since >= AloneSeconds;
            }
            Timers[id] = new Alone { Uid = uid, Since = _now, Pass = _pass };
            return false;
        }

        // A timer not kept in the last pass breaks the run: the creature had nobody, or company, or another player.
        private static void DropTimers(int lastPass)
        {
            foreach (KeyValuePair<ZDOID, Alone> entry in Timers)
            {
                if (entry.Value.Pass != lastPass)
                {
                    Expired.Add(entry.Key);
                }
            }
            foreach (ZDOID id in Expired)
            {
                Timers.Remove(id);
            }
            Expired.Clear();
        }

        // Everything round a creature the server runs is loaded in full, so it collides, paths and picks targets there.
        private static void Mark(ZDO zdo) => FullSectors.MarkAround(zdo.GetSector(), 1, _now);

        // Players whose zones are close enough for their creatures to be shared soon: everything round them in full.
        private static void MarkCompany()
        {
            FindCompany(FullSectors.CompanyZones(ZNet.instance.GetSyncedSimulationDistance()));
            for (int i = 0; i < NearPlayers.Count; i++)
            {
                if (InCompany[i])
                {
                    FullSectors.MarkAround(NearPlayers.ZoneOf(i), FullSectors.CompanyReach, _now);
                }
            }
        }

        // Every pair once: a few thousand tests for a hundred players, once a pass.
        private static void FindCompany(int company)
        {
            InCompany.Clear();
            for (int i = 0; i < NearPlayers.Count; i++)
            {
                InCompany.Add(false);
            }
            for (int i = 0; i < NearPlayers.Count; i++)
            {
                for (int j = i + 1; j < NearPlayers.Count; j++)
                {
                    if (Apart(NearPlayers.ZoneOf(i), NearPlayers.ZoneOf(j)) <= company)
                    {
                        InCompany[i] = true;
                        InCompany[j] = true;
                    }
                }
            }
        }

        private static int Apart(Vector2s a, Vector2s b) => Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
    }
}
