using System;
using System.Collections.Generic;
using OpenKeep.Core;
using OpenKeep.Reach;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// The feeding rule, run by the station's ZDO owner after the game's own one-second tick. With room for an item
    /// (the queue below <c>m_maxOre</c>, the game's <c>OnAddOre</c> cap) one acceptable item is taken from the
    /// containers within range of the station's outline (each keeps Auto Feed Leave units of every item) and handed
    /// to the game's <c>RPC_AddOre</c> with its prefab
    /// name and cheated flag; with room for fuel (the fuel at most <c>m_maxFuel</c> - 1, the game's <c>OnAddFuel</c>
    /// cap) one fuel unit goes to <c>RPC_AddFuel</c>. The RPCs go to the owner, this machine, and are handled at once:
    /// the game's allowed-item check, the ZDO write and one added effect per unit, as for a player feeding by hand.
    /// A station therefore fills at one item and one fuel a second and then takes one of each as it works them off.
    /// </summary>
    public static class FeedTick
    {
        public static void Tick(Smelter station)
        {
            bool ore = OreRoom(station), fuel = FuelRoom(station);
            if ((!ore && !fuel) || Player.m_localPlayer == null || !TakeRetry.Due(station))
                return;
            if (!ReachRules.StationRuleFor(station).Enabled)
            {
                TakeRetry.Later(station);
                return;
            }
            List<Container> near = ContainerScan.Nearby(FeedStations.Outline(station), FeedSettings.AutoFeedRange.Value, ContainerUse.Reach);
            bool fed = ore && AddOre(station, near);
            fed |= fuel && AddFuel(station, near);
            if (!fed)
                TakeRetry.Later(station);
        }

        private static bool OreRoom(Smelter station) => station.m_maxOre > 0 && station.GetQueueSize() < station.m_maxOre;

        private static bool FuelRoom(Smelter station)
        {
            return station.m_maxFuel > 0 && station.m_fuelItem != null && station.GetFuel() <= station.m_maxFuel - 1;
        }

        private static bool AddOre(Smelter station, List<Container> near)
        {
            ItemDrop.ItemData taken = null;
            if (NearbyTake.Take(near, FeedSkip.Without(StationAccepts.SmelterOre(station)), 1, (item, count) => taken = item, FeedSettings.AutoFeedLeave.Value) < 1)
                return false;
            int before = station.GetQueueSize();
            string name = ItemNames.PrefabName(taken);
            station.m_nview.InvokeRPC("RPC_AddOre", name, taken.m_cheated);
            Report(station, name, before, station.GetQueueSize());
            return true;
        }

        private static bool AddFuel(Smelter station, List<Container> near)
        {
            Func<ItemDrop.ItemData, bool> accepts = FeedSkip.Without(StationAccepts.Fuel(station, station.m_fuelItem));
            if (NearbyTake.Take(near, accepts, 1, keep: FeedSettings.AutoFeedLeave.Value) < 1)
                return false;
            float before = station.GetFuel();
            station.m_nview.InvokeRPC("RPC_AddFuel");
            Report(station, station.m_fuelItem.name, before, station.GetFuel());
            return true;
        }

        /// <summary>A warning when the game's count did not go up by one (nothing is built or logged per unit fed).</summary>
        private static void Report(Smelter station, string item, float before, float after)
        {
            if (after + 0.01f >= before + 1f)
                return;
            string name = Utils.GetPrefabName(station.m_nview.gameObject);
            Plugin.Log.LogWarning($"OpenKeep: {name} took one {item} from containers but the game did not add it ({before:0.##} before, {after:0.##} after)");
        }
    }
}
