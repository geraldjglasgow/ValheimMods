using System;
using PatchGuard;

namespace GrindstoneSkills
{
    /// <summary>
    /// Runs one Woodcutting feature's hook from inside the game's own tree and log code, where an exception would abort
    /// the rest of the game's method (a tree that never drops, a log that never breaks). A failing hook is logged under
    /// GrindstoneSkills and swallowed, so the other features and the game carry on.
    /// </summary>
    public static class WoodGuard
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
    }
}
