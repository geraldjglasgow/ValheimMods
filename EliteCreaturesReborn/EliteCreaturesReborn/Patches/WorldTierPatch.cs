using EliteCreaturesReborn.Runtime;
using HarmonyLib;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// The server's handler for a new global key - every boss defeat arrives here, from whichever machine owned the
    /// boss. The tier is read before and after the key lands and a rise is announced. It runs inside the game's own key
    /// handling, so a failure here is reported under the mod's name and swallowed rather than losing the key.
    /// </summary>
    [HarmonyPatch(typeof(ZoneSystem), "RPC_SetGlobalKey")]
    public static class TierRisePatch
    {
        private static void Prefix(out int __state)
        {
            int before = 0;
            SafeCall.Run("ZoneSystem.RPC_SetGlobalKey tier before", () => before = WorldTier.Current());
            __state = before;
        }

        private static void Postfix(int __state) =>
            SafeCall.Run("ZoneSystem.RPC_SetGlobalKey tier after", () => TierAnnounce.IfRisen(__state));
    }

    /// <summary>
    /// World start, on every machine: the tier message's handler is registered before any boss can fall, and a listed
    /// boss key that no known boss sets is logged once.
    /// </summary>
    [HarmonyPatch(typeof(ZoneSystem), "Start")]
    public static class TierStartPatch
    {
        private static void Postfix() => SafeCall.Run("ZoneSystem.Start tiers", Start);

        private static void Start()
        {
            TierAnnounce.EnsureRegistered();
            WorldTier.WarnUnknownKeys();
        }
    }
}
