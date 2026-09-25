using System.Collections.Generic;
using EliteCrafting.Items;
using YamlDotNet.RepresentationModel;

namespace EliteCrafting.Rules
{
    /// <summary>Reads <c>drops:</c> (economy-yaml.md section 8): globals, chances, the tier tables and the gear block.</summary>
    internal static class DropParser
    {
        public const int Tiers = 7;

        private static readonly string[] Keys =
        {
            "tamed", "require_player", "max_stones_per_kill", "max_gear_per_kill", "star_multipliers", "star_step",
            "chances", "stones", "rarity_weights", "boss_rarity_weights", "gear", "bosses", "creatures", "chests", "ecr",
        };

        public static DropRules Parse(MapReader root)
        {
            MapReader? sub = root.Sub("drops");
            if (sub == null)
            {
                return new DropRules();
            }
            MapReader r = sub.Value;
            r.Unknown(Keys);
            DropRules drops = ReadGlobals(r);
            ReadChances(r, drops);
            drops.Stones = TierRows(r, "stones");
            drops.RarityWeights = TierRows(r, "rarity_weights");
            drops.BossRarityWeights = TierRows(r, "boss_rarity_weights");
            drops.Gear = ReadGear(r);
            drops.Bosses = BossDropParser.ParseBosses(r);
            drops.Creatures = BossDropParser.ParseCreatures(r);
            drops.Chests = ReadChests(r);
            drops.Ecr = EcrDropParser.Parse(r);
            return drops;
        }

        private static DropRules ReadGlobals(MapReader r)
        {
            return new DropRules
            {
                Tamed = r.Bool("tamed", false),
                RequirePlayer = r.Bool("require_player", true),
                MaxStonesPerKill = r.Int("max_stones_per_kill", 5, 0),
                MaxGearPerKill = r.Int("max_gear_per_kill", 2, 0),
                StarMultipliers = r.Floats("star_multipliers") ?? new[] { 1f, 2f, 3f },
                StarStep = r.Float("star_step", 1f, 0f),
            };
        }

        private static void ReadChances(MapReader r, DropRules drops)
        {
            MapReader? chances = r.Sub("chances");
            if (chances == null)
            {
                return;
            }
            chances.Value.Unknown("stone", "gear");
            drops.StoneChance = chances.Value.Floats("stone", Tiers) ?? new float[Tiers];
            drops.GearChance = chances.Value.Floats("gear", Tiers) ?? new float[Tiers];
        }

        /// <summary>A map of id → seven weights, tier 1 first.</summary>
        private static Dictionary<string, float[]> TierRows(MapReader r, string key)
        {
            Dictionary<string, float[]> rows = new Dictionary<string, float[]>(System.StringComparer.Ordinal);
            MapReader? sub = r.Sub(key);
            if (sub == null)
            {
                return rows;
            }
            foreach (KeyValuePair<string, YamlNode> pair in YamlLists.Pairs(sub.Value.Map))
            {
                float[]? row = YamlLists.Floats(pair.Value, sub.Value.At(pair.Key), r.Issues, Tiers);
                if (row != null)
                {
                    rows[pair.Key] = row;
                }
            }
            return rows;
        }

        private static GearDropRules ReadGear(MapReader parent)
        {
            MapReader? sub = parent.Sub("gear");
            if (sub == null)
            {
                return new GearDropRules();
            }
            MapReader r = sub.Value;
            r.Unknown("require_recipe", "tiers_below", "same_tier_weight", "lower_tier_weight", "slot_weights", "exclude", "include");
            return new GearDropRules
            {
                RequireRecipe = r.Bool("require_recipe", true),
                TiersBelow = r.Int("tiers_below", 1, 0, 6),
                SameTierWeight = r.Float("same_tier_weight", 3f, 0f),
                LowerTierWeight = r.Float("lower_tier_weight", 1f, 0f),
                SlotWeights = ReadSlotWeights(r),
                Exclude = r.Strings("exclude") ?? new List<string>(),
                Include = YamlLists.IntMap(r, "include", 1, 7),
            };
        }

        private static Dictionary<ItemSlot, float> ReadSlotWeights(MapReader r)
        {
            Dictionary<ItemSlot, float> weights = new Dictionary<ItemSlot, float>();
            foreach (KeyValuePair<string, float> pair in YamlLists.FloatMap(r, "slot_weights"))
            {
                if (ItemSlots.TryParse(pair.Key, out ItemSlot slot))
                {
                    weights[slot] = pair.Value;
                }
                else
                {
                    r.Issues.Error($"{r.At("slot_weights")}.{pair.Key}", r.Node("slot_weights"), "is not a slot");
                }
            }
            return weights;
        }

        private static ChestDrops ReadChests(MapReader r)
        {
            MapReader? sub = r.Sub("chests");
            if (sub == null)
            {
                return new ChestDrops();
            }
            sub.Value.Unknown("stone_chance", "gear_chance", "containers");
            return new ChestDrops
            {
                StoneChance = sub.Value.Float("stone_chance", 30f, 0f, 100f),
                GearChance = sub.Value.Float("gear_chance", 10f, 0f, 100f),
                Containers = BossDropParser.ParseCreatures(sub.Value, "containers"),
            };
        }
    }
}
