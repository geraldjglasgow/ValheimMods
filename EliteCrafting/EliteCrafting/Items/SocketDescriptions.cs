using System.Collections.Generic;
using EliteCrafting.Rules;
using EliteCrafting.Text;

namespace EliteCrafting.Items
{
    /// <summary>
    /// The extra description lines of the socket stones (sockets.md section 7), under the vanilla description like the
    /// essence family line: a gem lists what it gives in each slot, a catalyst the affixes it strengthens. Rebuilt with
    /// the rules and the words, never per frame.
    /// </summary>
    internal static class SocketDescriptions
    {
        /// <summary>"Melee weapon, Ranged weapon: Rimebrand" per affix, slots in slot order.</summary>
        public static string GemLine(StoneDef def, RuleSet rules)
        {
            List<string> order = new List<string>();
            Dictionary<string, List<string>> slotsByAffix = new Dictionary<string, List<string>>();
            foreach (KeyValuePair<ItemSlot, string> pair in def.GemAffixes)
            {
                if (!slotsByAffix.TryGetValue(pair.Value, out List<string> slots))
                {
                    slotsByAffix[pair.Value] = slots = new List<string>();
                    order.Add(pair.Value);
                }
                slots.Add(Words.Localize("$ecf_ui_slot_" + ItemSlots.Id(pair.Key)));
            }
            List<string> lines = new List<string>();
            foreach (string id in order)
            {
                AffixDef? affix = rules.Affixes.Get(id);
                lines.Add(string.Join(", ", slotsByAffix[id]) + ": " + (affix != null ? Words.Localize(affix.Name) : id));
            }
            return lines.Count == 0 ? "" : Words.Localize("$ecf_ui_gem_header") + "\n" + string.Join("\n", lines);
        }

        /// <summary>"Strengthens: Rimebrand, Frostward, ..." - the catalyst family's live members.</summary>
        public static string CatalystLine(StoneDef def, RuleSet rules)
        {
            EssenceFamilyDef? family = rules.Economy.Family(def.Family);
            List<string> names = family == null ? new List<string>() : ItemDescriptions.MemberNames(family, rules);
            return names.Count == 0 ? "" : Words.Localize("$ecf_ui_catalyst_family", string.Join(", ", names));
        }
    }
}
