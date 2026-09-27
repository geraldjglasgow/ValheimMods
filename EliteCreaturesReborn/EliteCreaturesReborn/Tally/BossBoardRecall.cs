using System;
using PatchGuard;

namespace EliteCreaturesReborn.Tally
{
    /// <summary>
    /// Fetches the latest boss board from the server, for a player whose own machine has none - one who joined after the
    /// kill. The server received every board's broadcast and keeps the latest (<see cref="BossBoard"/>); the player asks
    /// it, and it answers the asking player alone, with its board or with none. A board that comes back is kept here as
    /// the latest too, unless one arrived from a boss's death while the question was out.
    /// </summary>
    internal static class BossBoardRecall
    {
        public const string Ask = "ecr_boss_board_ask";
        public const string Answer = "ecr_boss_board_answer";

        private static ZRoutedRpc? _registeredOn;
        private static Action<BossBoard?>? _pending;

        /// <summary>Registers both handlers once per routed-RPC bus. Called at world start on every machine.</summary>
        public static void EnsureRegistered()
        {
            ZRoutedRpc bus = ZRoutedRpc.instance;
            if (bus == null || ReferenceEquals(bus, _registeredOn))
            {
                return;
            }
            _registeredOn = bus;
            _pending = null;
            bus.Register(Ask, OnAsk);
            bus.Register<ZPackage>(Answer, OnAnswer);
        }

        /// <summary>
        /// Player side: asks the server and hands its answer (null for none) to <paramref name="done"/>. False, asking
        /// nothing, when this machine is the server itself or no world is loaded: then there is nobody else to ask.
        /// </summary>
        public static bool Request(Action<BossBoard?> done)
        {
            if (ZNet.instance == null || ZNet.instance.IsServer() || ZRoutedRpc.instance == null)
            {
                return false;
            }
            EnsureRegistered();
            _pending = done;
            ZRoutedRpc.instance.InvokeRoutedRPC(Ask);
            return true;
        }

        private static void OnAsk(long sender) => Guard.Run("BossBoardRecall.OnAsk", () => Reply(sender));

        // Server side: the answer goes to the asking player alone.
        private static void Reply(long sender)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer())
            {
                return;
            }
            BossBoard? board = BossBoard.Latest;
            ZPackage pkg = new ZPackage();
            pkg.Write(board != null);
            board?.Write(pkg);
            ZRoutedRpc.instance.InvokeRoutedRPC(sender, Answer, pkg);
        }

        private static void OnAnswer(long sender, ZPackage pkg) => Guard.Run("BossBoardRecall.OnAnswer", () => Receive(pkg));

        private static void Receive(ZPackage pkg)
        {
            BossBoard? board = pkg.ReadBool() ? BossBoard.Read(pkg) : null;
            if (board != null && BossBoard.Latest == null)
            {
                BossBoard.Remember(board);
            }
            Action<BossBoard?>? done = _pending;
            _pending = null;
            done?.Invoke(board);
        }
    }
}
