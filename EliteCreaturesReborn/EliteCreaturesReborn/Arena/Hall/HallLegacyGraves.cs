using System.Collections.Generic;
using EliteCreaturesReborn.Util;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Hall
{
    /// <summary>
    /// Graves left in the old hall (ECR 3.25.0 and 3.26.0 stood it at (0, 5000, 0), over the world's spawn): once per
    /// world, on the server, while the Arena setting is on, every player tombstone (<c>Player_tombstone</c>) within 100 m
    /// across of the old origin and above 4,900 m is moved, with its spawn point (where a tombstone returns when pushed
    /// more than 4 m away), to just inside the new arena's gate: the walls' <c>gate_inside</c>, a metre up. The arena is
    /// not built on the server at start, so the point is the walls' plan's (<c>aw_plan.points</c>: 2 m inside the gate's
    /// frame at r 66, (0, 0, 64) in arena coordinates) unless the hall stands. The server takes each grave's ownership
    /// first, as the trial's sweep of the old core does, so the move is saved and sent; a client then takes it over as
    /// usual when it loads. The trial removes the old core and arena creatures (<c>Trial/ArenaLegacy</c>).
    /// </summary>
    internal static class HallLegacyGraves
    {
        private static readonly Vector3 OldOrigin = new Vector3(0f, 5000f, 0f);
        private static readonly Vector3 GateInside = new Vector3(0f, 0f, 64f);
        private static readonly int Grave = "Player_tombstone".GetStableHashCode();
        private static readonly List<ZDO> Found = new List<ZDO>();
        private const float Reach = 100f;
        private const float Above = 4900f;
        private const float Raise = 1f;
        private const int Zones = 2;

        private static ZDOMan? _doneFor;

        /// <summary>Once per world on the server, when the arena has moved from the old spot.</summary>
        public static void MoveOnce()
        {
            ZDOMan? zdos = ZDOMan.instance;
            if (zdos == null || ReferenceEquals(_doneFor, zdos) || ZNet.instance == null)
            {
                return;
            }
            _doneFor = zdos; // a client is done here too: only the server moves graves
            if (!ZNet.instance.IsServer() || (ArenaHall.Origin - OldOrigin).sqrMagnitude < Reach * Reach)
            {
                return;
            }
            Found.Clear();
            zdos.FindSectorObjects(ZoneSystem.GetZone(OldOrigin), new SimulationDistance(Zones, 0, classic: true), Found);
            int moved = 0;
            foreach (ZDO zdo in Found)
            {
                moved += Old(zdo) && Move(zdo) ? 1 : 0;
            }
            Found.Clear();
            if (moved > 0)
            {
                Log.Info($"Hel's arena: moved {moved} grave(s) from the old hall at {OldOrigin:F0} to the arena's gate.");
            }
        }

        private static bool Old(ZDO zdo)
        {
            Vector3 at = zdo.GetPosition();
            float dx = at.x - OldOrigin.x;
            float dz = at.z - OldOrigin.z;
            return zdo.GetPrefab() == Grave && at.y > Above && dx * dx + dz * dz < Reach * Reach;
        }

        private static bool Move(ZDO zdo)
        {
            Vector3 spot = (ArenaHall.TryPoint("gate_inside", out Vector3 gate, out _) ? gate : ArenaHall.Origin + GateInside)
                + Vector3.up * Raise;
            zdo.SetOwner(ZDOMan.GetSessionID());
            zdo.Set(ZDOVars.s_spawnPoint, spot);
            zdo.SetPosition(spot);
            return true;
        }
    }
}
