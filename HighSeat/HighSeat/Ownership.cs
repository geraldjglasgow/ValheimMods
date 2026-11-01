using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// The game's ownership pass, on the server every two seconds, once for the server's own position and once for every
    /// player's (ZDOMan.ReleaseNearbyZDOS), the calls spread over frames by <see cref="OwnershipRounds"/>. Everything goes as the game decides, except what the server runs
    /// (<see cref="ServerRun"/>): near any player it goes to the server and stays there, wherever the creature wanders,
    /// so it never flips between players. While the server is over its frame budget (<see cref="FrameGuard"/>) a
    /// creature it does not run yet goes as the game decides; one it runs stays. With "Creatures only where shared" on,
    /// creatures follow <see cref="SharedRule"/> instead (the server's where two or more players share them), and the
    /// pass's players are listed once at its start; spawners and everything else go as above. A player in a zone
    /// already done in this pass (this round) is passed over: their area is the same, so what is free there went to
    /// the first player and an owner in the area keeps what it owns. The one difference from the game: something a later
    /// player owns in the scanned ring just outside the area is not let go by their pass; any other player near it still
    /// takes it over, and with nobody near it no one runs it either way. 100 players in one zone scanned it 100 times
    /// every 2 s (measured 2026-10-31).
    /// </summary>
    [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.ReleaseNearbyZDOS))]
    internal static class Ownership
    {
        private static readonly List<ZDO> Near = new List<ZDO>();
        private static readonly HashSet<Vector2s> Done = new HashSet<Vector2s>();

        private static bool Prefix(ZDOMan __instance, Vector3 refPosition, long uid)
        {
            if (!ServerRun.Active)
            {
                return true;
            }
            bool shared = Settings.OnlyWhereShared.Value;
            Vector2s zone = ZoneSystem.GetZone(refPosition);
            if (!Done.Add(zone))
            {
                return false;
            }
            SimulationDistance synced = ZNet.instance.GetSyncedSimulationDistance();
            Near.Clear();
            __instance.FindSectorObjects(zone, new SimulationDistance(synced.NearSimulationDistance, 0, synced.IsClassic), Near);
            Release(__instance, zone, uid, shared);
            Near.Clear();
            return false;
        }

        /// <summary>A new pass (<see cref="OwnershipRounds"/>): no zone done yet, and the shared rule's players listed again.</summary>
        public static void BeginRound()
        {
            Done.Clear();
            if (Settings.OnlyWhereShared.Value)
            {
                SharedRule.BeginPass();
            }
        }

        private static void Release(ZDOMan man, Vector2s zone, long uid, bool shared)
        {
            long server = ZDOMan.GetSessionID();
            bool take = !FrameGuard.Busy;
            foreach (ZDO zdo in Near)
            {
                if (zdo.Persistent && !(shared ? SharedRule.Decide(zdo, uid, server, take) : Claim(zdo, server, take)))
                {
                    Vanilla(man, zdo, zone, uid);
                }
            }
        }

        // True when the server runs it (now, or from here on); false leaves it to the game's own rule.
        internal static bool Claim(ZDO zdo, long server, bool take)
        {
            if (!ServerRun.Is(zdo))
            {
                return false;
            }
            if (zdo.GetOwner() == server)
            {
                return true;
            }
            if (!take)
            {
                return false;
            }
            zdo.SetOwner(server);
            return true;
        }

        // The game's own rule: an owner that left the area lets go; a free one, or one whose owner is away, goes to this peer.
        private static void Vanilla(ZDOMan man, ZDO zdo, Vector2s zone, long uid)
        {
            Vector3 position = zdo.GetPosition();
            if (zdo.GetOwner() == uid)
            {
                if (!ZNetScene.InActiveArea(position, zone))
                {
                    zdo.SetOwner(0L);
                }
            }
            else if ((!zdo.HasOwner() || !man.IsInPeerActiveArea(position, zdo.GetOwner())) && ZNetScene.InActiveArea(position, zone))
            {
                zdo.SetOwner(uid);
            }
        }
    }
}
