using System;
using EliteCrafting.Affixes;
using EliteCrafting.Rolling;
using EliteCrafting.Rules;

namespace EliteCrafting.Loot
{
    /// <summary>
    /// How many sockets a dropped magic item carries (sockets.md section 2): one draw from <c>drops.gear.sockets</c>
    /// (index = socket count), capped at <see cref="StoneDef.SocketLimit"/>. Drops may carry more sockets than the
    /// Jeweller's Chisel makes. Runs where the item is built (the creature's owner, or <c>ecraft roll</c>); pure.
    /// </summary>
    internal static class DropSockets
    {
        public static ItemState Add(ItemState state, Random random, RuleSet rules)
        {
            int count = RollMath.PickWeighted(rules.Economy.Drops.Gear.SocketWeights, random);
            return count <= 0 ? state : state.ToBuilder().SetSockets(Math.Min(count, StoneDef.SocketLimit)).Build();
        }
    }
}
