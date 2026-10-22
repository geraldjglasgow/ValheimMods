using PatchGuard;
using SyncedConfig;

namespace OpenKeep.Carts
{
    /// <summary>Entry point of the Carts module: binds its settings and applies setting changes to loaded carts.</summary>
    public static class CartsModule
    {
        public static void Initialize(SyncedConfiguration synced)
        {
            CartsSettings.Initialize(synced);
            CartsSettings.CartWorkbench.SettingChanged += (_, _) => Guard.Run("cart workbench switch", CartStation.ApplyAll);
            CartsSettings.CartStationRange.SettingChanged += (_, _) => Guard.Run("cart station range", CartStation.ApplyAll);
        }
    }
}
