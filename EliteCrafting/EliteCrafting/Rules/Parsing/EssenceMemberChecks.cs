using System.Collections.Generic;
using EliteCrafting.Core;
using EliteCrafting.Items;

namespace EliteCrafting.Rules
{
    /// <summary>
    /// The cross-family warnings of essences.md section 11: family members checked against the affix family in force.
    /// Warnings only, never an error, because the two YAML families load, sync and reload separately (ESS-12); a member
    /// that fails is skipped at roll time. Runs whenever either family applies, on every peer (logging only).
    /// </summary>
    internal static class EssenceMemberChecks
    {
        // Design rule 3: an affix this light never stands alone for a slot in a family.
        private const float LightWeight = 30f;

        public static void Run(RuleSet rules)
        {
            foreach (EssenceFamilyDef family in rules.Economy.EssenceFamilies.Values)
            {
                List<AffixDef> live = LiveMembers(rules, family);
                WarnLoneLight(family, live);
            }
        }

        // One line per family and problem, so a default list waiting on later effects does not flood the log.
        private static List<AffixDef> LiveMembers(RuleSet rules, EssenceFamilyDef family)
        {
            List<AffixDef> live = new List<AffixDef>();
            List<string> unknown = new List<string>();
            List<string> mythic = new List<string>();
            foreach (string id in family.Affixes)
            {
                AffixDef? def = rules.Affixes.Get(id);
                List<string>? skipped = def == null ? unknown : def.MythicOnly ? mythic : null;
                if (skipped != null)
                {
                    skipped.Add(id);
                    continue;
                }
                live.Add(def!);
            }
            Warn(family, unknown, "not defined affixes");
            Warn(family, mythic, "Mythic-only affixes, never guaranteed");
            return live;
        }

        private static void Warn(EssenceFamilyDef family, List<string> ids, string what)
        {
            if (ids.Count > 0)
            {
                Log.Warn($"economy: essence_families.{family.Id}: {ids.Count} member(s) are {what}, skipped: {string.Join(", ", ids)}");
            }
        }

        private static void WarnLoneLight(EssenceFamilyDef family, List<AffixDef> live)
        {
            foreach (AffixDef def in live)
            {
                if (def.Weight > LightWeight)
                {
                    continue;
                }
                foreach (ItemSlot slot in def.Slots)
                {
                    if (!HasHeavierFor(live, slot))
                    {
                        Log.Warn($"economy: essence_families.{family.Id}: '{def.Id}' (weight {Numbers.Format(def.Weight)}) is the only member for "
                            + $"{ItemSlots.Id(slot)}: the essence would guarantee it there (design rule 3)");
                    }
                }
            }
        }

        private static bool HasHeavierFor(List<AffixDef> live, ItemSlot slot)
        {
            foreach (AffixDef def in live)
            {
                if (def.Weight > LightWeight && def.RollsOn(slot))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
