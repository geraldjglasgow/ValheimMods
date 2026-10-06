using System;
using System.Collections.Generic;

namespace PackPanel.Slots
{
    /// <summary>
    /// One slot of the layout: its kind and its number within the kind (1 for the first food slot). The id
    /// ("food1") is what the layout record in the character's data stores, so an item keeps its slot when the grid or
    /// the other slot counts change.
    /// </summary>
    public sealed class Slot
    {
        /// <summary>Every kind in the enum's order, and each one's id prefix ("food"), worked out once rather than per slot.</summary>
        private static readonly SlotKind[] kinds = (SlotKind[])Enum.GetValues(typeof(SlotKind));
        private static readonly string[] prefixes = Prefixes();

        public Slot(SlotKind kind, int number)
        {
            Kind = kind;
            Number = number;
            Id = Prefix(kind) + number;
        }

        /// <summary>A slot of an old record whose kind this build no longer has; it keeps its id and its cell.</summary>
        public Slot(string id)
        {
            Kind = SlotKind.Retired;
            Number = 0;
            Id = id;
        }

        public SlotKind Kind { get; }

        public int Number { get; }

        public string Id { get; }

        public override string ToString() => Id;

        /// <summary>The kinds in the enum's order (the order a slot id is matched in).</summary>
        public static IReadOnlyList<SlotKind> Kinds => kinds;

        /// <summary>A kind's id prefix: its name in lower case.</summary>
        public static string Prefix(SlotKind kind)
        {
            int index = (int)kind;
            return index >= 0 && index < prefixes.Length && prefixes[index] != null ? prefixes[index] : kind.ToString().ToLowerInvariant();
        }

        private static string[] Prefixes()
        {
            int size = 0;
            foreach (SlotKind kind in kinds)
                size = Math.Max(size, (int)kind + 1);
            string[] names = new string[size];
            foreach (SlotKind kind in kinds)
                if ((int)kind >= 0)
                    names[(int)kind] = kind.ToString().ToLowerInvariant();
            return names;
        }
    }
}
