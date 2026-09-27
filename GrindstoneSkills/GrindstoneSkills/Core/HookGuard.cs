using System;
using PatchGuard;

namespace GrindstoneSkills
{
    /// <summary>
    /// Runs one feature's hook from inside the game's own code (a tree falling, a log or a rock chunk breaking, a
    /// swing), where an exception would abort the rest of the game's method: a tree that never drops, a chunk that
    /// never spawns its ore. A failing hook is logged under GrindstoneSkills and swallowed, so the other features and
    /// the game carry on. Shared by the Woodcutting and Pickaxes modules; it runs on whichever machine calls it.
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
    }
}
