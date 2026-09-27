using EarthWright.Core;
using EarthWright.Terrain;
using SyncedConfig;

namespace EarthWright.Costs
{
    /// <summary>
    /// Entry point of the Costs module: stamina, tool wear, materials, stations, volume costs, the cooldown, free build
    /// and the live cost line, for terrain entries only. Costs are charged on the player's own machine (their stamina,
    /// tool and inventory); the rules are synced settings and EarthWright.Costs.yml, so the server decides them.
    /// Brush clicks are charged by the game's placement, adjusted by the patches of <see cref="PlacementCharges"/>, and
    /// checked by the sender guard of <see cref="ClickGuard"/>; special entries pay through <see cref="CostApi"/>.
    /// </summary>
    public static class CostsModule
    {
        public static void Initialize(SyncedConfiguration synced)
        {
            CostWords.Register();
            CostSettings.Bind(synced);
            MaterialSettings.Bind(synced);
            CostOverrides.Register(synced);
            EditGuards.AddSender("costs", ClickGuard.Check);
            EditEvents.Sent += ClickGuard.OnSent;
            Ticker.OnUpdate("EarthWright free build key", FreeBuild.Tick);
            Ticker.OnUpdate("EarthWright cost line", CostDisplay.Tick);
        }
    }
}
