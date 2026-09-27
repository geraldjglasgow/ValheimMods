using EarthWright.Actions;
using EarthWright.Core;
using SyncedConfig;

namespace EarthWright.Extras
{
    /// <summary>
    /// Entry point of the Extras module: the cultivator extras (section "14. Cultivator": seed grid, cultivating ground the
    /// game refuses, the Uproot entry's action) and the road travel bonus (section "15. Road Travel").
    /// </summary>
    public static class ExtrasModule
    {
        public static void Initialize(SyncedConfiguration synced)
        {
            ExtrasSettings.Bind(synced);
            ExtrasWords.Register();
            SpecialActions.Register("uproot", new UprootAction());
            Ticker.OnUpdate("EarthWright road travel", RoadTravel.Update);
            Ticker.OnUpdate("EarthWright seed grid", SeedGrid.Update);
        }
    }
}
