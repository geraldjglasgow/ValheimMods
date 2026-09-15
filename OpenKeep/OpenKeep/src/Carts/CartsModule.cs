using OpenKeep.Core;
using PatchGuard;
using SyncedConfig;

namespace OpenKeep.Carts
{
    /// <summary>Entry point of the Carts module: binds its settings, registers its language words and applies setting changes to loaded carts.</summary>
    public static class CartsModule
    {
        public static void Initialize(SyncedConfiguration synced)
        {
            CartsSettings.Initialize(synced);
            Language.Add("ok_cartcraft", "Craft at the cart");
            CartsSettings.CartWorkbench.SettingChanged += (_, _) => Guard.Run("cart workbench switch", CartStation.ApplyAll);
            CartsSettings.CartStationRange.SettingChanged += (_, _) => Guard.Run("cart station range", CartStation.ApplyAll);
        }
    }
}
