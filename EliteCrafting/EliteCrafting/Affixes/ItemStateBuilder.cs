using System;
using System.Collections.Generic;
using EliteCrafting.Rules;

namespace EliteCrafting.Affixes
{
    /// <summary>
    /// A mutable working copy of an item's state, for runes, drops and commands. Nothing reaches the item until
    /// <see cref="Build"/> is handed to <see cref="ItemState.Write"/>, so a rune can try an operation and walk away on
    /// failure. Unreadable segments stay in their positions unless explicitly cleared.
    /// </summary>
    public sealed class ItemStateBuilder
    {
        private readonly List<ItemSegment> _segments;
        private readonly StateData _data;

        internal ItemStateBuilder(StateData from)
        {
            _data = from.Copy();
            _segments = new List<ItemSegment>(from.Segments);
        }

        /// <summary>A builder for an item with no state yet.</summary>
        public static ItemStateBuilder New() => new ItemStateBuilder(StateData.Empty);

        public string? RarityId => _data.RarityId;
        public string? SealedReason => _data.SealedReason;

        /// <summary>The parsed affixes, in order (unreadable segments left out).</summary>
        public List<AffixRoll> Affixes => _segments.FindAll(s => s.IsRoll).ConvertAll(s => s.Roll);

        public int AffixCount => _segments.FindAll(s => s.IsRoll).Count;

        public bool HasAffix(string id) => IndexOf(id) >= 0;

        /// <summary>Sets the rarity; null or the base rarity's id makes the item Normal (the key is removed).</summary>
        public ItemStateBuilder SetRarity(string? rarityId)
        {
            RarityDef? rarity = ActiveRules.Current.Rarity(rarityId);
            _data.RarityId = rarity != null && rarity.IsBase ? null : rarityId;
            return this;
        }

        /// <summary>Appends an affix at the end. Throws when the id is already on the item (never two copies).</summary>
        public ItemStateBuilder AddAffix(AffixRoll roll)
        {
            if (HasAffix(roll.Id))
            {
                throw new InvalidOperationException($"inscription '{roll.Id}' is already on the item");
            }
            _segments.Add(new ItemSegment(roll));
            return this;
        }

        /// <summary>Removes an affix. False when absent.</summary>
        public bool RemoveAffix(string id)
        {
            int index = IndexOf(id);
            if (index < 0)
            {
                return false;
            }
            _segments.RemoveAt(index);
            return true;
        }

        /// <summary>Replaces an affix in place (a value reroll keeps its position). False when absent.</summary>
        public bool ReplaceAffix(string id, AffixRoll roll)
        {
            int index = IndexOf(id);
            if (index < 0 || (roll.Id != id && HasAffix(roll.Id)))
            {
                return false;
            }
            _segments[index] = new ItemSegment(roll);
            return true;
        }

        /// <summary>Removes every parsed affix; unreadable segments stay unless <paramref name="unreadableToo"/>.</summary>
        public ItemStateBuilder ClearAffixes(bool unreadableToo = false)
        {
            _segments.RemoveAll(s => s.IsRoll || unreadableToo);
            return this;
        }

        /// <summary>Seals with a reason id (<see cref="ItemKeys.SealedSerpent"/>); null unseals (no rune does).</summary>
        public ItemStateBuilder Seal(string? reason)
        {
            _data.SealedReason = string.IsNullOrEmpty(reason) ? null : reason;
            return this;
        }

        /// <summary>The finished state, resolved against the running rules, in the current format.</summary>
        public ItemState Build()
        {
            StateData data = _data.Copy();
            data.Segments = _segments.ToArray();
            if (!data.Newer)
            {
                data.Format = ItemKeys.CurrentFormat;
            }
            return data.IsEmpty ? ItemState.Empty : new ItemState(data, ActiveRules.Current);
        }

        private int IndexOf(string id) => _segments.FindIndex(s => s.IsRoll && s.Roll.Id == id);
    }
}
