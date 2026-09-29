using EliteCreaturesPack.Core;
using HarmonyLib;

namespace EliteCreaturesPack.Mimic
{
    /// <summary>
    /// Where a crypt chest may turn out to be a mimic: the game fills a chest once, on its owner, when it first exists;
    /// a chest claimed for a mimic skips that fill (<see cref="CryptSwap.Claim"/>). A chest that wakes with a claimed but
    /// unfinished swap has it finished by its owner. Both run inside the chest's own wake-up, so a failure is reported
    /// and swallowed: the worst case is an ordinary chest.
    /// </summary>
    [HarmonyPatch(typeof(Container), "AddDefaultItems")]
    public static class CryptChestPatch
    {
        private static bool Prefix(Container __instance)
        {
            bool claimed = false;
            SafeCall.Run("Container.AddDefaultItems mimic", () => claimed = CryptSwap.Claim(__instance));
            return !claimed;
        }
    }

    [HarmonyPatch(typeof(Container), "Awake")]
    public static class CryptChestAwakePatch
    {
        private static void Postfix(Container __instance) =>
            SafeCall.Run("Container.Awake mimic", () => CryptSwap.Resume(__instance));
    }
}
