using System.Collections.Generic;
using EliteCreaturesPack.Custom.Definitions;
using YamlConfig;

namespace EliteCreaturesPack.Custom.Files
{
    /// <summary>
    /// Reads `elite:` (<see cref="EliteBlock"/>). Only the shape is checked here: whether ECR knows the mutation and aspect
    /// names, and whether the creature has the attacks an aspect needs, is checked by the elite step when ECR is there.
    /// </summary>
    internal static class EliteReader
    {
        public static EliteBlock? Read(YamlNode entry, FieldReader fields)
        {
            YamlNode? block = fields.Block(entry, "elite");
            if (block == null)
            {
                return null;
            }
            EliteBlock elite = new EliteBlock();
            elite.Mutations.AddRange(fields.Names(block, "mutations") ?? new List<string>());
            elite.Aspects.AddRange(fields.Names(block, "aspect") ?? new List<string>());
            elite.PortalAttacks.AddRange(fields.Names(block, "portal attacks") ?? new List<string>());
            foreach (YamlNode item in fields.At(block, "summon").Items)
            {
                fields.Note(item);
                SummonEntry? summon = ReadSummon(item, fields);
                if (summon != null)
                {
                    elite.Summon.Add(summon);
                }
            }
            return elite;
        }

        /// <summary>A creature name, or { creature: name, stars: n }.</summary>
        private static SummonEntry? ReadSummon(YamlNode item, FieldReader fields)
        {
            if (item.Kind == YamlNodeKind.Scalar)
            {
                string? name = fields.TextOf(item);
                return name == null ? null : new SummonEntry { Creature = name };
            }
            string? creature = fields.Text(item, "creature");
            if (creature == null)
            {
                item.Error("a summon needs a creature");
                return null;
            }
            return new SummonEntry { Creature = creature, Stars = fields.Whole(item, "stars", 0, 10) };
        }
    }
}
