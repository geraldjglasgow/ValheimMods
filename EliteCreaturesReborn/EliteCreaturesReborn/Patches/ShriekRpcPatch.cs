using EliteCreaturesReborn.Mutations;
using HarmonyLib;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// World start, on every machine: the shriek's deafen message is registered before any Screecher can send it, so a
    /// player is reached even when their machine has not yet set up the creature that shrieked at them.
    /// </summary>
    [HarmonyPatch(typeof(ZoneSystem), "Start")]
    public static class ShriekRpcPatch
    {
        private static void Postfix() => SafeCall.Run("ZoneSystem.Start screecher deafen", ShriekRpc.EnsureRegistered);
    }
}
