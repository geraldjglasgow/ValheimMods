using System.Collections.Generic;
using EliteCrafting.Core;
using EliteCrafting.Items;
using EliteCrafting.Rules;
using EliteCrafting.Text;

namespace EliteCrafting.Commands
{
    /// <summary>
    /// <c>ecraft list affixes|stones|rarities [&lt;filter&gt;]</c>: the running configuration, one line per entry
    /// (console-commands.md section 3). The filter matches, in this order: a slot id, a category id (affixes), a
    /// rarity id (<c>mythic</c> = the Mythic-only affixes; for stones, <c>applies_to</c>), a verb id (stones), else an id
    /// prefix. The last column is the last file that touched the entry (<see cref="RuleOrigins"/>). Under the stones, the
    /// essence families with their live members and the salvage shards (<see cref="StoneExtras"/>).
    /// </summary>
    internal static class ListCommand
    {
        public const string Grammar = "ecraft list affixes|stones|rarities [<filter>]";

        public static void Run(CommandCall call)
        {
            string what = call.Lower(0);
            string filter = call.Lower(1);
            switch (what)
            {
                case "affixes": Affixes(call, filter); break;
                case "stones": Stones(call, filter); break;
                case "rarities": Rarities(call, filter); break;
                default: call.Fail(what.Length == 0 ? "list what?" : $"cannot list '{what}'.", Grammar); break;
            }
        }

        private static void Affixes(CommandCall call, string filter)
        {
            IReadOnlyList<AffixDef> all = ActiveRules.Current.Affixes.Affixes;
            List<AffixDef> shown = new List<AffixDef>();
            foreach (AffixDef def in all)
            {
                if (filter.Length == 0 || AffixMatches(def, filter))
                {
                    shown.Add(def);
                }
            }
            call.Reply($"{shown.Count} of {all.Count} affixes" + (filter.Length > 0 ? $" matching '{filter}'" : ""));
            RuleOrigins origins = RuleOrigins.For(FamilySpec.Affixes, "affixes");
            shown.ForEach(def => call.Detail(AffixLine(def, origins.Of(def.Id))));
        }

        private static bool AffixMatches(AffixDef def, string filter)
        {
            if (ItemSlots.TryParse(filter, out ItemSlot slot))
            {
                return def.RollsOn(slot);
            }
            if (EnumIds<AffixCategory>.TryParse(filter, out AffixCategory category))
            {
                return def.Category == category;
            }
            if (filter == "mythic")
            {
                return def.MythicOnly;
            }
            return def.Id.StartsWith(filter, System.StringComparison.Ordinal) || def.Effect == filter;
        }

        private static string AffixLine(AffixDef def, string origin)
        {
            List<string> slots = new List<string>();
            foreach (ItemSlot slot in def.Slots)
            {
                slots.Add(ItemSlots.Id(slot));
            }
            string mythic = def.MythicOnly ? " mythic_only" : "";
            return $"{def.Id} \"{Words.Localize(def.Name)}\" {EnumIds<AffixValueType>.Id(def.Value)} "
                + $"[{string.Join(",", slots)}] {EnumIds<AffixCategory>.Id(def.Category)}{mythic} T{def.MinTier}-T{def.MaxTier} "
                + $"weight {Numbers.Format(def.Weight)} {(def.Enabled ? "enabled" : "disabled")} ({origin})";
        }

        private static void Stones(CommandCall call, string filter)
        {
            IReadOnlyList<StoneDef> all = ActiveRules.Current.Economy.Stones;
            List<StoneDef> shown = new List<StoneDef>();
            foreach (StoneDef stone in all)
            {
                if (filter.Length == 0 || StoneMatches(stone, filter))
                {
                    shown.Add(stone);
                }
            }
            call.Reply($"{shown.Count} of {all.Count} stones" + (filter.Length > 0 ? $" matching '{filter}'" : ""));
            RuleOrigins origins = RuleOrigins.For(FamilySpec.Economy, "stones");
            shown.ForEach(stone => call.Detail(StoneLine(stone, origins.Of(stone.Id))));
            StoneExtras.Families(call, filter);
            StoneExtras.Shards(call, filter);
        }

        private static bool StoneMatches(StoneDef stone, string filter)
        {
            if (ItemSlots.TryParse(filter, out ItemSlot slot))
            {
                return stone.AcceptsSlot(slot);
            }
            if (ActiveRules.Current.Economy.Rarity(filter) != null)
            {
                return stone.AppliesToRarity(filter);
            }
            if (EnumIds<StoneVerb>.TryParse(filter, out StoneVerb verb))
            {
                return stone.Verb == verb;
            }
            return stone.Id.StartsWith(filter, System.StringComparison.Ordinal);
        }

        private static string StoneLine(StoneDef stone, string origin)
        {
            string grade = stone.Grade == StoneGrade.None ? "" : "/" + EnumIds<StoneGrade>.Id(stone.Grade);
            string floor = (stone.TierFloor > 0 ? $" floor T{stone.TierFloor}" : "") + (stone.Family != null ? $" family {stone.Family}" : "");
            return $"{stone.Id} \"{Words.Localize(stone.Name)}\" {EnumIds<StoneVerb>.Id(stone.Verb)}{grade} "
                + $"on [{string.Join(",", stone.AppliesTo)}]{floor} prefab {stone.Prefab} "
                + $"{(stone.Enabled ? "enabled" : "disabled")} ({origin})";
        }

        private static void Rarities(CommandCall call, string filter)
        {
            IReadOnlyList<RarityDef> all = ActiveRules.Current.Economy.Rarities;
            RuleOrigins origins = RuleOrigins.For(FamilySpec.Economy, "rarities");
            List<RarityDef> shown = new List<RarityDef>();
            foreach (RarityDef rarity in all)
            {
                if (filter.Length == 0 || rarity.Id.StartsWith(filter, System.StringComparison.Ordinal))
                {
                    shown.Add(rarity);
                }
            }
            call.Reply($"{shown.Count} of {all.Count} rarities, lowest first");
            shown.ForEach(rarity => call.Detail(RarityLine(rarity, origins.Of(rarity.Id))));
        }

        private static string RarityLine(RarityDef rarity, string origin)
        {
            string mythic = rarity.MythicAffixes > 0 ? $" ({rarity.MythicAffixes} mythic-only)" : "";
            return $"{rarity.Id} \"{Words.Localize(rarity.Name)}\" {rarity.Color} affixes {rarity.MinAffixes}-{rarity.MaxAffixes}{mythic} "
                + $"glow {(rarity.Glow ? "yes" : "no")} drop_weight {Numbers.Format(rarity.DropWeight)} ({origin})";
        }
    }
}
