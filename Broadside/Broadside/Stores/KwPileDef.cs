using System;

namespace Broadside
{
    /// <summary>
    /// One ammunition pile as plain data (<see cref="KwPiles"/>): its prefab (<c>BS_pile_&lt;id&gt;</c>), its workshop bundle
    /// and prefab (<c>bs_pile_&lt;id&gt;</c>, with the sprite <c>bs_pile_&lt;id&gt;_icon</c>), its name, its slots, its recipe
    /// (items by the game's prefab names, given back when it is removed) and the one sort of item it holds.
    /// </summary>
    public sealed class KwPileDef
    {
        public KwPileDef(string id, string label, string holds, Func<ItemDrop.ItemData, bool> accepts, params (string Item, int Amount)[] cost)
        {
            (Prefab, Bundle, Label, Holds) = (KwPiles.Prefix + id, "bs_pile_" + id, label, holds);
            (Accepts, Cost) = (accepts, cost);
        }

        public string Prefab { get; }
        public string Bundle { get; }
        public string Label { get; }

        /// <summary>What it holds, in words ("cannonballs").</summary>
        public string Holds { get; }

        /// <summary>Whether an item may go in: the one sort it holds.</summary>
        public Func<ItemDrop.ItemData, bool> Accepts { get; }

        public (string Item, int Amount)[] Cost { get; }

        public int Width => KwPiles.Width;
        public int Height => KwPiles.Height;

        public string Description =>
            $"Holds only {Holds}, {Width * Height} slots. Built on land or on a launched ship's deck, where the captain's broadside reloads from it.";
    }
}
