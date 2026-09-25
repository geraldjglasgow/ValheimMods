using System;
using System.Collections.Generic;
using EliteCrafting.Rules;

namespace EliteCrafting.Affixes
{
    /// <summary>
    /// An item's EliteCrafting state (item-data.md): rarity, affixes, bound affix, refine bonus, sealed reason, pending
    /// sigil and format version, resolved against the running rules. Immutable: to change an item, take
    /// <see cref="ToBuilder"/>, change the builder, and hand the result to <see cref="Write"/> - the only write path.
    /// <para>
    /// Read an item with <see cref="Read"/>: a cache hit is one table lookup and one reference compare, and a plain
    /// item (no custom data) returns <see cref="Empty"/> without any lookup, so hot paths may call it on every item.
    /// </para>
    /// </summary>
    public sealed class ItemState
    {
        public static readonly ItemState Empty = new ItemState(StateData.Empty, RuleSet.Empty);

        private readonly AffixRoll[] _rolls;
        private readonly AffixDef?[] _defs;
        private readonly string[] _unreadable;

        internal ItemState(StateData data, RuleSet rules)
        {
            Data = data;
            Generation = rules.Generation;
            _rolls = Rolls(data.Segments, out _unreadable);
            _defs = Resolve(_rolls, rules.Affixes);
            RarityId = data.RarityId;
            Rarity = rules.Rarity(data.RarityId);
        }

        internal StateData Data { get; }

        /// <summary>The rules generation this state was resolved against.</summary>
        public int Generation { get; }

        // ---- the stored data

        /// <summary>Format version (current after in-memory migration).</summary>
        public int Format => Data.Format;

        /// <summary>Written by a newer mod version: effects of parseable affixes apply, every stone refuses.</summary>
        public bool IsNewerFormat => Data.Newer;

        /// <summary>Rarity id; null = Common (the base rarity is never stored).</summary>
        public string? RarityId { get; }

        /// <summary>The parsed affixes in display order (unreadable segments left out).</summary>
        public IReadOnlyList<AffixRoll> Affixes => _rolls;

        /// <summary>Segments of <c>ecf_affixes</c> that did not parse, kept verbatim and written back in place.</summary>
        public IReadOnlyList<string> Unreadable => _unreadable;

        /// <summary>The bound affix's id; always one of <see cref="Affixes"/> when set.</summary>
        public string? BoundId => Data.BoundId;

        /// <summary>Honed/tempered bonus in percent points; 0 when none.</summary>
        public float Refine => Data.Refine;

        public string? SealedReason => Data.SealedReason;
        public string? SigilId => Data.SigilId;

        // ---- resolved against the rules

        /// <summary>The rarity definition; null for Common and for an unknown rarity id.</summary>
        public RarityDef? Rarity { get; }

        /// <summary>Uncommon or better: the item carries a rarity id (known or not).</summary>
        public bool IsMagic => RarityId != null;

        public bool IsUnknownRarity => RarityId != null && Rarity == null;
        public bool IsSealed => Data.SealedReason != null;
        public bool HasSigil => Data.SigilId != null;
        public bool HasAffixes => _rolls.Length > 0;
        public int AffixCount => _rolls.Length;

        /// <summary>No <c>ecf_</c> state at all: a plain vanilla item.</summary>
        public bool IsEmpty => Data.IsEmpty;

        /// <summary>The live definition of affix <paramref name="index"/>; null when orphaned (id unknown).</summary>
        public AffixDef? DefinitionAt(int index) => _defs[index];

        /// <summary>Active = defined and enabled; everything else is dormant (kept, shown greyed, inert).</summary>
        public bool IsActiveAt(int index) => _defs[index] != null && _defs[index]!.Enabled;

        public bool IsBoundAt(int index) => Data.BoundId != null && _rolls[index].Id == Data.BoundId;

        public int IndexOf(string id) => Array.FindIndex(_rolls, r => r.Id == id);

        public bool HasAffix(string id) => IndexOf(id) >= 0;

        public ItemStateBuilder ToBuilder() => new ItemStateBuilder(Data);

        // ---- entry points

        /// <summary>The item's state, from the cache (see <see cref="ItemStateCache"/>).</summary>
        public static ItemState Read(ItemDrop.ItemData? item) => ItemStateCache.Read(item);

        /// <summary>Writes the state into the item's custom data; the only write path. False (and an error log) when refused.</summary>
        public static bool Write(ItemDrop.ItemData item, ItemState state) => ItemStateCache.Write(item, state);

        /// <summary>Parses a custom-data dictionary without the cache (commands, tests).</summary>
        public static ItemState Parse(Dictionary<string, string> data) => new ItemState(ItemCodec.Parse(data), ActiveRules.Current);

        /// <summary>The same data resolved against another rules snapshot (no string work).</summary>
        internal ItemState Resolve(RuleSet rules) => Data.IsEmpty ? Empty : new ItemState(Data, rules);

        private static AffixRoll[] Rolls(ItemSegment[] segments, out string[] unreadable)
        {
            List<AffixRoll> rolls = new List<AffixRoll>(segments.Length);
            List<string> raw = new List<string>();
            foreach (ItemSegment segment in segments)
            {
                if (segment.IsRoll)
                {
                    rolls.Add(segment.Roll);
                }
                else
                {
                    raw.Add(segment.Raw!);
                }
            }
            unreadable = raw.Count == 0 ? Array.Empty<string>() : raw.ToArray();
            return rolls.Count == 0 ? Array.Empty<AffixRoll>() : rolls.ToArray();
        }

        private static AffixDef?[] Resolve(AffixRoll[] rolls, AffixRules rules)
        {
            AffixDef?[] defs = rolls.Length == 0 ? Array.Empty<AffixDef?>() : new AffixDef?[rolls.Length];
            for (int i = 0; i < rolls.Length; i++)
            {
                defs[i] = rules.Get(rolls[i].Id);
            }
            return defs;
        }
    }
}
