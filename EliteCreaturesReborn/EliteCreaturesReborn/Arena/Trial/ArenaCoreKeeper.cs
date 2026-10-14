using System.Collections.Generic;
using EliteCreaturesReborn.Patches;
using EliteCreaturesReborn.Util;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// Keeps exactly one arena core in the world (features/arena.md section 4: "placed by the server when the world has
    /// none"). It lives on the world's ZoneSystem object, so it comes and goes with the world, and does anything only on
    /// the server, which holds every ZDO of the world: every few seconds it looks for the core it knows (one ZDO lookup);
    /// not finding it, it searches the one zone at the arena's origin for core ZDOs, places a new core there when there
    /// is none and Arena is on (the server makes its ZDO without the zone being loaded: the object it instantiates is
    /// dropped again at once, the persistent ZDO stays), and removes any extra a race could have left. The core is
    /// persistent, so it is found again after every restart and never placed twice. The same check removes an older
    /// world's arena once (<see cref="ArenaLegacy"/>) and ends a trial nobody is at any more
    /// (<see cref="ArenaOrphanWatch"/>). The core is owned, as any object, by a player at the arena.
    /// </summary>
    internal sealed class ArenaCoreKeeper : MonoBehaviour
    {
        /// <summary>Seconds between checks; the first waits as long, so the world's saved objects are loaded.</summary>
        private const float CheckSeconds = 5f;

        private static readonly List<ZDO> Sector = new List<ZDO>();

        private static ZDOID _known = ZDOID.None;

        private float _next;

        /// <summary>The world's arena core, as the server (which holds every ZDO) finds it; null when there is none.</summary>
        public static ZDO? KnownCore()
        {
            ZDOMan zdos = ZDOMan.instance;
            if (zdos == null)
            {
                return null;
            }
            ZDO? zdo = _known.IsNone() ? null : zdos.GetZDO(_known);
            if (zdo != null && zdo.GetPrefab() == ArenaCorePrefab.PrefabHash)
            {
                return zdo;
            }
            _known = Find();
            return _known.IsNone() ? null : zdos.GetZDO(_known);
        }

        private void Start() => _next = Time.time + CheckSeconds;

        private void Update()
        {
            float now = Time.time;
            if (now < _next)
            {
                return;
            }
            _next = now + CheckSeconds;
            Guard.Run("arena core keeper", static keeper => keeper.Check(), this);
        }

        private void Check()
        {
            ZNet net = ZNet.instance;
            if (net == null || !net.IsServer() || ZDOMan.instance == null)
            {
                return;
            }
            ArenaLegacy.SweepOnce(); // an older world's core and creatures over the world's centre
            ZDO? core = KnownCore();
            if (core != null)
            {
                ArenaOrphanWatch.Check(core, Time.time);
            }
            else if (Config.ArenaSettings.Enabled)
            {
                _known = Place();
            }
        }

        private static ZDOID Place()
        {
            ZNetView? placed = ArenaCorePrefab.Place(ArenaHall.Origin);
            ZDO? zdo = placed != null ? placed.GetZDO() : null;
            if (zdo == null)
            {
                return ZDOID.None; // the scene is not up yet: the next check tries again
            }
            Log.Info($"Hel's Trial: the arena core is placed at the hall's origin {ArenaHall.Origin:F0}");
            return zdo.m_uid;
        }

        // The core ZDOs in the zone at the world's centre; the first is kept, any other is removed.
        private static ZDOID Find()
        {
            Sector.Clear();
            SimulationDistance oneZone = new SimulationDistance(0, 0, classic: true);
            ZDOMan.instance.FindSectorObjects(ZoneSystem.GetZone(ArenaHall.Origin), oneZone, Sector);
            ZDOID kept = ZDOID.None;
            foreach (ZDO zdo in Sector)
            {
                if (zdo.GetPrefab() != ArenaCorePrefab.PrefabHash)
                {
                    continue;
                }
                if (kept.IsNone())
                {
                    kept = zdo.m_uid;
                }
                else if (ZNet.instance != null && ZNet.instance.IsServer())
                {
                    zdo.SetOwner(ZDOMan.GetSessionID()); // an extra: the server takes it, to remove what it owns
                    ZDOMan.instance.DestroyZDO(zdo);
                }
            }
            Sector.Clear();
            return kept;
        }
    }

    /// <summary>World start, on every machine: the keeper joins the world's ZoneSystem (it acts only on the server).</summary>
    [HarmonyPatch(typeof(ZoneSystem), "Start")]
    internal static class ArenaCoreKeeperPatch
    {
        private static void Postfix(ZoneSystem __instance) => SafeCall.Run("ZoneSystem.Start arena core keeper",
            static zones => zones.gameObject.AddComponent<ArenaCoreKeeper>(), __instance);
    }
}
