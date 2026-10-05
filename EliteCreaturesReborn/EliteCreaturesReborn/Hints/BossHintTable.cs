using System.Collections.Generic;
using UnityEngine;

namespace EliteCreaturesReborn.Hints
{
    /// <summary>
    /// Which bosses leave a hint and where it points, from the boss's own altar: Bonemass to the Bog Witch, Moder to the
    /// ancient forge (the Forge of Potential), Yagluth to Haldor and the Elder to Hildir. Both ends are the game's own
    /// location names, which the server's world knows the place of. Fixed by design: no other boss hints.
    /// </summary>
    internal static class BossHintTable
    {
        public readonly struct Entry
        {
            public readonly string Altar;
            public readonly string Target;

            public Entry(string altar, string target)
            {
                Altar = altar;
                Target = target;
            }
        }

        private static readonly Dictionary<string, Entry> Entries = new Dictionary<string, Entry>
        {
            ["Bonemass"] = new Entry("Bonemass", "BogWitch_Camp"),
            ["Dragon"] = new Entry("Dragonqueen", "AncientUpgradeStation"),
            ["GoblinKing"] = new Entry("GoblinKing", "Vendor_BlackForest"),
            ["gd_king"] = new Entry("GDKing", "Hildir_camp"),
        };

        private static readonly string[] Words =
            { "north", "northeast", "east", "southeast", "south", "southwest", "west", "northwest" };

        public static bool TryGet(string boss, out Entry entry) => Entries.TryGetValue(boss, out entry);

        /// <summary>The eighth of the compass from <paramref name="from"/> toward <paramref name="to"/>, clockwise from
        /// north: 0 is north (+z, up on the map), 2 is east (+x).</summary>
        public static int Sector(Vector3 from, Vector3 to)
        {
            float degrees = Mathf.Atan2(to.x - from.x, to.z - from.z) * Mathf.Rad2Deg;
            return Wrap(Mathf.RoundToInt(degrees / 45f));
        }

        public static string Word(int sector) => Words[Wrap(sector)];

        private static int Wrap(int sector) => ((sector % 8) + 8) % 8;
    }
}
