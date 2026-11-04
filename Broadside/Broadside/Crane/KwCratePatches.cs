using HarmonyLib;
using PatchGuard;

namespace Broadside
{
    /// <summary>
    /// A crate on a crane's hook, or falling to break (<see cref="KwCrateBreak"/>), is shut: nobody opens it, stacks into it or
    /// takes from it (refused where the player clicks, and again by the crate's owner, who would otherwise hand the crate
    /// over to them), and the hammer cannot remove it. The same use patch serves the cart (<c>Cart/</c>): the game's alternate
    /// use on a crate riding a cart unloads it (<see cref="KwCartUnload.Asked"/>), and use on an empty cart's storage is the
    /// cart's own (<see cref="KwCartStore.Forward"/>). Every other chest is left as the game has it.
    /// </summary>
    public static class KwCratePatches
    {
        /// <summary>Whether this chest is a crate the crane lifts (the cargo crate or a storage crate) that a crane names as its own, or that falls to break.</summary>
        public static bool Hooked(Container container)
        {
            KwCrate? crate = container.GetComponent<KwCrate>();
            return crate != null && (!crate.Crane.IsNone() || KwCrateBreak.Pending(crate));
        }

        // On the owner: a request to open, stack or take all is answered "no" while the crate hangs.
        internal static bool Refuse(Container container, long uid, string response)
        {
            if (!Guard.Run("Broadside crate refuse", () => Hooked(container)))
                return true;
            container.m_nview.InvokeRPC(uid, response, false);
            return false;
        }

        [HarmonyPatch(typeof(Container), nameof(Container.Interact))]
        private static class Open
        {
            [HarmonyPrefix]
            private static bool Prefix(Container __instance, Humanoid character, bool hold, bool alt, ref bool __result)
            {
                if (KwCartStore.Forward(__instance, character, hold, alt, out bool used))
                {
                    __result = used;
                    return false;
                }
                if (!Guard.Run("Broadside crate open", () => Hooked(__instance) || KwCartUnload.Asked(__instance, character, hold, alt)))
                    return true;
                if (Hooked(__instance))
                    character.Message(MessageHud.MessageType.Center, __instance.GetComponent<KwCrate>().Crane.IsNone() ? KwCartText.Falling : KwCraneText.Hanging);
                __result = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(Container), nameof(Container.RPC_RequestOpen))]
        private static class RequestOpen
        {
            [HarmonyPrefix]
            private static bool Prefix(Container __instance, long uid) => Refuse(__instance, uid, "RPC_OpenResponse");
        }

        [HarmonyPatch(typeof(Container), nameof(Container.RPC_RequestStack))]
        private static class RequestStack
        {
            [HarmonyPrefix]
            private static bool Prefix(Container __instance, long uid) => Refuse(__instance, uid, "RPC_StackResponse");
        }

        [HarmonyPatch(typeof(Container), nameof(Container.RPC_RequestTakeAll))]
        private static class RequestTakeAll
        {
            [HarmonyPrefix]
            private static bool Prefix(Container __instance, long uid) => Refuse(__instance, uid, "RPC_TakeAllResponse");
        }

        [HarmonyPatch(typeof(Piece), nameof(Piece.CanBeRemoved))]
        private static class Remove
        {
            [HarmonyPostfix]
            private static void Postfix(Piece __instance, ref bool __result)
            {
                if (__result && __instance.GetComponent<KwCrate>() is KwCrate crate && (!crate.Crane.IsNone() || KwCrateBreak.Pending(crate)))
                    __result = false;
            }
        }
    }
}
