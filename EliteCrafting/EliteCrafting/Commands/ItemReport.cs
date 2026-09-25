using System.Collections.Generic;
using System.Text;
using EliteCrafting.Affixes;
using EliteCrafting.Core;
using EliteCrafting.Items;
using EliteCrafting.Rules;
using EliteCrafting.Text;

namespace EliteCrafting.Commands
{
    /// <summary>
    /// The <c>inspect</c> block for one item (console-commands.md section 3): headline, the raw <c>ecf_</c> keys, the
    /// rarity and format, one line per affix with its effect and whether it is active, the unreadable segments, then
    /// refine, seal, sigil and binding. The raw line is what two players compare in the multiplayer checklist, so it
    /// prints the stored strings exactly, in a fixed key order.
    /// </summary>
    internal static class ItemReport
    {
        public static void Write(CommandCall call, ItemDrop.ItemData item, string? where = null)
        {
            ItemState state = ItemState.Read(item);
            call.Reply(Headline(item) + (where != null ? $", from {where}" : ""));
            call.Detail("raw: " + Raw(item.m_customData));
            call.Detail(RarityLine(state));
            for (int i = 0; i < state.AffixCount; i++)
            {
                call.Detail(AffixLine(state, i));
            }
            foreach (string raw in state.Unreadable)
            {
                call.Detail($"unreadable segment '{raw}' (kept as it is)");
            }
            call.Detail(FlagsLine(state));
        }

        private static string Headline(ItemDrop.ItemData item)
        {
            SlotInfo info = ItemSlots.Classify(item);
            string slot = info.Slot == ItemSlot.None ? "-" : ItemSlots.Id(info.Slot);
            string kind = ItemSlots.IsMagicBase(item) ? "magic base" : ItemSlots.IsStone(item) ? (Salvage.Fuser.IsShard(item) ? "a shard, not a magic base" : "a stone, not a magic base") : "not a magic base";
            return $"{ItemText.Name(item)} [{item.m_quality}] ({ItemText.Prefab(item)}), slot {slot}, "
                + $"tier ceiling {ItemTier.Of(item)}, {kind}";
        }

        /// <summary>Every <c>ecf_</c> key as stored: the version, the state keys in order, then any other <c>ecf_</c> key.</summary>
        public static string Raw(Dictionary<string, string>? data)
        {
            if (data == null || data.Count == 0)
            {
                return "(no custom data)";
            }
            StringBuilder sb = new StringBuilder();
            Append(sb, data, ItemKeys.Version);
            foreach (string key in ItemKeys.StateKeys)
            {
                Append(sb, data, key);
            }
            AppendOthers(sb, data, out int foreign);
            string text = sb.Length == 0 ? "(no ecf_ data)" : sb.ToString();
            return foreign > 0 ? $"{text} (+{foreign} keys of other mods)" : text;
        }

        private static void Append(StringBuilder sb, Dictionary<string, string> data, string key)
        {
            if (data.TryGetValue(key, out string value))
            {
                sb.Append(sb.Length > 0 ? " | " : "").Append(key).Append('=').Append(value);
            }
        }

        private static void AppendOthers(StringBuilder sb, Dictionary<string, string> data, out int foreign)
        {
            foreign = 0;
            List<string> extra = new List<string>();
            foreach (string key in data.Keys)
            {
                bool ours = key.StartsWith(ItemKeys.Prefix, System.StringComparison.Ordinal);
                bool known = key == ItemKeys.Version || System.Array.IndexOf(ItemKeys.StateKeys, key) >= 0;
                foreign += ours ? 0 : 1;
                if (ours && !known)
                {
                    extra.Add(key);
                }
            }
            extra.Sort(System.StringComparer.Ordinal);
            extra.ForEach(key => Append(sb, data, key));
        }

        private static string RarityLine(ItemState state)
        {
            string rarity = !state.IsMagic ? "rarity common"
                : state.Rarity == null ? $"rarity {state.RarityId} (unknown: not in the configuration)"
                : $"rarity {state.RarityId} ({state.Rarity.Color}), {state.Rarity.MinAffixes}-{state.Rarity.MaxAffixes} affixes";
            string format = state.IsEmpty ? "no format key" : $"format v{state.Format}";
            return state.IsNewerFormat ? $"{rarity}, {format} (newer than this version: stones refuse)" : $"{rarity}, {format}";
        }

        private static string AffixLine(ItemState state, int index)
        {
            AffixRoll roll = state.Affixes[index];
            AffixDef? def = state.DefinitionAt(index);
            string head = $"affix {roll.Id} T{roll.Tier} {Numbers.Format(roll.Value)} -> ";
            string bound = state.IsBoundAt(index) ? ", bound" : "";
            if (def == null)
            {
                return head + "dormant (not in configuration)" + bound;
            }
            string effect = def.Param == null ? def.Effect : $"{def.Effect}:{def.Param}";
            string status = def.Enabled ? "active" : "dormant (disabled)";
            string tier = def.TierRow(roll.Tier) == null ? ", tier not defined any more" : "";
            return $"{head}effect {effect}, {status}{tier}{bound}";
        }

        private static string FlagsLine(ItemState state)
        {
            string refine = state.Refine != 0f ? $"refine +{Numbers.Format(state.Refine)}%" : "no refine";
            string seal = state.IsSealed ? $"sealed ({state.SealedReason})" : "not sealed";
            string sigil = state.HasSigil ? $"sigil {state.SigilId}" : "no sigil";
            string bound = state.BoundId != null ? $"bound {state.BoundId}" : "not bound";
            return $"{refine}, {seal}, {sigil}, {bound}";
        }
    }

    /// <summary>Names of an item for console output.</summary>
    internal static class ItemText
    {
        /// <summary>The display name in the player's language (the raw token before the game is up).</summary>
        public static string Name(ItemDrop.ItemData item) => Words.Localize(item.m_shared?.m_name ?? "?");

        public static string Prefab(ItemDrop.ItemData item) => ItemTier.PrefabName(item) ?? "?";
    }
}
