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
    /// work and the HUD run in <see cref="BlueprintRunner"/>.
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
            OpenKeep.BuildCamera.CameraArea.AroundPlayer = () => BlueprintCamera.EntryOut;
            Plugin.Instance.gameObject.AddComponent<BlueprintRunner>();
        }
    }
}
