using HarmonyLib;
using OpenKeep.Blueprints.Sites;
using SyncedConfig;
using WindowInput;

namespace OpenKeep.Blueprints.Planner
{
    /// <summary>
    /// Entry point of the Site planner (SPEC-Blueprints.md, section 2): with the hammer's Blueprints tab Site planner entry
    /// selected, the crosshair picks ghost pieces of construction sites; a click selects a piece, Shift + click the
    /// whole house around it, Enter queues the selection as the site's next build order, Backspace clears it and K
    /// opens the queue panel. Registers the words, the click, the per-frame and OnGUI work, the queue RPC on every site
    /// marker and the panel as a game window (WindowInput: free cursor, no attacks or mouse look, Esc closes). No
    /// settings of its own: the keys are fixed (<see cref="PlannerKeys"/>), the switch is the Blueprints one.
    /// </summary>
    public static class PlannerModule
    {
        public static void Initialize(SyncedConfiguration synced)
        {
            PlannerWords.Register();
            SiteHooks.PlannerClick = PlannerClicks.OnClick;
            SiteHooks.OnUpdate("OpenKeep site planner", PlannerSession.Tick);
            SiteHooks.OnGui("OpenKeep site planner panel", PlannerPanel.OnGui);
            SiteHooks.MarkerCreated += QueueRpc.Register;
            // The library's patches go in by hand (no attributes), once per merged copy; its own Harmony id keeps them apart.
            GameWindow.Install(new Harmony(Plugin.PluginGuid + ".planner"));
            GameWindow.Add(() => PlannerPanel.Showing, PlannerPanel.Close);
        }
    }
}
