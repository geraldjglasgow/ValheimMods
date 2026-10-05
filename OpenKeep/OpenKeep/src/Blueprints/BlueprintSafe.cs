using System;

namespace OpenKeep.Blueprints
{
    /// <summary>Runs per-frame and callback work of the blueprint feature so one failure is logged and the game carries on.</summary>
    public static class BlueprintSafe
    {
        public static void Run(string what, Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"{what}: {e}");
            }
        }

        public static T Call<T>(string what, Func<T> func, T fallback)
        {
            try
            {
                return func();
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"{what}: {e}");
                return fallback;
            }
        }
    }
}
