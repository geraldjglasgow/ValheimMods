using HarmonyLib;
using PatchGuard;

namespace GrindstoneSkills
{
    /// <summary>
    /// Cooking experience for a dish that was thrown away by a trash filter. The dish is never taken off, so the game's
    /// take-off experience would be lost; the station's owner sends it to the cook instead, by routed RPC
    /// (<see cref="Keys.RpcCookCredit"/>). An offline cook loses it.
    /// ZRoutedRpc.InvokeRoutedRPC to Everybody handles the call on the sender at once and routes it on; the server
    /// passes it to every peer except the sender, so each machine, the sender included, handles it exactly once. Only
    /// the client whose local player is the cook acts on it; a dedicated server has no local player and ignores it.
    /// The routed RPC is registered in a ZNet.Awake postfix, where the game creates a fresh ZRoutedRpc for each session.
    /// </summary>
    public static class CookCredit
    {
        /// <summary>The game's take-off experience at a cooking station (CookingStation.OnInteract).</summary>
        public const float TakeOffExperience = 0.6f;

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
        private static class NetAwake
        {
            [HarmonyPostfix]
            private static void Postfix() => ZRoutedRpc.instance?.Register<long, string>(Keys.RpcCookCredit, Receive);
        }

        /// <summary>Sends the take-off experience for <paramref name="dishPrefab"/> to the player with this ID, wherever they are.</summary>
        public static void Send(long cookPlayerId, string dishPrefab)
        {
            if (cookPlayerId == 0L || ZRoutedRpc.instance == null)
                return;
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, Keys.RpcCookCredit, cookPlayerId, dishPrefab ?? "");
        }

        private static void Receive(long sender, long cookPlayerId, string dishPrefab) =>
            Guard.Run("cook credit", () => Credit(cookPlayerId, dishPrefab));

        /// <summary>The take-off share times the Experience Multiplier and the dish's tier; no discovery bonus.</summary>
        private static void Credit(long cookPlayerId, string dishPrefab)
        {
            Player player = Player.m_localPlayer;
            if (player == null || player.GetPlayerID() != cookPlayerId)
                return;
            XpScaling.RaiseScaled(player, TakeOffExperience * XpScaling.Multiplier * XpScaling.Tier(dishPrefab));
        }
    }
}
