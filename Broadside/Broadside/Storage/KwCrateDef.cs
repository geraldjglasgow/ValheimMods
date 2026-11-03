namespace Broadside
{
    /// <summary>
    /// One storage crate as plain data (<see cref="KwCrates"/>): its prefab (<c>BS_crate_&lt;id&gt;</c>), its workshop
    /// bundle and prefab (<c>bs_crate_&lt;id&gt;</c>, with the sprite <c>bs_crate_&lt;id&gt;_icon</c>), its name, its slots and
    /// its recipe (items by the game's prefab names, given back when it is removed).
    /// </summary>
    public sealed class KwCrateDef
    {
        public KwCrateDef(string id, string label, int width, int height, params (string Item, int Amount)[] cost)
        {
            (Prefab, Bundle, Label) = (KwCrates.Prefix + id, "bs_crate_" + id, label);
            (Width, Height, Cost) = (width, height, cost);
        }

        public string Prefab { get; }
        public string Bundle { get; }
        public string Label { get; }
        public int Width { get; }
        public int Height { get; }
        public (string Item, int Amount)[] Cost { get; }

        /// <summary>Whether each crate wears its own marks from a seed (<see cref="KwCrateCoat"/>): the slatted crates.</summary>
        public bool Marked { get; set; }

        public string Description => $"A {(Marked ? "slatted" : "rope-lashed plank")} crate with {Width * Height} slots, built on land or on a launched ship's deck.";
    }
}
