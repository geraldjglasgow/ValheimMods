using System;
using System.Collections.Generic;

namespace EarthWright.Core
{
    /// <summary>
    /// Runs a callback and logs instead of throwing, for hooks where one broken handler must not stop the others
    /// (guards, events, per-frame callbacks). Unlike PatchGuard's Guard.Run, which rethrows, this swallows. A context
    /// that failed is logged once and then stays quiet for five seconds, so a broken per-frame hook does not flood the
    /// log. The overloads with arguments take a lambda that captures nothing (the compiler keeps one delegate for it),
    /// so a hot patch allocates no closure.
    /// </summary>
    public static class Safe
    {
        private const int QuietMilliseconds = 5000;

        private static readonly Dictionary<string, int> lastLogged = new Dictionary<string, int>();

        public static void Run(string context, Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Error(context, e);
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
                Error(context, e);
                return fallback;
            }
        }

        public static T Call<TArg, T>(string context, Func<TArg, T> func, TArg arg, T fallback)
        {
            try
            {
                return func(arg);
            }
            catch (Exception e)
            {
                Error(context, e);
                return fallback;
            }
        }

        public static T Call<TArg1, TArg2, T>(string context, Func<TArg1, TArg2, T> func, TArg1 first, TArg2 second, T fallback)
        {
            try
            {
                return func(first, second);
            }
            catch (Exception e)
            {
                Error(context, e);
                return fallback;
            }
        }

        public static void Run<TArg1, TArg2>(string context, Action<TArg1, TArg2> action, TArg1 first, TArg2 second)
        {
            try
            {
                action(first, second);
            }
            catch (Exception e)
            {
                Error(context, e);
            }
        }

        /// <summary>Logs the failure unless the same context failed in the last five seconds.</summary>
        public static void Error(string context, Exception e)
        {
            int now = Environment.TickCount;
            lock (lastLogged)
            {
                if (lastLogged.TryGetValue(context, out int last) && now - last < QuietMilliseconds)
                    return;
                lastLogged[context] = now;
            }
            Plugin.Log.LogError($"{context}: {e}");
        }
    }
}
