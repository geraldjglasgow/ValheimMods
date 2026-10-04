using System.Collections.Generic;
using EliteCreaturesReborn.Aspects;
using EliteCreaturesReborn.Traits;
using PatchGuard;

namespace EliteCreaturesReborn.Tally
{
    /// <summary>
    /// The boss damage board's message. The boss's owner sends it as the boss dies, over the game's routed-RPC bus to
    /// every machine on the server, and each one keeps it as its latest board (<see cref="BossBoard"/>) and shows it
    /// (<see cref="BossBoardView"/>). A Twin's tally is the pair's together, since they share one health pool; the fight
    /// is named by the smaller of the pair's IDs, so the partner that falls with it sends the same fight and replaces
    /// neither the board kept nor the one on screen. A Tethered pair shows one board, when the last of the two falls: the
    /// first to fall sends nothing and hands its tally on to its partner (<see cref="BossCredit.HandOver"/>), so the
    /// last board counts the damage to both. A Phantom copy is a decoy and sends nothing; the damage done to it is
    /// already in its boss's tally.
    /// </summary>
    internal static class BossBoardRpc
    {
        public const string Rpc = "ecr_boss_board";

        private static ZRoutedRpc? _registeredOn;

        /// <summary>
        /// Registers the handler once per routed-RPC bus. Called at world start on every machine. A new bus is a new
        /// session, so the board kept from the last world or server is dropped with the old one.
        /// </summary>
        public static void EnsureRegistered()
        {
            ZRoutedRpc bus = ZRoutedRpc.instance;
            if (bus == null || ReferenceEquals(bus, _registeredOn))
            {
                return;
            }
            _registeredOn = bus;
            BossBoard.Forget();
            bus.Register<ZPackage>(Rpc, OnBoard);
        }

        /// <summary>Owner side, as the boss dies and while its ZDO still holds the tally.</summary>
        public static void Announce(Character boss, ZDO zdo)
        {
            if (AspectStore.GetPhantomOf(zdo) != ZDOID.None || ZRoutedRpc.instance == null)
            {
                return;
            }
            ZDOID tether = AspectStore.GetTether(zdo);
            if (TetherPair.Standing(tether))
            {
                BossCredit.HandOver(zdo, tether);
                return;
            }
            List<DamageTally.Entry> entries = DamageTally.Load(zdo);
            ZDOID partner = tether != ZDOID.None ? tether : AspectStore.GetTwin(zdo);
            AddPartner(entries, partner);
            if (entries.Count > 0)
            {
                Send(new BossBoard { Fight = FightOf(zdo.m_uid, partner), BossName = boss.m_name, Entries = entries });
            }
        }

        // A Twin's partner is still alive at this point (it falls a moment later), so its ZDO still has its half. So may
        // a Tethered partner that fell in the same moment, before it could hand its tally on; one that did has an empty one.
        private static void AddPartner(List<DamageTally.Entry> entries, ZDOID twin)
        {
            ZDO? partner = twin != ZDOID.None && ZDOMan.instance != null ? ZDOMan.instance.GetZDO(twin) : null;
            if (partner == null)
            {
                return;
            }
            foreach (DamageTally.Entry entry in DamageTally.Load(partner))
            {
                DamageTally.Merge(entries, entry.Id, entry.Name, entry.Damage);
            }
        }

        private static string FightOf(ZDOID self, ZDOID twin)
        {
            string a = self.ToString();
            string b = twin == ZDOID.None ? a : twin.ToString();
            return string.CompareOrdinal(a, b) <= 0 ? a : b;
        }

        // Everybody includes the sender itself: the routed-RPC bus hands a broadcast to its own handler as well.
        private static void Send(BossBoard board)
        {
            ZPackage pkg = new ZPackage();
            board.Write(pkg);
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, Rpc, pkg);
        }

        private static void OnBoard(long sender, ZPackage pkg) => Guard.Run("BossBoardRpc.OnBoard", () => Read(pkg));

        // A dedicated server has no HUD: it keeps the board for players who ask later, and the view finds nothing to draw on.
        private static void Read(ZPackage pkg)
        {
            BossBoard board = BossBoard.Read(pkg);
            if (BossBoard.Remember(board))
            {
                BossBoardView.Show(board);
            }
        }
    }
}
