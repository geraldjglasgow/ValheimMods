using System;
using PatchGuard;

namespace Hearthhold
{
    /// <summary>
    /// Runs one feature's hook from inside the game's own code (a pick, a kill, a cooking station's update), where an
    /// exception would abort the rest of the game's method. A failing hook is logged under Hearthhold and swallowed, so
    /// the game carries on with a plain item.
    /// </summary>
    public static class HookGuard
    {
        public static void Run(string context, Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                Guard.Report(exception, context);
            }
        }

        public static T Run<T>(string context, Func<T> func, T fallback)
        {
            try
            {
                return func();
            }
            catch (Exception exception)
            {
                Guard.Report(exception, context);
                return fallback;
            }
        }

        /// <summary>Run with the hook's argument passed in: a static lambda captures nothing and allocates nothing.</summary>
        public static void Run<TArg>(string context, Action<TArg> action, TArg arg)
        {
            try
            {
                action(arg);
            }
            catch (Exception exception)
            {
                Guard.Report(exception, context);
            }
        }
    }
}
