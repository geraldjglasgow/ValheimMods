using System.Collections.Generic;
using EliteCreaturesPack.Custom.Definitions;
using YamlConfig;

namespace EliteCreaturesPack.Custom.Files
{
    /// <summary>Reads `drops:` (<see cref="DropsBlock"/>) and `gear:` (<see cref="GearBlock"/>).</summary>
    internal static class LootReader
    {
        public static DropsBlock? ReadDrops(YamlNode entry, FieldReader fields)
        {
            YamlNode? block = fields.Block(entry, "drops");
            if (block == null)
            {
                return null;
            }
            DropsBlock drops = new DropsBlock { Replace = fields.Switch(block, "replace") ?? false };
            foreach (YamlNode row in fields.At(block, "items").Items)
            {
                fields.Note(row);
                DropRow? read = ReadRow(row, fields);
                if (read != null)
                {
                    drops.Items.Add(read);
                }
            }
            return drops;
        }

        public static GearBlock? ReadGear(YamlNode entry, FieldReader fields)
        {
            YamlNode? block = fields.Block(entry, "gear");
            if (block == null)
            {
                return null;
            }
            GearBlock gear = new GearBlock { Replace = fields.Switch(block, "replace") ?? false };
            gear.Always.AddRange(fields.Names(block, "always") ?? new List<string>());
            ReadLists(fields.At(block, "pick one from"), fields, gear.PickOneFrom);
            ReadLists(fields.At(block, "one set from"), fields, gear.OneSetFrom);
            return gear;
        }

        private static DropRow? ReadRow(YamlNode row, FieldReader fields)
        {
            string? item = fields.Text(row, "item");
            if (item == null)
            {
                row.Error("a drop needs an item");
                return null;
            }
            DropRow drop = new DropRow { Item = item, Field = fields.Relative(row) };
            drop.Amount = fields.Count(row, "amount", 0, 10000) ?? drop.Amount;
            drop.Chance = fields.Number(row, "chance", 0f, 1f) ?? drop.Chance;
            drop.OnePerPlayer = fields.Switch(row, "one per player") ?? drop.OnePerPlayer;
            drop.MoreForHigherLevels = fields.Switch(row, "more for higher levels") ?? drop.MoreForHigherLevels;
            return drop;
        }

        /// <summary>A list of item lists; each inner list may also be a single name. Empty lists are errors.</summary>
        private static void ReadLists(YamlNode node, FieldReader fields, List<List<string>> into)
        {
            foreach (YamlNode item in node.Items)
            {
                fields.Note(item);
                List<string>? names = fields.NamesOf(item);
                if (names != null && names.Count == 0)
                {
                    item.Error("an empty list of items");
                }
                else if (names != null)
                {
                    into.Add(names);
                }
            }
        }
    }
}
