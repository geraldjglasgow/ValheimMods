using System.Runtime.CompilerServices;
using EliteCrafting.Affixes;

namespace EliteCrafting.Effects
{
    /// <summary>
    /// Per-item cache of <see cref="ItemLocalSums"/>, keyed weakly on the <c>ItemData</c> object. An entry remembers the
    /// <see cref="ItemState"/> instance it was built from; the state cache hands out a new instance whenever the item's
    /// data is written, reloaded by the game or re-resolved against new rules, so a reference compare is the whole
    /// validity check. <see cref="ItemStateCache.Written"/> also drops the entry at once (<see cref="Forget"/>).
    /// <para>
    /// Hot path: a plain item (no custom data) costs one dictionary-count check inside <see cref="ItemState.Read"/>;
    /// an item with state costs that lookup plus one here and a reference compare. No allocation after the first
    /// sight of an item object.
    /// </para>
    /// </summary>
    internal static class ItemLocalCache
    {
        private sealed class Entry
        {
            public ItemState? State;
            public ItemLocalSums? Sums;
        }

        private static readonly ConditionalWeakTable<ItemDrop.ItemData, Entry> Table =
            new ConditionalWeakTable<ItemDrop.ItemData, Entry>();

        private static readonly ConditionalWeakTable<ItemDrop.ItemData, Entry>.CreateValueCallback NewEntry = _ => new Entry();

        /// <summary>The item's local numbers, or null when it has none or affix effects are off (the getters then do nothing).</summary>
        public static ItemLocalSums? Get(ItemDrop.ItemData? item)
        {
            if (item == null || !ItemEffects.Enabled)
            {
                return null;
            }
            ItemState state = ItemState.Read(item);
            if (state.IsEmpty)
            {
                return null;
            }
            Entry entry = Table.GetValue(item, NewEntry);
            if (!ReferenceEquals(entry.State, state))
            {
                entry.Sums = ItemLocalSums.Build(item, state);
                entry.State = state;
            }
            return entry.Sums;
        }

        public static void Forget(ItemDrop.ItemData item) => Table.Remove(item);
    }
}
