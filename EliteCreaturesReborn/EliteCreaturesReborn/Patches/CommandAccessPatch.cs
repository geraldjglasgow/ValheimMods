using EliteCreaturesReborn.Commands;
using HarmonyLib;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// World start, on every machine: the admin-check messages are registered before anyone can type a command. The
    /// server answers the question and the client receives the answer, so both ends need the handlers.
    /// </summary>
    [HarmonyPatch(typeof(ZoneSystem), "Start")]
    public static class CommandAccessPatch
    {
        private static void Postfix() => SafeCall.Run("ZoneSystem.Start admin check", CommandAccess.EnsureRegistered);
    }
}
