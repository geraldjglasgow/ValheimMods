using System.Collections.Generic;
using YamlDotNet.RepresentationModel;

namespace EliteCrafting.Rules
{
    /// <summary>The verb-specific rune fields: the Serpent Rune's outcome table and overflow.</summary>
    internal static class StoneVerbParser
    {
        public static void Read(MapReader r, StoneDef stone)
        {
            stone.Overflow = r.Int("overflow", 1, 0);
            stone.Outcomes = ReadOutcomes(r);
            if (stone.Verb == StoneVerb.Corrupt && stone.Outcomes.Count == 0)
            {
                r.Issues.Error(r.At("outcomes"), r.Map, "a corrupt rune needs an outcomes table");
            }
        }

        private static List<CorruptWeight> ReadOutcomes(MapReader r)
        {
            List<CorruptWeight> outcomes = new List<CorruptWeight>();
            YamlSequenceNode? seq = r.Seq("outcomes");
            for (int i = 0; seq != null && i < seq.Children.Count; i++)
            {
                if (!(seq.Children[i] is YamlMappingNode map))
                {
                    r.Issues.Error(r.At("outcomes"), seq.Children[i], "an outcome row looks like { outcome: seal_only, weight: 25 }");
                    continue;
                }
                MapReader row = new MapReader(map, $"{r.At("outcomes")}[{i}]", r.Issues);
                row.Unknown("outcome", "weight");
                outcomes.Add(new CorruptWeight
                {
                    Outcome = row.Enum("outcome", CorruptOutcome.SealOnly),
                    Weight = row.Float("weight", 0f, 0f),
                });
            }
            return outcomes;
        }
    }
}
