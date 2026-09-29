using System;
using PatchGuard;

namespace EliteCreaturesPack.Core
{
    /// <summary>
    /// For patches that run inside a game call the mod must never break - a chest filling, a creature spawning, a hit
    /// landing. A failure is reported under the mod's name and swallowed, so the game's own step still happens and the
    /// mod's part is simply skipped.
    /// </summary>
    internal static class SafeCall
    {
        public static void Run(string context, Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Guard.Report(e, context);
            }
        }
    }
}
