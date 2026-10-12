using System;
using System.Collections.Generic;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Runs a part of the casters that must never break the game's own code around it (a HUD update, an RPC, a cast): an
    /// exception is logged once per place (so a broken part does not flood the log every frame) and swallowed.
    /// </summary>
    public static class CasterSafe
    {
        private static readonly HashSet<string> reported = new HashSet<string>();

        public static void Run(string where, Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Report(where, e);
            }
        }

        /// <summary>The same with an argument, for a hot path: a cached delegate allocates nothing per call.</summary>
        public static void Run<TArg>(string where, Action<TArg> action, TArg arg)
        {
            try
            {
                action(arg);
            }
            catch (Exception e)
            {
                Report(where, e);
            }
        }

        public static T Run<T>(string where, Func<T> func)
        {
            try
            {
                return func();
            }
            catch (Exception e)
            {
                Report(where, e);
                return default;
            }
        }

        private static void Report(string where, Exception e)
        {
            if (reported.Add(where))
                Plugin.Log.LogError($"EliteEquipment: casters, {where} failed (reported once): {e}");
        }
    }
}
