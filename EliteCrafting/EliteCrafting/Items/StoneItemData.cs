using EliteCrafting.Rules;
using EliteCrafting.Text;

namespace EliteCrafting.Items
{
    /// <summary>
    /// Writes the economy YAML's item values into every stone prefab (prefabs.md section 2 "YAML on top",
    /// economy-yaml.md section 4): name, description, max stack, item weight, plus teleportable and no trade value.
    /// Runs on every peer after the prefabs are built and on every rules change. Every live copy of a stone shares its
    /// prefab's shared data (<see cref="StonePrefabs.LinkShared"/>), so this reaches existing stacks too; a stone the
    /// YAML disables or no longer defines keeps its prefab and built-in values, so its stacks survive (RC-3).
    /// </summary>
    internal static class StoneItemData
    {
        /// <summary>Key of an unbound reserved prefab's name (no stone definition names it).</summary>
        public const string UnboundName = "ecf_ui_stone_unbound";

        private static readonly StoneDef Defaults = new StoneDef();

        public static void ApplyAll(RuleSet rules)
        {
            StoneTints.Rebuild(rules.Economy);
            foreach (StoneEntry entry in StonePrefabs.All)
            {
                StoneDef? def = entry.IsShard ? null : rules.Economy.StoneForPrefab(entry.PrefabName);
                if (entry.IsShard)
                {
                    ShardItemData.Apply(entry, rules);
                }
                else
                {
                    Apply(entry, def, rules);
                }
                StoneVisuals.Apply(entry, def);
            }
        }

        private static void Apply(StoneEntry entry, StoneDef? def, RuleSet rules)
        {
            ItemDrop.ItemData.SharedData shared = entry.Shared;
            shared.m_name = NameOf(entry, def);
            string familyLine = def == null ? "" : ItemDescriptions.FamilyLine(def, rules);
            shared.m_description = ItemDescriptions.WithLine(DescriptionOf(entry, def), familyLine);
            shared.m_maxStackSize = def?.Stack ?? Defaults.Stack;
            shared.m_weight = def?.ItemWeight ?? Defaults.ItemWeight;
            shared.m_teleportable = true;
            shared.m_value = 0;
        }

        private static string NameOf(StoneEntry entry, StoneDef? def)
        {
            if (def == null)
            {
                return entry.BuiltInId != null ? "$ecf_stone_" + entry.BuiltInId : UnboundNameOf(entry);
            }
            string key = "ecf_stone_" + def.Id;
            if (def.Name == "$" + key && !Words.Has(key))
            {
                Words.Add(key, def.Id);   // localization.md section 4: an unnamed owner stone shows its id
            }
            return def.Name;
        }

        // Each reserved prefab needs a name of its own: the game stacks items by shared name (IsSameType,
        // FindFreeStackItem, ground auto-stack), so one shared "Unbound Stone" name would merge ECF_Custom01 and
        // ECF_Custom05 stacks into one prefab, and the stones would come back as the wrong stone once defined.
        // Shown as "Unbound Stone (ECF_Custom05)": the token ends at the space, the rest is literal.
        private static string UnboundNameOf(StoneEntry entry) => "$" + UnboundName + " (" + entry.PrefabName + ")";

        // The definition's description (economy-yaml.md section 4); the default key only when a word exists for it,
        // so an owner stone without a description shows none rather than a raw key.
        private static string DescriptionOf(StoneEntry entry, StoneDef? def)
        {
            string? id = def?.Id ?? entry.BuiltInId;
            if (id == null)
            {
                return "$" + UnboundName + "_desc";
            }
            string key = "ecf_stone_" + id + "_desc";
            string text = def?.Description ?? "$" + key;
            return text == "$" + key && !Words.Has(key) ? "" : text;
        }
    }
}
