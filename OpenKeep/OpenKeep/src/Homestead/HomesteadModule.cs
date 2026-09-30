using SyncedConfig;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// Entry point of section 8, the base-life tweaks outside storage: several beds with respawn at the nearest or
    /// the one chosen on the map, sooner the nearer (Bed*), portal jumps quicker the nearer (Portal*), pieces built on
    /// wooden floors (Fire*), honey per day (Hive*), fires refilling themselves from nearby containers (Fuel*), torches
    /// lit only at night (Torch*), smelters and kilns feeding themselves from the containers beside them (Feed*),
    /// Rested sooner (Rest*), area repair with the hammer (Repair*), gear repaired on opening a crafting station
    /// (StationRepair*) and tamed animals eating from containers (Pet*). Bed and Portal share <see cref="QuickWait"/>;
    /// Fuel and Feed share <see cref="NearbyTake"/> and <see cref="TakeRetry"/>; Pet takes through
    /// <see cref="NearbyTake"/> too. Each feature binds its own keys.
    /// </summary>
    public static class HomesteadModule
    {
        public const string Section = "8. Homestead";

        public static void Initialize(SyncedConfiguration synced)
        {
            BedFeature.Initialize(synced);
            PortalFeature.Initialize(synced);
            FireFeature.Initialize(synced);
            HiveFeature.Initialize(synced);
            FuelFeature.Initialize(synced);
            TorchFeature.Initialize(synced);
            FeedFeature.Initialize(synced);
            RestFeature.Initialize(synced);
            RepairFeature.Initialize(synced);
            StationRepairFeature.Initialize(synced);
            PetFeature.Initialize(synced);
        }
    }
}
