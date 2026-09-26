using EliteCreaturesReborn.Util;
using PatchGuard;

namespace EliteCreaturesReborn.Runtime
{
    /// <summary>
    /// Tells every player when the world tier rises, because a world that silently got harder looks like bad luck. The
    /// server decides: the game routes every new global key to it, and it compares the tier before and after the key
    /// lands. A rise goes out over the game's routed-RPC bus to <c>Everybody</c>, the host included, and each client
    /// shows it on its own screen. A tier that falls (an admin's removekey) is not announced; `elite tier` reports it.
    /// </summary>
    public static class TierAnnounce
    {
        public const string Rpc = "ecr_tier";

        private static ZRoutedRpc? _registeredOn;

        /// <summary>Registers the handler once per routed-RPC bus, so leaving a world and joining another re-registers.</summary>
        public static void EnsureRegistered()
        {
            ZRoutedRpc bus = ZRoutedRpc.instance;
            if (bus == null || ReferenceEquals(bus, _registeredOn))
            {
                return;
            }
            _registeredOn = bus;
            bus.Register<int, int>(Rpc, OnTier);
        }

        /// <summary>Server side, after a global key has landed: announce when it lifted the tier above <paramref name="before"/>.</summary>
        public static void IfRisen(int before)
        {
            int after = WorldTier.Current();
            if (after <= before || ZRoutedRpc.instance == null)
            {
                return;
            }
            EnsureRegistered();
            Log.Info($"world tier rose from {before} to {after}");
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, Rpc, after, WorldTier.Ceiling());
        }

        private static void OnTier(long sender, int tier, int ceiling) =>
            Guard.Run("TierAnnounce.OnTier", () => Show(tier, ceiling));

        // A dedicated server has no HUD and simply skips this.
        private static void Show(int tier, int ceiling)
        {
            if (MessageHud.instance != null)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,
                    $"The world hardens: tier {tier} of {ceiling}");
            }
        }
    }
}
