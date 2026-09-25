using System.Collections.Generic;
using EliteCrafting.Core;
using YamlDotNet.RepresentationModel;

namespace EliteCrafting.Rules
{
    /// <summary>
    /// Reads <c>salvage:</c> (salvage.md section 7): <c>confirm</c>, <c>stations</c>, <c>yields</c> (rarity → rows)
    /// and <c>fragments</c> (the five shards, by id). References to rarities and stones are checked by
    /// <see cref="SalvageChecks"/> once every section is read.
    /// </summary>
    internal static class SalvageParser
    {
        private static readonly string[] FragmentKeys =
        {
            "id", "stone", "fuse", "name", "description", "stack", "item_weight", "tint",
        };

        public static SalvageRules Parse(MapReader root)
        {
            MapReader? sub = root.Sub("salvage");
            if (sub == null)
            {
                return new SalvageRules();
            }
            MapReader r = sub.Value;
            r.Unknown("confirm", "stations", "yields", "fragments");
            List<FragmentDef> fragments = ReadFragments(r);
            return new SalvageRules
            {
                Confirm = r.Bool("confirm", true),
                Stations = r.Strings("stations") ?? new List<string>(),
                Yields = ReadYields(r),
                Fragments = fragments,
                FragmentById = Index(fragments, f => f.Id),
                FragmentByPrefab = Index(fragments, f => f.Prefab),
            };
        }

        private static Dictionary<string, IReadOnlyList<SalvageYield>> ReadYields(MapReader r)
        {
            Dictionary<string, IReadOnlyList<SalvageYield>> yields = new Dictionary<string, IReadOnlyList<SalvageYield>>(System.StringComparer.Ordinal);
            MapReader? sub = r.Sub("yields");
            foreach (KeyValuePair<string, YamlNode> pair in sub == null ? new List<KeyValuePair<string, YamlNode>>() : YamlLists.Pairs(sub.Value.Map))
            {
                string path = sub!.Value.At(pair.Key);
                if (pair.Value is YamlSequenceNode seq)
                {
                    yields[pair.Key] = ReadRows(seq, path, r.Issues);
                }
                else if (!YamlNodes.IsNull(pair.Value))
                {
                    r.Issues.Error(path, pair.Value, "should be a list of rows like { fragment: shard_ascension, amount: 2 }");
                }
            }
            return yields;
        }

        private static List<SalvageYield> ReadRows(YamlSequenceNode seq, string path, RuleIssues issues)
        {
            List<SalvageYield> rows = new List<SalvageYield>();
            for (int i = 0; i < seq.Children.Count; i++)
            {
                if (!(seq.Children[i] is YamlMappingNode map))
                {
                    issues.Error(path, seq.Children[i], "a yield row looks like { fragment: shard_ascension, amount: 2 }");
                    continue;
                }
                rows.Add(ReadRow(new MapReader(map, $"{path}[{i}]", issues)));
            }
            return rows;
        }

        private static SalvageYield ReadRow(MapReader row)
        {
            row.Unknown("fragment", "amount", "chance");
            string? fragment = row.Id("fragment");
            if (fragment == null && !row.Has("fragment"))
            {
                row.Error("fragment", "is required: a shard id");
            }
            return new SalvageYield
            {
                Fragment = fragment ?? "",
                Amount = row.Int("amount", 1, 1, 999),
                Chance = row.Float("chance", 100f, 0f, 100f),
            };
        }

        private static List<FragmentDef> ReadFragments(MapReader r)
        {
            List<FragmentDef> fragments = new List<FragmentDef>();
            YamlSequenceNode? seq = r.Seq("fragments");
            foreach (YamlNode node in seq?.Children ?? new List<YamlNode>())
            {
                FragmentDef? fragment = node is YamlMappingNode map ? ReadFragment(new MapReader(map, r.At("fragments") + "[?]", r.Issues)) : null;
                if (fragment == null)
                {
                    r.Issues.Error(r.At("fragments"), node, "a fragment entry needs one of the shard ids: " + string.Join(", ", StoneCatalog.ShardIds));
                    continue;
                }
                fragments.Add(fragment);
            }
            return fragments;
        }

        private static FragmentDef? ReadFragment(MapReader probe)
        {
            string? id = probe.Id("id");
            if (!StoneCatalog.IsShard(id))
            {
                return null;
            }
            MapReader r = new MapReader(probe.Map, $"salvage.fragments[{id}]", probe.Issues);
            r.Unknown(FragmentKeys);
            FragmentDef fragment = new FragmentDef
            {
                Id = id!,
                Prefab = StoneCatalog.PrefabFor(id!),
                Stone = r.Id("stone") ?? "",
                Fuse = r.Int("fuse", 5, 1, 999),
                Name = r.Str("name") ?? "$ecf_fragment_" + id,
                Description = r.Str("description") ?? "$ecf_fragment_" + id + "_desc",
            };
            ReadItem(r, fragment);
            return fragment;
        }

        private static void ReadItem(MapReader r, FragmentDef fragment)
        {
            if (fragment.Stone.Length == 0 && !r.Has("stone"))
            {
                r.Error("stone", "is required: the stone id the shards fuse into");
            }
            fragment.Stack = r.Int("stack", 50, 1, 9999);
            fragment.ItemWeight = r.Float("item_weight", 0.1f, 0f);
            fragment.Tint = r.Str("tint");
            if (fragment.Tint != null && !Colors.TryParse(fragment.Tint, out _))
            {
                r.Error("tint", $"'{fragment.Tint}' is not a #RRGGBB color");
            }
        }

        private static Dictionary<string, FragmentDef> Index(List<FragmentDef> fragments, System.Func<FragmentDef, string> key)
        {
            Dictionary<string, FragmentDef> index = new Dictionary<string, FragmentDef>(System.StringComparer.Ordinal);
            foreach (FragmentDef fragment in fragments)
            {
                index[key(fragment)] = fragment;
            }
            return index;
        }
    }
}
