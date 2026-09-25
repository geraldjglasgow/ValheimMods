using System;
using System.Collections.Generic;
using EliteCrafting.Rules;

namespace EliteCrafting.Affixes
{
    /// <summary>
    /// A mutable working copy of an item's state, for stones, drops and commands. Nothing reaches the item until
    /// <see cref="Build"/> is handed to <see cref="ItemState.Write"/>, so a stone can try an operation and walk away on
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
        public string? BoundId => _data.BoundId;
        public float Refine => _data.Refine;
        public string? SealedReason => _data.SealedReason;
        public string? SigilId => _data.SigilId;

        /// <summary>The parsed affixes, in order (unreadable segments left out).</summary>
        public List<AffixRoll> Affixes => _segments.FindAll(s => s.IsRoll).ConvertAll(s => s.Roll);

        public int AffixCount => _segments.FindAll(s => s.IsRoll).Count;

        public bool HasAffix(string id) => IndexOf(id) >= 0;

        /// <summary>Sets the rarity; null or the base rarity's id makes the item Common (the key is removed).</summary>
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
                throw new InvalidOperationException($"affix '{roll.Id}' is already on the item");
            }
            _segments.Add(new ItemSegment(roll));
            return this;
        }

        /// <summary>Removes an affix (and the binding, if it was the bound one). False when absent.</summary>
        public bool RemoveAffix(string id)
        {
            int index = IndexOf(id);
            if (index < 0)
            {
                return false;
            }
            _segments.RemoveAt(index);
            if (_data.BoundId == id)
            {
                _data.BoundId = null;
            }
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
            if (_data.BoundId == id && roll.Id != id)
            {
                _data.BoundId = null;
            }
            return true;
        }

        /// <summary>Removes every parsed affix and the binding; unreadable segments stay unless <paramref name="unreadableToo"/>.</summary>
        public ItemStateBuilder ClearAffixes(bool unreadableToo = false)
        {
            _segments.RemoveAll(s => s.IsRoll || unreadableToo);
            _data.BoundId = null;
            return this;
        }

        /// <summary>Binds an affix on the item (null unbinds). Throws when the id is not on the item.</summary>
        public ItemStateBuilder SetBound(string? id)
        {
            if (id != null && !HasAffix(id))
            {
                throw new InvalidOperationException($"cannot bind '{id}': not on the item");
            }
            _data.BoundId = id;
            return this;
        }

        public ItemStateBuilder SetRefine(float percent)
        {
            _data.Refine = percent;
            _data.RefineRaw = null;
            return this;
        }

        /// <summary>Seals with a reason id (<see cref="ItemKeys.SealedSerpent"/>...); null unseals (no stone does).</summary>
        public ItemStateBuilder Seal(string? reason)
        {
            _data.SealedReason = string.IsNullOrEmpty(reason) ? null : reason;
            return this;
        }

        public ItemStateBuilder SetSigil(string? stoneId)
        {
            _data.SigilId = string.IsNullOrEmpty(stoneId) ? null : stoneId;
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
