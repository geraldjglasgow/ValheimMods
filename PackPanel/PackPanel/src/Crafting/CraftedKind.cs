using UnityEngine;

namespace PackPanel.Crafting
{
    /// <summary>
    /// One of PackPanel's own crafted items, a backpack or a tacklebox: its item prefab name (the name ZNetScene and ObjectDB
    /// know it by, and its key in the item's YAML file), its group and word (its $packpanel_ words and icon: its name and
    /// look are in <c>translations/English.yml</c>), its item weight and its recipe. The item is set when ZNetScene wakes;
    /// a kind whose model is missing from its bundle keeps it null and is left out.
    /// </summary>
    public abstract class CraftedKind
    {
        protected CraftedKind(string id, string group, string word, float weight)
        {
            Id = id;
            Group = group;
            Word = word;
            Weight = weight;
        }

        /// <summary>The item prefab, "PackPanel_TrollhideBackpack".</summary>
        public string Id { get; }

        /// <summary>"backpack" or "tacklebox": the middle of its words and its icon's name.</summary>
        public string Group { get; }

        /// <summary>The short word in its $packpanel_ words and its icon, "trollhide".</summary>
        public string Word { get; }

        public float Weight { get; }

        /// <summary>The game's highest quality of the item: 1, no upgrades (a backpack has levels, <see cref="Backpacks.BackpackLevels"/>).</summary>
        public virtual int MaxQuality => 1;

        public GameObject Item { get; internal set; }

        /// <summary>The game's item type of the item: a Misc item, which the game neither equips nor uses.</summary>
        public virtual ItemDrop.ItemData.ItemType ItemType => ItemDrop.ItemData.ItemType.Misc;

        /// <summary>The recipe in use: the YAML over the built-in default.</summary>
        public abstract CraftStats Recipe { get; }

        public abstract CraftStats DefaultRecipe { get; }

        /// <summary>Whether the settings let it be crafted now.</summary>
        public abstract bool Craftable { get; }

        /// <summary>The bundle's prefab of the model: the AssetWorkshop asset, "packpanel_trollhide_backpack".</summary>
        public string Asset => "packpanel_" + Snake(Id.Substring("PackPanel_".Length));

        /// <summary>The key of its name, "packpanel_backpack_trollhide"; its item's shared name is the token of it.</summary>
        public string Key => $"packpanel_{Group}_{Word}";

        public string Token => "$" + Key;

        /// <summary>The key of its look, the first sentence of its description.</summary>
        public string LookKey => Key + "_look";

        /// <summary>Its description's word, made from the words and its numbers at runtime (<see cref="CraftedWords"/>).</summary>
        public string DescriptionToken => Token + "_description";

        public string IconResource => $"PackPanel.assets.{Group}_{Word}.png";

        /// <summary>"DeerhideSatchel" to "deerhide_satchel".</summary>
        private static string Snake(string camel)
        {
            System.Text.StringBuilder text = new System.Text.StringBuilder();
            foreach (char c in camel)
            {
                if (char.IsUpper(c) && text.Length > 0)
                    text.Append('_');
                text.Append(char.ToLowerInvariant(c));
            }
            return text.ToString();
        }
    }
}
