using EliteCreaturesReborn.Traits;
using PatchGuard;

namespace EliteCreaturesReborn.Tally
{
    /// <summary>
    /// Puts one hit's health loss on the right boss's board, from the owner of whatever was hit. A hit on the boss is
    /// written straight into its own tally. A hit on one of a Phantom boss's copies counts on that boss's board too, at
    /// the hit, so a copy killed early still counts - but the copy may be owned by another machine than its boss, and
    /// only the boss's owner may write the boss's ZDO. So the credit goes to the machine that owns the boss's ZDO, or,
    /// when this machine does not have that ZDO, to the server, which hands it on once to the owner it knows. A credit
    /// that lands on a machine that has just given the boss away is dropped: one hit uncounted, never counted twice.
    /// </summary>
    internal static class BossCredit
    {
        public const string Rpc = "ecr_boss_credit";

        private static ZRoutedRpc? _registeredOn;

        /// <summary>Registers the handler once per routed-RPC bus. Called at world start on every machine.</summary>
        public static void EnsureRegistered()
        {
            ZRoutedRpc bus = ZRoutedRpc.instance;
            if (bus == null || ReferenceEquals(bus, _registeredOn))
            {
                return;
            }
            _registeredOn = bus;
            bus.Register<ZPackage>(Rpc, OnCredit);
        }

        /// <summary>On the owner of the character hit: the boss itself, or one of its Phantom copies.</summary>
        public static void Add(ZDO hit, Player player, float amount)
        {
            ZDOID boss = AspectStore.GetPhantomOf(hit);
            if (boss == ZDOID.None)
            {
                DamageTally.Add(hit, player.GetPlayerID(), player.GetPlayerName(), amount);
                return;
            }
            Route(boss, player.GetPlayerID(), player.GetPlayerName(), amount);
        }

        /// <summary>
        /// On the owner of a Tethered boss as it falls first: its whole tally goes on to its partner's board, the same way
        /// a copy's hits reach their boss, and its own is emptied so nothing counts twice.
        /// </summary>
        public static void HandOver(ZDO fallen, ZDOID partner)
        {
            foreach (DamageTally.Entry entry in DamageTally.Load(fallen))
            {
                Route(partner, entry.Id, entry.Name, entry.Damage);
            }
            DamageTally.Clear(fallen);
        }

        private static void Route(ZDOID boss, long playerId, string playerName, float amount)
        {
            ZDO? zdo = ZDOMan.instance?.GetZDO(boss);
            if (zdo != null && zdo.IsOwner())
            {
                DamageTally.Add(zdo, playerId, playerName, amount);
                return;
            }
            ZRoutedRpc bus = ZRoutedRpc.instance;
            long owner = zdo != null ? zdo.GetOwner() : 0L;
            if (bus == null)
            {
                return;
            }
            if (owner != 0L)
            {
                bus.InvokeRoutedRPC(owner, Rpc, Pack(boss, playerId, playerName, amount));
                return;
            }
            bus.InvokeRoutedRPC(Rpc, Pack(boss, playerId, playerName, amount));
        }

        private static ZPackage Pack(ZDOID boss, long playerId, string playerName, float amount)
        {
            ZPackage pkg = new ZPackage();
            pkg.Write(boss);
            pkg.Write(playerId);
            pkg.Write(playerName);
            pkg.Write(amount);
            return pkg;
        }

        private static void OnCredit(long sender, ZPackage pkg) => Guard.Run("BossCredit.OnCredit", () => Receive(pkg));

        // The boss's owner adds it. Only the server hands it on, and only to another machine, so it never goes round.
        private static void Receive(ZPackage pkg)
        {
            ZDOID boss = pkg.ReadZDOID();
            long playerId = pkg.ReadLong();
            string playerName = pkg.ReadString();
            float amount = pkg.ReadSingle();
            ZDO? zdo = ZDOMan.instance?.GetZDO(boss);
            if (zdo == null)
            {
                return;
            }
            if (zdo.IsOwner())
            {
                DamageTally.Add(zdo, playerId, playerName, amount);
                return;
            }
            long owner = zdo.GetOwner();
            if (ZNet.instance != null && ZNet.instance.IsServer() && owner != 0L && owner != ZDOMan.GetSessionID())
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(owner, Rpc, Pack(boss, playerId, playerName, amount));
            }
        }
    }
}
