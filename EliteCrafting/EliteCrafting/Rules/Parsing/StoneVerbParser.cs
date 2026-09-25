using System.Collections.Generic;
using YamlDotNet.RepresentationModel;

namespace EliteCrafting.Rules
{
    /// <summary>The verb-specific stone fields: corrupt outcomes, gamble weights, duplicate, lock, quality, sigil, imbue.</summary>
    internal static class StoneVerbParser
    {
        public static void Read(MapReader r, StoneDef stone)
        {
            stone.Overflow = r.Int("overflow", 1, 0);
            stone.Outcomes = ReadOutcomes(r);
            stone.GambleWeights = YamlLists.FloatMap(r, "weights");
            stone.SealCopy = r.Bool("seal_copy", true);
            stone.MaxBound = r.Int("max_bound", 1, 1);
            stone.Step = r.Int("step", 1, 1);
            stone.Cap = r.Int("cap", 10, 1);
            stone.Steer = r.Enum("steer", SigilSteer.None);
            stone.SteerCategory = r.Has("category") ? r.Enum("category", AffixCategory.Utility) : (AffixCategory?)null;
            stone.Family = r.Id("family");
            Check(r, stone);
            CheckFamily(r, stone);
        }

        // essences.md section 11: imbue needs a family (whether it exists is checked once every section is read).
        private static void CheckFamily(MapReader r, StoneDef stone)
        {
            if (stone.Verb == StoneVerb.Imbue && stone.Family == null && !r.Has("family"))
            {
                r.Issues.Error(r.At("family"), r.Map, "an imbue stone needs family: an essence_families id");
            }
            if (stone.Verb != StoneVerb.Imbue && stone.Family != null)
            {
                r.Warn("family", "only an imbue stone has a family; ignored");
                stone.Family = null;
            }
        }

        private static void Check(MapReader r, StoneDef stone)
        {
            if (stone.Verb == StoneVerb.Quality && (stone.Slots.Count == 0 || stone.Cap < stone.Step))
            {
                r.Issues.Error(r.Path, r.Map, "a quality stone needs slots, and cap at least step");
            }
            if (stone.Verb == StoneVerb.Sigil && stone.Steer == SigilSteer.None)
            {
                r.Issues.Error(r.At("steer"), r.Map, "a sigil needs steer: preserve, category or cull");
            }
            if (stone.Steer == SigilSteer.Category && stone.SteerCategory == null)
            {
                r.Issues.Error(r.At("category"), r.Map, "steer: category needs category: offense, defense or utility");
            }
            if (stone.Verb == StoneVerb.Corrupt && stone.Outcomes.Count == 0)
            {
                r.Issues.Error(r.At("outcomes"), r.Map, "a corrupt stone needs an outcomes table");
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
