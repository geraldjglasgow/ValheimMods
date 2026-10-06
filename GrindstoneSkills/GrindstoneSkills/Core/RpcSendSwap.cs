using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// The one patch on ZNetView.InvokeRPC(string, object[]), through which every object's RPCs leave every machine.
    /// The features that swap one of the game's sends for their own (a starred item into a mill, <see cref="MillSend"/>;
    /// a mead base into a fermenter, <see cref="FermenterAddSend"/>) each test their open add first, so any other send
    /// costs one test per feature. A swapped send skips the game's.
    /// </summary>
    [HarmonyPatch(typeof(ZNetView), nameof(ZNetView.InvokeRPC), typeof(string), typeof(object[]))]
    public static class RpcSendSwap
    {
        [HarmonyPrefix]
        private static bool Prefix(ZNetView __instance, string method, object[] parameters) =>
            !MillSend.Swapped(__instance, method, parameters) && !FermenterAddSend.Swapped(__instance, method, parameters);
    }
}
