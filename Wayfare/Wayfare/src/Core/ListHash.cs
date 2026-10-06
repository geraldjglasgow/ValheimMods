using System;
using UnityEngine;

namespace Wayfare.Core
{
    /// <summary>A list's contents folded into one 64-bit number, in order, so the server can tell whether the list a client
    /// already holds is still current without sending the list again. A change to any value, a new entry or a missing
    /// one gives another number (in practice: two lists colliding would take billions of years of changes). Never 0, which
    /// stands for "no list held".</summary>
    internal struct ListHash
    {
        private ulong value;

        /// <summary>A new hash of a list with <paramref name="count"/> entries.</summary>
        public static ListHash Start(int count)
        {
            ListHash hash = new ListHash { value = 0x9E3779B97F4A7C15UL };
            hash.Add(count);
            return hash;
        }

        public long Value => value != 0UL ? (long)value : 1L;

        public void Add(long part) => value = Mix(value ^ (ulong)part);

        public void Add(int part) => Add((long)part);

        public void Add(uint part) => Add((long)part);

        /// <summary>Exactly: a float widens to a double without loss.</summary>
        public void Add(float part) => Add(BitConverter.DoubleToInt64Bits(part));

        public void Add(Vector3 part)
        {
            Add(part.x);
            Add(part.y);
            Add(part.z);
        }

        /// <summary>The finaliser of SplitMix64: every bit of the input moves about half the bits of the output.</summary>
        private static ulong Mix(ulong x)
        {
            x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
            x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
            return x ^ (x >> 31);
        }
    }
}
