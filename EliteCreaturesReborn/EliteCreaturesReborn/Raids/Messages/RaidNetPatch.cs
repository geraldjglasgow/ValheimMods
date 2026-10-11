using EliteCreaturesReborn.Patches;
using HarmonyLib;

namespace EliteCreaturesReborn.Raids
{
    /// <summary>
    /// World start, on every machine: the raids' routed messages are registered before any raid can send one (the
    /// owner sends, every client shows, so every machine needs the handler).
    /// </summary>
    [HarmonyPatch(typeof(ZoneSystem), "Start")]
    internal static class RaidNetPatch
    {
        private static void Postfix() => SafeCall.Run("ZoneSystem.Start raid messages", RaidNet.EnsureRegistered);
    }
}
