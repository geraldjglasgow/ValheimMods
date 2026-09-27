using EarthWright.Actions;
using EarthWright.Core;
using EarthWright.History;
using SyncedConfig;

namespace EarthWright.Paths
{
    /// <summary>
    /// Entry point of the Paths module: ramps and roads. Binds section "4. Ramps and Roads", registers the words, the
    /// "ramp" and "road" special actions (the Menu module makes their entries), the undo hook that removes the last
    /// ramp point or road waypoint before any terrain is undone, and the per-frame driver (keys, preview, HUD).
    /// </summary>
    public static class PathsModule
    {
        public static void Initialize(SyncedConfiguration synced)
        {
            PathSettings.Bind(synced);
            PathWords.Register();
            SpecialActions.Register(PathSelection.RampKey, RampTool.Instance);
            SpecialActions.Register(PathSelection.RoadKey, RoadTool.Instance);
            UndoHooks.AddFirst(() => Safe.Call("EarthWright paths undo", PathSession.RemoveLastPoint, false));
            Ticker.OnUpdate("EarthWright paths", PathSession.Tick);
        }
    }
}
