using System;
using System.Collections.Generic;
using EliteCrafting.Rules;

namespace EliteCrafting.Sockets
{
    /// <summary>
    /// What each boss adds of the socket stones (sockets.md section 7), on top of its YAML drops: the Dvergr Chisel one
    /// time in four from every boss, and its own gems. Fixed in code so every server has them whatever its own economy
    /// file says (a file written before the gems keeps its boss bonus lists, which replace the defaults' whole). Rolled
    /// on the creature's owner with the rest of the boss's drops, while <c>Rune drops</c> and <c>Gems and sockets</c> are on.
    /// </summary>
    internal static class BossGems
    {
        private sealed class Row
        {
            public Row(string stone, float chance)
            {
                Stone = stone;
                Chance = chance;
            }

            public string Stone { get; }

            /// <summary>Percent.</summary>
            public float Chance { get; }
        }

        private static Row Chisel => new Row(StoneCatalog.ChiselId, 25f);

        private static readonly Dictionary<string, Row[]> Table = new Dictionary<string, Row[]>(StringComparer.Ordinal)
        {
            ["Eikthyr"] = new[] { new Row("gem_thor", 10f), new Row("gem_sleipnir", 10f), Chisel },
            ["gd_king"] = new[] { new Row("gem_freyja", 15f), Chisel },
            ["Bonemass"] = new[] { new Row("gem_nidhogg", 15f), Chisel },
            ["Dragon"] = new[] { new Row("gem_ymir", 20f), new Row("gem_skadi", 20f), Chisel },
            ["GoblinKing"] = new[] { new Row("gem_surtr", 20f), new Row("gem_tyr", 20f), Chisel },
            ["SeekerQueen"] = new[] { new Row("gem_odin", 20f), new Row("gem_heimdall", 20f), Chisel },
            ["Fader"] = new[] { new Row("gem_hel", 25f), Chisel },
        };

        /// <summary>The boss's gems and chisel, each row rolled on its own; a disabled stone never drops.</summary>
        public static void Add(EconomyRules economy, string? bossPrefab, Random random, List<StoneDef> into)
        {
            if (!SocketSwitch.On || bossPrefab == null || !Table.TryGetValue(bossPrefab, out Row[] rows))
            {
                return;
            }
            foreach (Row row in rows)
            {
                StoneDef? stone = economy.Stone(row.Stone);
                if (stone != null && stone.Enabled && random.NextDouble() * 100.0 < row.Chance)
                {
                    into.Add(stone);
                }
            }
        }
    }
}
