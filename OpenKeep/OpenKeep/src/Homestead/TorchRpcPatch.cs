using HarmonyLib;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// <c>Fireplace.Awake</c> (every client and the server, once per fire instance, right after the game registers its
    /// own fire RPCs): registers <see cref="TorchKeep.RpcName"/> on the fire's net view, on every fire, since Torch Pieces
    /// may change while the fire is loaded.
    /// </summary>
    [HarmonyPatch(typeof(Fireplace), nameof(Fireplace.Awake))]
    public static class TorchRpcPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Fireplace __instance) => TorchKeep.Register(__instance);
    }
}
