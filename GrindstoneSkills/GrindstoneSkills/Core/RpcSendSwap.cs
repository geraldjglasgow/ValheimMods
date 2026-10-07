using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// The one patch on ZNetView.InvokeRPC(string, object[]), through which every object's RPCs leave every machine.
    /// <see cref="FermenterAddSend"/> swaps the game's send of a mead base into a fermenter for its own (with the cook's
    /// level) and tests its open add first, so any other send costs one test. A swapped send skips the game's.
    /// </summary>
    [HarmonyPatch(typeof(ZNetView), nameof(ZNetView.InvokeRPC), typeof(string), typeof(object[]))]
    public static class RpcSendSwap
    {
        [HarmonyPrefix]
        private static bool Prefix(ZNetView __instance, string method, object[] parameters) =>
            !FermenterAddSend.Swapped(__instance, method, parameters);
    }
}
