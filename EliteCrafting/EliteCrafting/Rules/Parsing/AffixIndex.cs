using System;
using System.Collections.Generic;
using EliteCrafting.Core;
using EliteCrafting.Effects;
using EliteCrafting.Items;

namespace EliteCrafting.Rules
{
    /// <summary>
    /// The affix family's cross-entry work, done once per load: the id table, the channel table (one per effect, param
    /// and condition of the enabled affixes, with its cap resolved: YAML <c>caps:</c> key, then the effect-level key,
    /// then the registry default), the rollable pools per slot, and the load warnings (never-rolling affixes, caps
    /// nobody feeds, unknown cap effects are errors).
    /// </summary>
    internal static class AffixIndex
    {
        public static void Build(AffixRules rules, RuleIssues issues)
        {
            Dictionary<string, AffixDef> byId = new Dictionary<string, AffixDef>(StringComparer.Ordinal);
            foreach (AffixDef def in rules.Affixes)
            {
                byId[def.Id] = def;
                WarnNeverRolls(def, issues);
            }
            rules.ById = byId;
            CheckCapKeys(rules, issues);
            rules.Channels = BuildChannels(rules);
            BuildPools(rules);
        }

        private static void WarnNeverRolls(AffixDef def, RuleIssues issues)
        {
            if (def.Enabled && (def.Weight <= 0f || NoTierWeight(def)))
            {
                issues.Warn($"affixes[{def.Id}]", null, "weight 0 (or every tier weight 0): it never rolls; existing copies keep working");
            }
        }

        private static bool NoTierWeight(AffixDef def)
        {
            foreach (AffixTierDef tier in def.Tiers)
            {
                if (tier.Weight > 0f)
                {
                    return false;
                }
            }
            return true;
        }

        private static void CheckCapKeys(AffixRules rules, RuleIssues issues)
        {
            foreach (string key in rules.Caps.Keys)
            {
                string effect = EffectPart(key);
                if (!EffectRegistry.IsRegistered(effect))
                {
                    issues.Error($"caps.{key}", null, $"'{effect}' is not an effect this version implements");
                }
            }
        }

        private static string EffectPart(string key)
        {
            int cut = key.IndexOfAny(new[] { ':', '@' });
            return cut < 0 ? key : key.Substring(0, cut);
        }

        public static string ChannelKey(AffixDef def)
        {
            string key = def.Param == null ? def.Effect : def.Effect + ":" + def.Param;
            return def.Condition == AffixCondition.HealthCritical ? key + "@health_critical" : key;
        }

        private static List<ChannelDef> BuildChannels(AffixRules rules)
        {
            List<ChannelDef> channels = new List<ChannelDef>();
            Dictionary<string, ChannelDef> byKey = new Dictionary<string, ChannelDef>(StringComparer.Ordinal);
            foreach (AffixDef def in rules.Affixes)
            {
                if (!def.Enabled)
                {
                    continue;
                }
                string key = ChannelKey(def);
                if (!byKey.TryGetValue(key, out ChannelDef channel))
                {
                    channel = NewChannel(rules, def, key, channels.Count);
                    byKey[key] = channel;
                    channels.Add(channel);
                }
                def.ChannelIndex = channel.Index;
            }
            return channels;
        }

        private static ChannelDef NewChannel(AffixRules rules, AffixDef def, string key, int index)
        {
            return new ChannelDef
            {
                Index = index,
                Key = key,
                Effect = def.EffectDef,
                Param = def.Param,
                Condition = def.Condition,
                Cap = ResolveCap(rules, def, key),
                Sample = def,
            };
        }

        private static float ResolveCap(AffixRules rules, AffixDef def, string key)
        {
            if (rules.Caps.TryGetValue(key, out float cap))
            {
                return cap;
            }
            string effectKey = def.Condition == AffixCondition.HealthCritical ? def.Effect + "@health_critical" : def.Effect;
            if (rules.Caps.TryGetValue(effectKey, out cap))
            {
                return cap;
            }
            return def.EffectDef.DefaultCap ?? float.PositiveInfinity;
        }

        private static void BuildPools(AffixRules rules)
        {
            Dictionary<ItemSlot, List<AffixDef>> regular = new Dictionary<ItemSlot, List<AffixDef>>();
            Dictionary<ItemSlot, List<AffixDef>> mythic = new Dictionary<ItemSlot, List<AffixDef>>();
            foreach (AffixDef def in rules.Affixes)
            {
                if (!def.Enabled || def.Weight <= 0f)
                {
                    continue;
                }
                foreach (ItemSlot slot in def.Slots)
                {
                    Add(def.MythicOnly ? mythic : regular, slot, def);
                }
            }
            rules.RegularPools = Freeze(regular);
            rules.MythicPools = Freeze(mythic);
        }

        private static void Add(Dictionary<ItemSlot, List<AffixDef>> pools, ItemSlot slot, AffixDef def)
        {
            if (!pools.TryGetValue(slot, out List<AffixDef> list))
            {
                list = new List<AffixDef>();
                pools[slot] = list;
            }
            list.Add(def);
        }

        private static Dictionary<ItemSlot, AffixDef[]> Freeze(Dictionary<ItemSlot, List<AffixDef>> pools)
        {
            Dictionary<ItemSlot, AffixDef[]> frozen = new Dictionary<ItemSlot, AffixDef[]>();
            foreach (KeyValuePair<ItemSlot, List<AffixDef>> pair in pools)
            {
                frozen[pair.Key] = pair.Value.ToArray();
            }
            return frozen;
        }
    }
}
