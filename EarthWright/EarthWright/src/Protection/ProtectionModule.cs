using EarthWright.Core;
using EarthWright.Terrain;
using SyncedConfig;

namespace EarthWright.Protection
{
    /// <summary>
    /// Entry point of the Protection module (section "11. Protection"): who may change which ground.
    /// <list type="bullet">
    /// <item>Guards on the sender and on the owner of the ground: terrain tools allowed, admin-only entries, the terrain
    /// lock, combat (sender only), no-build places and dungeons, wards over the whole footprint, admin zones.</item>
    /// <item>The local player's own game terrain ops (pickaxe digging, the hoe with EarthWright off) under the lock and
    /// the tools switch (<see cref="GameOpLock"/>).</item>
    /// <item>Admin zones: EarthWright.Zones.yml on the server, pushed to every player, <c>ew zone</c> commands.</item>
    /// <item>Admins' edits routed through the server while a rule needs its approval, and the admin limit override key.</item>
    /// <item>Strict dig exceptions near ore, buried treasure and in tar, the preview status and the admin panel.</item>
    /// </list>
    /// Harmony patches in this folder apply themselves (Plugin patches every attributed class).
    /// </summary>
    public static class ProtectionModule
    {
        public static void Initialize(SyncedConfiguration synced)
        {
            ProtectionSettings.Bind(synced);
            ProtectionWords.Register();
            ZoneBook.Initialize(synced);
            ZoneCommands.Register();
            ProtectionGuards.Register();
            EditEvents.Building += edit => Safe.Run("EarthWright admin routing", () => AdminRouting.OnBuilding(edit));
            DigExceptions.Initialize();
            HeightLimits.AddDigException(DigExceptions.Lifts);
            ProtectionPanel.Register();
            Ticker.OnUpdate("EarthWright protection preview", ProtectionPreview.Tick);
        }
    }
}
