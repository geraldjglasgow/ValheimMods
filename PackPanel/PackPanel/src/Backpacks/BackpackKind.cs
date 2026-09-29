using PackPanel.Crafting;
using UnityEngine;

namespace PackPanel.Backpacks
{
    /// <summary>
    /// One of PackPanel's backpacks (<see cref="CraftedKind"/>): besides its item, the model it wears (a prefab of the
    /// embedded bundle, built in AssetWorkshop under the item's snake-case name) and its stats: the built-in defaults,
    /// and the ones in use (the YAML over the defaults, <see cref="BackpacksFile"/>). The item and the worn model are set
    /// when ZNetScene wakes (<see cref="BackpackPrefab"/>).
    /// </summary>
    public sealed class BackpackKind : CraftedKind
    {
        public BackpackKind(string id, string word, string name, string look, float weight, BackpackStats defaults)
            : base(id, "backpack", word, name, look, weight)
        {
            Defaults = defaults;
            Stats = defaults;
            Hash = id.GetStableHashCode();
        }

        public BackpackStats Defaults { get; }

        public BackpackStats Stats { get; internal set; }

        /// <summary>The stable hash of <see cref="CraftedKind.Id"/>: what a wearer's ZDO holds.</summary>
        public int Hash { get; }

        public GameObject Worn { get; internal set; }

        public override CraftStats Recipe => Stats;

        public override CraftStats DefaultRecipe => Defaults;

        public override bool Craftable => BackpackSettings.Active;
    }
}
