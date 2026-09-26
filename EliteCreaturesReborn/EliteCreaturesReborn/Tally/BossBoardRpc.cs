using System.Collections.Generic;
using EliteCreaturesReborn.Traits;
using PatchGuard;

namespace EliteCreaturesReborn.Tally
{
    /// <summary>
    /// The boss damage board's message. The boss's owner sends it as the boss dies, over the game's routed-RPC bus to
    /// every player on the server, and each client shows it (<see cref="BossBoardView"/>). A Twin's tally is the pair's
    /// together, since they share one health pool; the fight is named by the smaller of the pair's IDs, so the partner
    /// that falls with it sends the same fight and does not replace the board. A Phantom copy is a decoy and sends
    /// nothing.
    /// </summary>
    internal static class BossBoardRpc
    {
        public const string Rpc = "ecr_boss_board";

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
            bus.Register<ZPackage>(Rpc, OnBoard);
        }

        /// <summary>Owner side, as the boss dies and while its ZDO still holds the tally.</summary>
        public static void Announce(Character boss, ZDO zdo)
        {
            if (AspectStore.GetPhantomOf(zdo) != ZDOID.None || ZRoutedRpc.instance == null)
            {
                return;
            }
            List<DamageTally.Entry> entries = DamageTally.Load(zdo);
            ZDOID twin = AspectStore.GetTwin(zdo);
            AddPartner(entries, twin);
            if (entries.Count > 0)
            {
                Send(FightOf(zdo.m_uid, twin), boss.m_name, entries);
            }
        }

        // A Twin's partner is still alive at this point (it falls a moment later), so its ZDO still has its half.
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

        private static void Send(string fight, string bossName, List<DamageTally.Entry> entries)
        {
            ZPackage pkg = new ZPackage();
            pkg.Write(fight);
            pkg.Write(bossName);
            DamageTally.Write(pkg, entries);
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, Rpc, pkg);
        }

        private static void OnBoard(long sender, ZPackage pkg) => Guard.Run("BossBoardRpc.OnBoard", () => Read(pkg));

        // A dedicated server has no HUD, and the view simply finds nothing to draw on.
        private static void Read(ZPackage pkg)
        {
            string fight = pkg.ReadString();
            string bossName = pkg.ReadString();
            List<DamageTally.Entry> entries = new List<DamageTally.Entry>();
            DamageTally.Read(pkg, entries);
            BossBoardView.Show(fight, bossName, entries);
        }
    }
}
