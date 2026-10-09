using SyncedConfig;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// Entry point of section 14, Blueprints (moved here from EarthWright on 2026-10-05): a Blueprints tab in the game
    /// hammer's build menu (<see cref="BlueprintTab"/>) with the tools and the saved builds in
    /// BepInEx/config/OpenKeep.Blueprints and its folders (DevBridge's blueprint JSON); placing one clears the site, cuts
    /// hills and fills dips, digs water areas below sea level, lays dirt under the building and places every piece;
    /// plus 'openkeep blueprint' to list, save and undo. One synced switch,
    /// <see cref="BlueprintSettings.Enabled"/>, off by default. The patches are ordinary patch classes; the per-frame
    /// work runs in <see cref="BlueprintRunner"/> (idle on a dedicated server and, once settled, while off) and the HUD in
    /// <see cref="BlueprintGui"/>, switched on only while it has something to draw.
    /// </summary>
    public static class BlueprintsModule
    {
        public static void Initialize(SyncedConfiguration synced)
        {
            BlueprintSettings.Bind(synced);
            BlueprintWords.Register();
            Sites.SitesModule.Initialize(synced);
            Planner.PlannerModule.Initialize(synced);
            Copy.CopyModule.Initialize(synced);
            Bench.BenchModule.Initialize();
            OpenKeep.BuildCamera.CameraArea.AroundPlayer = () => BlueprintCamera.EntryOut;
            Plugin.Instance.gameObject.AddComponent<BlueprintRunner>();
        }
    }
}
