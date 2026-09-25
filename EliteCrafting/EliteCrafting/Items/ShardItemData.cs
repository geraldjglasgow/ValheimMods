using EliteCrafting.Rules;
using EliteCrafting.Text;

namespace EliteCrafting.Items
{
    /// <summary>
    /// Writes a shard's item values from <c>salvage.fragments</c> (salvage.md sections 5 and 7) into its prefab's shared
    /// data: name, description with the fuse line, max stack, item weight, teleportable, no trade value. Like the
    /// stones, every live shard shares its prefab's data, so a rules change reaches existing stacks. A shard the YAML no
    /// longer configures keeps its built-in values (its stacks survive; it just does not fuse). Every peer.
    /// </summary>
    internal static class ShardItemData
    {
        private static readonly FragmentDef Defaults = new FragmentDef();

        public static void Apply(StoneEntry entry, RuleSet rules)
        {
            string id = entry.ShardId!;
            FragmentDef? def = rules.Economy.Salvage.Fragment(id);
            ItemDrop.ItemData.SharedData shared = entry.Shared;
            shared.m_name = def?.Name ?? "$ecf_fragment_" + id;
            shared.m_description = ItemDescriptions.WithLine(def?.Description ?? "$ecf_fragment_" + id + "_desc", FuseLine(def, rules));
            shared.m_maxStackSize = def?.Stack ?? Defaults.Stack;
            shared.m_weight = def?.ItemWeight ?? Defaults.ItemWeight;
            shared.m_teleportable = true;
            shared.m_value = 0;
        }

        // "Right-click with 5 to fuse a Stone of Ascension." (salvage.md section 6), from the synced fuse count.
        private static string FuseLine(FragmentDef? def, RuleSet rules)
        {
            StoneDef? stone = def == null ? null : rules.Stone(def.Stone);
            if (stone == null)
            {
                return "";
            }
            return Words.Localize("$ecf_ui_fuse_line", def!.Fuse.ToString(), Words.Localize(stone.Name));
        }
    }

    /// <summary>
    /// Extra lines under a stone's or shard's vanilla description (the essence family line, the shard fuse line),
    /// localized when the rules or the words change. They live in the description the vanilla tooltip already shows.
    /// </summary>
    internal static class ItemDescriptions
    {
        public static string WithLine(string description, string line)
        {
            if (line.Length == 0)
            {
                return description;
            }
            return description.Length == 0 ? line : Words.Localize(description) + "\n\n" + line;
        }

        /// <summary>"Always adds one of: Venombrand, Blood Drinker" (essences.md section 12): the family's live members.</summary>
        public static string FamilyLine(StoneDef def, RuleSet rules)
        {
            EssenceFamilyDef? family = def.Verb == StoneVerb.Imbue ? rules.Economy.Family(def.Family) : null;
            if (family == null)
            {
                return "";
            }
            System.Collections.Generic.List<string> names = new System.Collections.Generic.List<string>();
            foreach (string id in family.Affixes)
            {
                AffixDef? affix = rules.Affixes.Get(id);
                if (affix != null && affix.Enabled && !affix.MythicOnly)
                {
                    names.Add(Words.Localize(affix.Name));
                }
            }
            return names.Count == 0 ? "" : Words.Localize("$ecf_ui_essence_family", string.Join(", ", names));
        }
    }
}
