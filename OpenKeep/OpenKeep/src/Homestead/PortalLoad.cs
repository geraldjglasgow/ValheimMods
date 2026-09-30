using System.Diagnostics;
using UnityEngine;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// Quick Area Loading, the land. After a long jump or while a respawn waits, the game loads the zones around the
    /// player one every 0.1 s (<c>ZoneSystem.Update</c> calls <c>CreateLocalZones</c>, which spawns one zone per call),
    /// and <c>ZNetScene</c> creates no nearby object until every zone of the simulation area is loaded
    /// (<c>IsActiveAreaLoaded</c>): with a simulation distance of 4 that is about 57 zones, some 6 s before the first
    /// tree appears, however quick the jump. While <see cref="Busy"/> and the area is not loaded yet, this calls
    /// <c>CreateLocalZones</c> again and again within <see cref="BudgetMs"/> of each frame, so the zones load in the
    /// game's own order as fast as the terrain is ready (the game builds it on its worker thread; asking again only
    /// queues it). The objects follow in <see cref="PortalObjects"/>. The screen is black then, so the longer frames
    /// are not seen. Nothing happens on a dedicated server, or on a server before it has placed its locations.
    /// </summary>
    public static class PortalLoad
    {
        private const double BudgetMs = 20.0;

        private static readonly Stopwatch clock = new Stopwatch();
        private static float startedAt = -1f;
        private static int zones;

        /// <summary>The local player's jump behind the loading screen, or its respawn, waits for the area around it.</summary>
        public static bool Busy()
        {
            if (!PortalSettings.QuickAreaLoading.Value || ZNet.instance == null || ZNet.instance.IsDedicated())
                return false;
            if (ZNet.GetConnectionStatus() != ZNet.ConnectionStatus.Connected)
                return false;
            Player player = Player.m_localPlayer;
            bool jumping = player != null && player.IsTeleporting() && !PortalScreen.Clear(player);
            bool respawning = player == null && Game.instance != null && Game.instance.WaitingForRespawn();
            return jumping || respawning;
        }

        public static void Tick(ZoneSystem system)
        {
            if (!Busy() || (ZNet.instance.IsServer() && !system.LocationsGenerated) || system.IsActiveAreaLoaded())
            {
                Report();
                return;
            }
            if (startedAt < 0f)
                startedAt = Time.time;
            Vector3 at = ZNet.instance.GetReferencePosition();
            clock.Restart();
            while (clock.Elapsed.TotalMilliseconds < BudgetMs && system.CreateLocalZones(at))
                zones++;
        }

        private static void Report()
        {
            if (startedAt < 0f)
                return;
            Plugin.Log.LogInfo($"OpenKeep: loaded the land around you in {Time.time - startedAt:0.0} s ({zones} zones beyond the game's one per 0.1 s)");
            startedAt = -1f;
            zones = 0;
        }
    }
}
