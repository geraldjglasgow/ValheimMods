using System;
using System.Linq;
using EliteCrafting.Affixes;
using EliteCrafting.Core;
using EliteCrafting.Items;
using EliteCrafting.Rolling;
using EliteCrafting.Rules;

namespace EliteCrafting.Sockets
{
    /// <summary>
    /// Sockets on dropped gear (sockets.md section 2): every pre-rolled Magic or Rare weapon, staff, armour piece or shield
    /// draws 0-3 sockets once, while <c>Gems and sockets</c> is on. Fixed weights (one setting per feature): Magic
    /// 80/15/4/1, Rare 60/25/11/4 for 0/1/2/3 sockets. Runs where the drop is built (the creature's owner). The Dvergr
    /// Chisel cuts by the same weights without the none (sockets.md section 5).
    /// </summary>
    internal static class SocketDrops
    {
        private static readonly float[] Magic = { 80f, 15f, 4f, 1f };
        private static readonly float[] Rare = { 60f, 25f, 11f, 4f };
        private static readonly float[] MagicCut = Magic.Skip(1).ToArray();
        private static readonly float[] RareCut = Rare.Skip(1).ToArray();

        /// <summary>The rolled state with its sockets; the state itself when none are drawn or the base takes none.</summary>
        public static ItemState Add(ItemState state, ClassInfo info, RarityDef rarity, Random random)
        {
            if (!SocketSwitch.On || GemCatalog.BaseOf(info) == SocketBase.None)
            {
                return state;
            }
            int count = RollMath.PickWeighted(rarity.Id == "magic" ? Magic : Rare, random);
            return count <= 0 ? state : state.ToBuilder().SetSockets(count).Build();
        }

        /// <summary>
        /// The sockets a Dvergr Chisel cuts (user 2026-10-08: "I want the dropped gear odds on the chisel use too"): 1-3 by
        /// the drop weights of the item's rarity, a Normal item as Magic, never none.
        /// </summary>
        public static int Cut(string? rarityId, Random random) => 1 + RollMath.PickWeighted(CutWeights(rarityId), random);

        /// <summary>The chance in percent that a chisel cuts this many sockets (1-3) into an item of this rarity.</summary>
        public static string CutChance(string? rarityId, int sockets)
        {
            float[] weights = CutWeights(rarityId);
            return Numbers.Format(100f * weights[sockets - 1] / weights.Sum());
        }

        private static float[] CutWeights(string? rarityId) => rarityId == "rare" ? RareCut : MagicCut;
    }
}
