using SyncedConfig;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// Entry point of section 8, the base-life tweaks outside storage: several beds with respawn at the nearest
    /// (Bed*), pieces built on wooden floors (Fire*), honey per day (Hive*), fires refilling themselves from nearby
    /// containers (Fuel*), torches lit only at night (Torch*), smelters and kilns feeding themselves from the
    /// containers beside them (Feed*), Rested sooner (Rest*) and area repair with the hammer (Repair*). Fuel and Feed
    /// share <see cref="NearbyTake"/> and <see cref="TakeRetry"/>. Each feature binds its own keys.
    /// </summary>
    public static class HomesteadModule
    {
        public const string Section = "8. Homestead";

        public static void Initialize(SyncedConfiguration synced)
        {
            BedFeature.Initialize(synced);
            FireFeature.Initialize(synced);
            HiveFeature.Initialize(synced);
            FuelFeature.Initialize(synced);
            TorchFeature.Initialize(synced);
            FeedFeature.Initialize(synced);
            RestFeature.Initialize(synced);
            RepairFeature.Initialize(synced);
        }
    }
}
