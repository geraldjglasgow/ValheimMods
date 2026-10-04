using PatchGuard;

namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// The one message a Screecher's shriek sends each player it deafens, over the game's routed-RPC bus straight to
    /// that player's own machine (the owner of their character), never to everybody: a player's sound and spells exist
    /// only there, and no one else needs to hear of it. A dedicated server only passes it on. Registered once per bus at
    /// world start on every machine (<c>ShriekRpcPatch</c>), keyed on the bus instance so a new world re-registers.
    /// </summary>
    public static class ShriekRpc
    {
        public const string Deafen = "ecr_deafen";

        private static ZRoutedRpc? _registeredOn;

        /// <summary>Registers the handler on the current bus, once per bus; safe to call from anywhere.</summary>
        public static void EnsureRegistered()
        {
            ZRoutedRpc bus = ZRoutedRpc.instance;
            if (bus == null || ReferenceEquals(bus, _registeredOn))
            {
                return;
            }
            _registeredOn = bus;
            bus.Register<float>(Deafen, OnDeafen);
        }

        /// <summary>Creature-owner side: deafen <paramref name="player"/> for <paramref name="seconds"/> on their own machine.</summary>
        public static void Send(Player player, float seconds)
        {
            EnsureRegistered();
            long peer = player.GetOwner();
            if (ZRoutedRpc.instance == null || peer == ZRoutedRpc.Everybody)
            {
                return; // a character with no owner yet: 0 would address the whole server, so it is skipped
            }
            ZRoutedRpc.instance.InvokeRoutedRPC(peer, Deafen, seconds);
        }

        private static void OnDeafen(long sender, float seconds) =>
            Guard.Run("ShriekRpc.Deafen", () => ShriekDeafness.Apply(Player.m_localPlayer, seconds));
    }
}
