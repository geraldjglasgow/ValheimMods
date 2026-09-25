using System.Collections.Generic;
using EliteCrafting.Rules;
using EliteCrafting.Text;

namespace EliteCrafting.Commands
{
    /// <summary>
    /// The lines <c>ecraft list stones</c> adds under the stones: each essence family with its live members
    /// (essences.md section 12) and each salvage shard with its fuse target (salvage.md section 6). Shown with no filter;
    /// the families also for the filter <c>imbue</c> or a family id, the shards for <c>shard</c>.
    /// </summary>
    internal static class StoneExtras
    {
        public static void Families(CommandCall call, string filter)
        {
            RuleSet rules = ActiveRules.Current;
            foreach (EssenceFamilyDef family in rules.Economy.EssenceFamilies.Values)
            {
                if (filter.Length == 0 || filter == "imbue" || filter == family.Id)
                {
                    call.Detail($"family {family.Id} \"{Words.Localize(family.Name)}\": {Members(rules, family)}");
                }
            }
        }

        public static void Shards(CommandCall call, string filter)
        {
            if (filter.Length > 0 && filter != "shard" && !filter.StartsWith("shard_", System.StringComparison.Ordinal))
            {
                return;
            }
            RuleSet rules = ActiveRules.Current;
            foreach (FragmentDef shard in rules.Economy.Salvage.Fragments)
            {
                StoneDef? stone = rules.Stone(shard.Stone);
                string state = stone == null || !stone.Enabled ? " (stone disabled: does not fuse)" : "";
                call.Detail($"shard {shard.Id} \"{Words.Localize(shard.Name)}\": {shard.Fuse} fuse into {shard.Stone}{state}, prefab {shard.Prefab}");
            }
        }

        // Live members by id; skipped ones (undefined, disabled, Mythic-only) in brackets, as the roll skips them.
        private static string Members(RuleSet rules, EssenceFamilyDef family)
        {
            List<string> parts = new List<string>();
            foreach (string id in family.Affixes)
            {
                AffixDef? def = rules.Affixes.Get(id);
                parts.Add(def != null && def.Enabled && !def.MythicOnly ? id : "[" + id + "]");
            }
            return string.Join(", ", parts);
        }
    }
}
