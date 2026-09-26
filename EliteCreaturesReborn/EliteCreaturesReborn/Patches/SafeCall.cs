using System;
using PatchGuard;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// For patches that run inside a game call the mod must never break - an altar spending its offering, the server
    /// storing a global key, a creature giving birth. A failure is reported under the mod's name and swallowed, so the
    /// game's own step still happens and the mod's part is simply skipped.
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
