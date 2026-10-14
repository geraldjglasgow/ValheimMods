using System;
using System.Collections.Generic;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// Each player's souls (features/arena.md section 3, "Souls"), kept in the core's ZDO (<see cref="ArenaKeys.Wallets"/>)
    /// with the core's owner the one who writes it. Every kill's souls go to every player of the trial in full, so a kill
    /// writes one number, the trial's total (<see cref="ArenaState.Souls"/>); a wallet keeps only what is the player's own:
    /// the total when they joined (nothing before that is theirs), what they spent at Hel's Anvil, and how many repairs
    /// they made. Their souls are the total less both. Players are their peers, as for the trial's messages. Written when
    /// a player joins and at each repair; read every frame by the HUD line and the anvil's hover, so the decoded wallets
    /// are kept against the very array they came from.
    /// </summary>
    internal static class SoulWallets
    {
        private static readonly int Key = ArenaKeys.Wallets.GetStableHashCode();
        private static readonly Dictionary<long, Wallet> Wallets = new Dictionary<long, Wallet>();
        private static byte[]? _readFrom;

        /// <summary>One player's wallet.</summary>
        public struct Wallet
        {
            public long Before;
            public long Spent;
            public int Repairs;
        }

        /// <summary>A new trial: the players in the hall now, each from nothing. Core's owner only.</summary>
        public static void Reset(ZDO core, List<long> inHall)
        {
            Wallets.Clear();
            foreach (long peer in inHall)
            {
                Wallets[peer] = new Wallet();
            }
            Save(core);
        }

        /// <summary>Players in the hall who have no wallet yet get one, from the souls earned so far. Core's owner only;
        /// written only when one is new.</summary>
        public static void Join(ZDO core, List<long> inHall, long total)
        {
            Load(core);
            bool changed = false;
            foreach (long peer in inHall)
            {
                if (!Wallets.ContainsKey(peer))
                {
                    Wallets[peer] = new Wallet { Before = total };
                    changed = true;
                }
            }
            if (changed)
            {
                Save(core);
            }
        }

        /// <summary>The player's wallet; false for a peer that is no player of the trial.</summary>
        public static bool TryGet(ZDO core, long peer, out Wallet wallet)
        {
            Load(core);
            return Wallets.TryGetValue(peer, out wallet);
        }

        /// <summary>The souls the player has now: the trial's total less what was earned before they joined and what they
        /// spent; 0 for no player of the trial.</summary>
        public static long Souls(ZDO core, long peer) =>
            TryGet(core, peer, out Wallet wallet) ? Unspent(new ArenaState(core).Souls, wallet) : 0L;

        /// <summary>The souls the player earned in the trial, spent or not.</summary>
        public static long Earned(ZDO core, long peer) =>
            TryGet(core, peer, out Wallet wallet) ? Math.Max(0L, new ArenaState(core).Souls - wallet.Before) : 0L;

        public static long Unspent(long total, Wallet wallet) => Math.Max(0L, total - wallet.Before - wallet.Spent);

        /// <summary>The player pays <paramref name="cost"/> for a repair. Core's owner only.</summary>
        public static void Spend(ZDO core, long peer, int cost)
        {
            Load(core);
            Wallets.TryGetValue(peer, out Wallet wallet);
            wallet.Spent += Mathf.Max(0, cost);
            wallet.Repairs++;
            Wallets[peer] = wallet;
            Save(core);
        }

        private static void Save(ZDO core)
        {
            ZPackage pkg = new ZPackage();
            pkg.Write(Wallets.Count);
            foreach (KeyValuePair<long, Wallet> entry in Wallets)
            {
                pkg.Write(entry.Key);
                pkg.Write(entry.Value.Before);
                pkg.Write(entry.Value.Spent);
                pkg.Write(entry.Value.Repairs);
            }
            byte[] bytes = pkg.GetArray();
            core.Set(Key, bytes);
            _readFrom = bytes;
        }

        private static void Load(ZDO core)
        {
            byte[]? bytes = core.GetByteArray(Key);
            if (ReferenceEquals(bytes, _readFrom))
            {
                return;
            }
            _readFrom = bytes;
            Wallets.Clear();
            if (bytes != null)
            {
                Decode(bytes);
            }
        }

        private static void Decode(byte[] bytes)
        {
            try
            {
                ZPackage pkg = new ZPackage(bytes);
                for (int n = pkg.ReadInt(); n > 0; n--)
                {
                    long peer = pkg.ReadLong();
                    Wallets[peer] = new Wallet { Before = pkg.ReadLong(), Spent = pkg.ReadLong(), Repairs = pkg.ReadInt() };
                }
            }
            catch (Exception)
            {
                Wallets.Clear(); // a damaged list holds no souls rather than failing every frame
            }
        }
    }
}
