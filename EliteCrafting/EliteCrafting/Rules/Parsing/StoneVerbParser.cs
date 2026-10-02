using System.Collections.Generic;
using EliteCrafting.Items;
using YamlDotNet.RepresentationModel;

namespace EliteCrafting.Rules
{
    /// <summary>
    /// The verb-specific stone fields: corrupt outcomes, gamble weights, duplicate, lock, quality, sigil, imbue, and the
    /// socket, gem and catalyse verbs of sockets.md.
    /// </summary>
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
            stone.MaxSockets = r.Int("max_sockets", 2, 1, StoneDef.SocketLimit);
            stone.GemAffixes = ReadGemAffixes(r);
            Check(r, stone);
            CheckSockets(r, stone);
            CheckFamily(r, stone);
        }

        // essences.md section 11, sockets.md section 8: imbue and catalyse need a family, a gem may name one (whether
        // it exists is checked once every section is read).
        private static void CheckFamily(MapReader r, StoneDef stone)
        {
            bool needs = stone.Verb == StoneVerb.Imbue || stone.Verb == StoneVerb.Catalyse;
            if (needs && stone.Family == null && !r.Has("family"))
            {
                r.Issues.Error(r.At("family"), r.Map, "an imbue or catalyse stone needs family: an essence_families id");
            }
            if (!needs && stone.Verb != StoneVerb.Gem && stone.Family != null)
            {
                r.Warn("family", "only imbue, gem and catalyse stones have a family; ignored");
                stone.Family = null;
            }
        }

        private static void CheckSockets(MapReader r, StoneDef stone)
        {
            if (stone.Verb == StoneVerb.Gem && stone.GemAffixes.Count == 0)
            {
                r.Issues.Error(r.At("inscriptions"), r.Map, "a gem needs inscriptions: a map of item slot to inscription id");
            }
            if (stone.Verb == StoneVerb.Catalyse && stone.Cap < stone.Step)
            {
                r.Issues.Error(r.Path, r.Map, "a catalyst needs cap at least step");
            }
        }

        // gem (sockets.md section 4): item slot -> the affix id the gem gives there.
        private static Dictionary<ItemSlot, string> ReadGemAffixes(MapReader r)
        {
            Dictionary<ItemSlot, string> affixes = new Dictionary<ItemSlot, string>();
            MapReader? sub = r.Sub("inscriptions");
            if (sub == null)
            {
                return affixes;
            }
            MapReader map = sub.Value;
            foreach (KeyValuePair<string, YamlNode> pair in YamlLists.Pairs(map.Map))
            {
                string? id = map.Id(pair.Key);
                if (!ItemSlots.TryParse(pair.Key, out ItemSlot slot))
                {
                    map.Error(pair.Key, "is not a slot");
                }
                else if (id != null)
                {
                    affixes[slot] = id;
                }
            }
            return affixes;
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
