using System.Collections.Generic;
using EliteCrafting.Core;
using YamlDotNet.RepresentationModel;

namespace EliteCrafting.Rules
{
    /// <summary>
    /// Reads <c>essence_families:</c> (essences.md section 11): family id → <c>{ affixes, name }</c>. The members are
    /// affix ids of the other YAML family; whether they exist is warned about when both families are in force
    /// (<see cref="EssenceMemberChecks"/>), never an error here (ESS-12).
    /// </summary>
    internal static class EssenceFamilyParser
    {
        public static Dictionary<string, EssenceFamilyDef> Parse(MapReader root)
        {
            Dictionary<string, EssenceFamilyDef> families = new Dictionary<string, EssenceFamilyDef>(System.StringComparer.Ordinal);
            MapReader? sub = root.Sub("essence_families");
            if (sub == null)
            {
                return families;
            }
            foreach (KeyValuePair<string, YamlNode> pair in YamlLists.Pairs(sub.Value.Map))
            {
                EssenceFamilyDef? family = ParseOne(sub.Value, pair.Key, pair.Value);
                if (family != null)
                {
                    families[family.Id] = family;
                }
            }
            return families;
        }

        private static EssenceFamilyDef? ParseOne(MapReader parent, string id, YamlNode node)
        {
            string path = parent.At(id);
            if (!Ids.IsValid(id))
            {
                parent.Issues.Error(path, node, $"'{id}' is not a valid family id (lowercase letters, digits and _)");
                return null;
            }
            if (!(node is YamlMappingNode map))
            {
                parent.Issues.Error(path, node, "a family looks like { affixes: [venombrand, blood_drinker] }");
                return null;
            }
            MapReader r = new MapReader(map, path, parent.Issues);
            r.Unknown("affixes", "name");
            List<string> affixes = r.Strings("affixes") ?? new List<string>();
            if (affixes.Count == 0)
            {
                r.Error("affixes", "a family needs at least one affix");
            }
            return new EssenceFamilyDef { Id = id, Name = r.Str("name") ?? "$ecf_family_" + id, Affixes = affixes };
        }
    }
}
