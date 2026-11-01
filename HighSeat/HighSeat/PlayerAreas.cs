using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// The server loads every player's area, not only its own (ZNetScene.CreateDestroyObjects, 30 times a second). A full
    /// pass, every FullSeconds: the near sectors around its own position and around every ready player, each sector
    /// once, then the kept places' (<see cref="KeptPlaces"/>, after the players' sectors so a player's sector wins), then
    /// the game's own create, paced by time instead of the game's fixed count (<see cref="CreatePace"/>), and the game's
    /// own remove, spread over passes (<see cref="PacedRemove"/>). The passes between gather the host's own area only
    /// (nothing on a dedicated server), make what is missing there and remove nothing, so what the host sees comes as
    /// fast as in the game: listing every one of 50 players' areas 30 times a second cost 8 ms a pass at 50,000 objects
    /// (measured 2026-11-01).
    /// Distant objects (seen from afar, never simulated) are kept for a hosting player's own view only; a dedicated server
    /// draws nothing. The lists are the game's own (m_tempCurrentObjects, m_tempCurrentDistantObjects), filled as the game
    /// fills them and left filled after, because AreaLoading (Wayfare's quick jumps, OpenKeep's bed respawn) reads them in
    /// a postfix on the same method to load a hosting player's arrival faster. Around a player only sectors whose ground
    /// is already built count: the game waits for the ground around its own position before making anything there, but
    /// not around anyone else's, and a terrain edit made before its ground gives up for good (the ground stays as the
    /// world first made it: creatures float over dug-out ground and sink into raised ground).
    /// With "Creatures only where shared" on, a player's sector is loaded in full only where players are in company or
    /// the server runs a creature or a boss altar stands (<see cref="FullSectors"/>); elsewhere it gets the lone set
    /// (<see cref="LoneSectors"/>): spawners, players, creatures, the ground's shape, locations, base markers, and for a
    /// hosting player's view the distant objects of it. The server's own area (a hosting player's view) is always full.
    /// </summary>
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.CreateDestroyObjects))]
    internal static class PlayerAreas
    {
        private const float FullSeconds = 0.1f;

        private static List<ZDO> Near = null!;
        private static List<ZDO> Distant = null!;
        private static readonly HashSet<ZoneSystem.SectorIndex> Visited = new HashSet<ZoneSystem.SectorIndex>();
        private static bool _shared;
        private static float _now;
        private static float _fullAt = float.NegativeInfinity;

        private static bool Prefix(ZNetScene __instance)
        {
            if (!ServerRun.Active)
            {
                return true;
            }
            Near = __instance.m_tempCurrentObjects;
            Distant = __instance.m_tempCurrentDistantObjects;
            Near.Clear();
            Distant.Clear();
            bool full = Time.unscaledTime - _fullAt >= FullSeconds;
            Gather(full);
            CreatePace.Create(__instance, Near, Distant);
            Remove(__instance, full);
            return false;
        }

        private static void Gather(bool full)
        {
            if (full)
            {
                _fullAt = Time.unscaledTime;
                Collect();
            }
            else
            {
                CollectOwn();
            }
        }

        // What the lists leave out goes, planned on a full pass, a share every pass.
        private static void Remove(ZNetScene scene, bool full)
        {
            if (full)
            {
                PacedRemove.Plan(scene, Near, Distant);
            }
            else
            {
                PacedRemove.Keep(Near, Distant);
            }
            PacedRemove.Step(scene);
        }

        // Between full passes: the host's own area, near and distant, as the game lists it; a dedicated server has none.
        private static void CollectOwn()
        {
            Visited.Clear();
            if (ZNet.instance.IsDedicated())
            {
                return;
            }
            _shared = Settings.OnlyWhereShared.Value;
            _now = Time.time;
            SimulationDistance sim = ZNet.instance.GetSyncedSimulationDistance();
            Vector2s own = ZoneSystem.GetZone(ZNet.instance.GetReferencePosition());
            AddNear(own, sim, false);
            AddDistant(own, sim);
        }

        private static void Collect()
        {
            Visited.Clear();
            _shared = Settings.OnlyWhereShared.Value;
            _now = Time.time;
            FullSectors.NewTick();
            SimulationDistance sim = ZNet.instance.GetSyncedSimulationDistance();
            Vector2s own = ZoneSystem.GetZone(ZNet.instance.GetReferencePosition());
            AddNear(own, sim, false);
            foreach (ZNetPeer peer in ZNet.instance.GetPeers())
            {
                if (peer.IsReady())
                {
                    AddNear(ZoneSystem.GetZone(peer.GetRefPos()), sim, true);
                }
            }
            KeptPlaces.AddStill(Near, Visited);
            if (!ZNet.instance.IsDedicated())
            {
                AddDistant(own, sim);
            }
        }

        private static void AddNear(Vector2s center, SimulationDistance sim, bool groundFirst)
        {
            int n = sim.NearSimulationDistance;
            for (int dy = -n; dy <= n; dy++)
            {
                for (int dx = -n; dx <= n; dx++)
                {
                    Vector2s sector = new Vector2s(center.x + dx, center.y + dy);
                    bool inside = (dx == 0 && dy == 0) || sim.IsClassic || ZoneSystem.instance.ZonesWithinRadius(center, sector, n);
                    if (!inside || (groundFirst && !ZoneSystem.instance.m_zones.ContainsKey(sector)))
                    {
                        continue;
                    }
                    if (groundFirst && _shared)
                    {
                        AddPlayerSector(sector);
                    }
                    else
                    {
                        ZDOMan.instance.FindObjects(sector, Near, Visited);
                    }
                }
            }
        }

        // A player's sector with the setting on: in full where FullSectors says so or a kept place holds it (it stays
        // loaded for when the player leaves), else its lone set. One already visited is left to FindObjects, which skips
        // it, before FullSectors is asked (asking may use up a drop).
        private static void AddPlayerSector(Vector2s sector)
        {
            ZoneSystem.SectorIndex index = ZoneSystem.SectorToIndex(sector);
            if (index.Sector == 0 || Visited.Contains(index) || KeptZones.Holds(sector) || FullSectors.IsFull(index.Sector, _now))
            {
                ZDOMan.instance.FindObjects(sector, Near, Visited);
                return;
            }
            Visited.Add(index);
            LoneSectors.AddNear(index.Sector, Near, _now);
        }

        // The game's distant ring around the host's own position; sectors already found near are skipped by Visited, a
        // player's lone sector gives the distant objects its lone set left out.
        private static void AddDistant(Vector2s center, SimulationDistance sim)
        {
            int total = sim.TotalSimulationDistance;
            for (int dy = -total; dy <= total; dy++)
            {
                for (int dx = -total; dx <= total; dx++)
                {
                    Vector2s sector = new Vector2s(center.x + dx, center.y + dy);
                    if (sim.IsClassic || ZoneSystem.instance.ZonesWithinRadius(center, sector, total, ghostZone: true))
                    {
                        AddDistantSector(sector);
                    }
                }
            }
        }

        private static void AddDistantSector(Vector2s sector)
        {
            uint index = ZoneSystem.SectorToIndex(sector).Sector;
            if (_shared && index != 0 && LoneSectors.LoadedLone(index, _now))
            {
                LoneSectors.AddFar(index, Distant, _now);
                return;
            }
            ZDOMan.instance.FindDistantObjects(sector, Distant, Visited);
        }
    }
}
