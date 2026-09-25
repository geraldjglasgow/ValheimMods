using System.Collections.Generic;
using YamlDotNet.RepresentationModel;

namespace EliteCrafting.Rules
{
    /// <summary>Reads <c>drops.bosses</c> and <c>drops.creatures</c> (prefab → entry) and their <c>bonus</c> rows.</summary>
    internal static class BossDropParser
    {
        public static Dictionary<string, BossDrop> ParseBosses(MapReader drops)
        {
            Dictionary<string, BossDrop> bosses = new Dictionary<string, BossDrop>(System.StringComparer.Ordinal);
            foreach (MapReader r in Entries(drops, "bosses"))
            {
                r.Unknown("tier", "stone_rolls", "gear_rolls", "bonus");
                bosses[Name(r)] = new BossDrop
                {
                    Tier = r.Int("tier", 1, 1, 7),
                    StoneRolls = r.Int("stone_rolls", 0, 0),
                    GearRolls = r.Int("gear_rolls", 0, 0),
                    Bonus = ReadBonus(r),
                };
            }
            return bosses;
        }

        public static Dictionary<string, CreatureDrop> ParseCreatures(MapReader drops) => ParseCreatures(drops, "creatures");

        /// <summary>A prefab -> creature-shaped entry map under <paramref name="key"/> (creatures, chests.containers).</summary>
        public static Dictionary<string, CreatureDrop> ParseCreatures(MapReader parent, string key)
        {
            Dictionary<string, CreatureDrop> creatures = new Dictionary<string, CreatureDrop>(System.StringComparer.Ordinal);
            foreach (MapReader r in Entries(parent, key))
            {
                r.Unknown("tier", "multiplier", "stone_multiplier", "gear_multiplier", "bonus");
                creatures[Name(r)] = new CreatureDrop
                {
                    Tier = r.Int("tier", 0, 1, 7),
                    Multiplier = r.Float("multiplier", 1f, 0f),
                    StoneMultiplier = r.Float("stone_multiplier", 1f, 0f),
                    GearMultiplier = r.Float("gear_multiplier", 1f, 0f),
                    Bonus = ReadBonus(r),
                };
            }
            return creatures;
        }

        private static string Name(MapReader r) => r.Path.Substring(r.Path.LastIndexOf('.') + 1);

        private static IEnumerable<MapReader> Entries(MapReader drops, string key)
        {
            MapReader? sub = drops.Sub(key);
            if (sub == null)
            {
                yield break;
            }
            foreach (KeyValuePair<string, YamlNode> pair in YamlLists.Pairs(sub.Value.Map))
            {
                if (pair.Value is YamlMappingNode map)
                {
                    yield return new MapReader(map, sub.Value.At(pair.Key), drops.Issues);
                }
                else
                {
                    drops.Issues.Error(sub.Value.At(pair.Key), pair.Value, "should be a block of keyed values");
                }
            }
        }

        private static List<DropBonus> ReadBonus(MapReader r)
        {
            List<DropBonus> bonus = new List<DropBonus>();
            YamlSequenceNode? seq = r.Seq("bonus");
            for (int i = 0; seq != null && i < seq.Children.Count; i++)
            {
                if (!(seq.Children[i] is YamlMappingNode map))
                {
                    r.Issues.Error(r.At("bonus"), seq.Children[i], "a bonus row looks like { stone: awakening, chance: 100, amount: 1 }");
                    continue;
                }
                MapReader row = new MapReader(map, $"{r.At("bonus")}[{i}]", r.Issues);
                row.Unknown("stone", "chance", "amount");
                bonus.Add(new DropBonus
                {
                    Stone = row.Id("stone") ?? "",
                    Chance = row.Float("chance", 100f, 0f, 100f),
                    Amount = row.Int("amount", 1, 1),
                });
            }
            return bonus;
        }
    }
}
