using System.Collections.Generic;
using UnityEngine;

namespace EliteCrafting.Loot
{
    /// <summary>
    /// The players taking part in a boss fight, for the boss gems' one roll each (user 2026-10-07: "roll the chance multiple
    /// times per player participating in the fight? like if 5 people are on, its possible to get 5 gems"): every player
    /// within <see cref="Range"/> of the boss when it dies, the dead included (they fought too), at least one. Counted on
    /// the boss's owner (where the loot is rolled) from the players its game has loaded nearby.
    /// </summary>
    internal static class BossParty
    {
        public const float Range = 50f;
        private const int Most = 10;

        private static readonly List<Player> Near = new List<Player>();

        public static int Count(Vector3 at)
        {
            Near.Clear();
            Player.GetPlayersInRange(at, Range, Near);
            return Mathf.Clamp(Near.Count, 1, Most);
        }
    }
}
