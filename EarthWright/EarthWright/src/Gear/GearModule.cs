using EarthWright.Core;
using SyncedConfig;

namespace EarthWright.Gear
{
    /// <summary>
    /// Entry point of the Gear module (section "13. Tools"): the craftable shovel, the upgrade levels of the hoe,
    /// cultivator and shovel (with the level caps and unlocks the Brush and Paths modules read), and what holding a
    /// terrain tool gives: reach, a light every player sees, movement speed and a torch in the left hand.
    /// </summary>
    public static class GearModule
    {
        public static void Initialize(SyncedConfiguration synced)
        {
            GearSettings.Bind(synced);
            GearWords.Register();
            LocalTool.AddTool(ToolNames.Shovel);
            LevelCaps.Install();
            LevelGate.Install();
            // Logged, never thrown: a failure here must not break the game's own database wake-up.
            GameReady.OnObjectDb(200, "EarthWright shovel", db => Safe.Run("EarthWright shovel", () => GearRegistration.OnObjectDb(db)));
            GameReady.OnObjectDb(210, "EarthWright tool levels", db => Safe.Run("EarthWright tool levels", () => ToolLevels.ApplyAll(db)));
            GameReady.OnScene(200, "EarthWright shovel", scene => Safe.Run("EarthWright shovel", () => GearRegistration.OnScene(scene)));
            GearRegistration.WatchSettings();
            Ticker.OnUpdate("EarthWright held tool", HeldTool.Update);
            Ticker.OnUpdate("EarthWright reach", Reach.Update);
            Ticker.OnUpdate("EarthWright tool lights", ToolLights.Update);
        }
    }
}
