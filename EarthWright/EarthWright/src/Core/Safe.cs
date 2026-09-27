using System;

namespace EarthWright.Core
{
    /// <summary>
    /// Runs a callback and logs instead of throwing, for hooks where one broken handler must not stop the others
    /// (guards, events, per-frame callbacks). Unlike PatchGuard's Guard.Run, which rethrows, this swallows.
    /// </summary>
    public static class Safe
    {
        public static void Run(string context, Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"{context}: {e}");
            }
        }

        public static T Call<T>(string context, Func<T> func, T fallback)
        {
            try
            {
                return func();
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"{context}: {e}");
                return fallback;
            }
        }
    }
}
