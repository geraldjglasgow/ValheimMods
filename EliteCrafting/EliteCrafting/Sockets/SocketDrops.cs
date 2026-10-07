using System;
using EliteCrafting.Affixes;
using EliteCrafting.Items;
using EliteCrafting.Rolling;
using EliteCrafting.Rules;

namespace EliteCrafting.Sockets
{
    /// <summary>
    /// Sockets on dropped gear (sockets.md section 2): every pre-rolled Magic or Rare weapon, staff, armour piece or shield
    /// draws 0-3 sockets once, while <c>Gems and sockets</c> is on. Fixed weights (one setting per feature): Magic
    /// 80/15/4/1, Rare 60/25/11/4 for 0/1/2/3 sockets. Runs where the drop is built (the creature's owner).
    /// </summary>
    internal static class SocketDrops
    {
        private static readonly float[] Magic = { 80f, 15f, 4f, 1f };
        private static readonly float[] Rare = { 60f, 25f, 11f, 4f };

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
    }
}
