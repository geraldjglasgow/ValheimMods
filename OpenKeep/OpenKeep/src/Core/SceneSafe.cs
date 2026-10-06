using System;

namespace OpenKeep.Core
{
    /// <summary>
    /// Runs work that must never throw into the game's method: the ZNetScene.Awake postfixes. A throw there leaves the
    /// network scene disabled, nothing spawns and the world loads for ever (the trace only in Player.log), so a failure
    /// is logged under OpenKeep and swallowed; the feature it belongs to is then missing for this world.
    /// </summary>
    public static class SceneSafe
    {
        public static void Run(string what, Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"OpenKeep: {what} failed while the scene loaded: {e}");
            }
        }
    }
}
