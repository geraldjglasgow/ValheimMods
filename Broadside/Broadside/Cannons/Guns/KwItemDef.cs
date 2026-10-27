using System.Collections.Generic;

namespace Broadside
{
    /// <summary>
    /// A set of cannon items (ammunition, materials), found by reflection (<see cref="KwArmory"/>): any class with a
    /// parameterless constructor that implements this, in its own file, adds its items to the game; nothing shared needs
    /// editing. A class may be a weapon set too.
    /// </summary>
    public interface IKwItemSet
    {
        IEnumerable<KwItemDef> Items();
    }

    /// <summary>
    /// An item of the cannons' own (<see cref="KwItems"/> makes it): a networked copy of a game item (its drop physics,
    /// pickup and sounds) named <see cref="Name"/>, wearing a model built in code from the game's materials and an icon
    /// rendered from it, stacking as it says, made at a crafting station from its cost: its tier's station
    /// (<see cref="KwTiers.Station"/>) unless it names its own. Model kit axes: x forward, y left, z up, metres, the
    /// origin under the model's middle.
    /// </summary>
    public sealed class KwItemDef
    {
        public KwItemDef(string name, string label, int tier)
        {
            Name = name;
            Label = label;
            Tier = tier;
        }

        /// <summary>The prefab name, unique in the game ("BS_BlackPowder"); the label is its name in inventories, unique too.</summary>
        public string Name { get; }
        public string Label { get; }
        public int Tier { get; }
        public string Description { get; set; } = "";

        /// <summary>The game item copied for its drop physics, pickup and sounds; its model and colliders are replaced.</summary>
        public string Base { get; set; } = "Coal";
        public ItemDrop.ItemData.ItemType Type { get; set; } = ItemDrop.ItemData.ItemType.Material;
        public int Stack { get; set; } = 50;
        public float Weight { get; set; } = 1f;
        public int Value { get; set; }
        public bool Teleports { get; set; } = true;

        /// <summary>An ammunition type of its own, for an item a game weapon could otherwise take; empty for none.</summary>
        public string AmmoType { get; set; } = "";

        public List<(KwLook Look, System.Action<MeshBuilder> Shape)> Parts { get; } = new List<(KwLook Look, System.Action<MeshBuilder> Shape)>();

        /// <summary>What a craft takes, how many it makes, and where: a station prefab ("" by hand) and level, or the tier's.</summary>
        public List<(string Item, int Amount)> Cost { get; } = new List<(string Item, int Amount)>();
        public int Amount { get; set; } = 1;
        public string? Station { get; set; }
        public int StationLevel { get; set; } = 1;

        /// <summary>Last touches to the item's copy once its model is made and before its icon is rendered, with that copy (parts in a game material no look has, say); none by default.</summary>
        public System.Action<UnityEngine.GameObject>? Finish { get; set; }

        /// <summary>Adds a part of the model in one look.</summary>
        public KwItemDef Part(KwLook look, System.Action<MeshBuilder> shape)
        {
            Parts.Add((look, shape));
            return this;
        }
    }
}
