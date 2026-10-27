using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What a broken-up ship leaves (<see cref="KwShipBreakSpill"/>), plain numbers only: its cost in stacks of at most each
    /// item's stack size, how many crates of a size they fill, and where the crates lie in the ship's own frame: in rows
    /// along the hull, every row at the same share of its length, side by side across it <see cref="Spacing"/> apart.
    /// </summary>
    public static class KwShipBreakPlan
    {
        /// <summary>How far apart the crates lie, at least along the hull and always across it (a crate is about a metre).</summary>
        public const float Spacing = 1.4f;

        /// <summary>A cost in stacks, item by item in the cost's order, each at most its item's stack size; an item the game lacks (size 0) left out.</summary>
        public static List<(string Item, int Count)> Stacks(IEnumerable<(string Item, int Amount)> cost, Func<string, int> stackSize)
        {
            List<(string Item, int Count)> stacks = new List<(string Item, int Count)>();
            foreach ((string item, int amount) in cost)
            {
                int most = stackSize(item);
                if (most <= 0)
                    continue;
                for (int left = amount; left > 0; left -= most)
                    stacks.Add((item, Math.Min(left, most)));
            }
            return stacks;
        }

        /// <summary>How many crates of so many slots the stacks fill, one stack a slot.</summary>
        public static int Crates(int stacks, int slots) => stacks <= 0 ? 0 : (stacks + Math.Max(slots, 1) - 1) / Math.Max(slots, 1);

        /// <summary>
        /// Where so many crates lie in the ship's frame (x across, y along, z unused): as many rows along the hull from aft
        /// to fore as fit <see cref="Spacing"/> apart (one at least), each in the middle of its share of the length, the
        /// crates of a row centred across the hull.
        /// </summary>
        public static List<Vector2> Places(int count, float aft, float fore)
        {
            List<Vector2> places = new List<Vector2>();
            if (count <= 0)
                return places;
            float length = Mathf.Max(fore - aft, 0.5f);
            int rows = Mathf.Clamp(Mathf.FloorToInt(length / Spacing), 1, count);
            int across = (count + rows - 1) / rows;
            rows = (count + across - 1) / across;
            for (int i = 0; i < count; i++)
            {
                int row = i / across, inRow = Math.Min(across, count - row * across);
                float x = (i % across - (inRow - 1) / 2f) * Spacing;
                places.Add(new Vector2(x, aft + length * (row + 0.5f) / rows));
            }
            return places;
        }
    }
}
