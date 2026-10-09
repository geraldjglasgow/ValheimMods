using System.Collections.Generic;
using EliteEquipment.Core;

namespace EliteEquipment.Boots
{
    /// <summary>
    /// The 20 leggings the workshop split (every player leggings of the game as of 2026-10-06, Deep North included; the
    /// workshop's <c>Assets/Gear/SeparatedLegArmor/NativeSplit_v001</c>) and their boots' English names, made from the leggings' own
    /// ("Iron Greaves" gives "Iron Boots", "Trousers of Ask" gives "Boots of Ask"; light cloth sets wear shoes). Items are
    /// told apart by their name token, which every live copy of an item carries.
    /// </summary>
    public static class BootSets
    {
        private static readonly List<BootSet> all = new List<BootSet>();
        private static readonly Dictionary<string, BootSet> byToken = new Dictionary<string, BootSet>();
        private static readonly Dictionary<string, BootSet> byLegs = new Dictionary<string, BootSet>();
        private static readonly Dictionary<string, BootSet> byLegsToken = new Dictionary<string, BootSet>();
        private static readonly Dictionary<int, BootSet> byHash = new Dictionary<int, BootSet>();
        private static readonly Dictionary<int, BootSet> byLegsHash = new Dictionary<int, BootSet>();

        public static IReadOnlyList<BootSet> All => all;

        /// <summary>Registers the sets and their words; called once from <see cref="BootsModule"/>.</summary>
        public static void Initialize()
        {
            if (all.Count > 0)
                return;
            Add("Rag", "ArmorRagsLegs", "Rag Shoes");
            Add("Leather", "ArmorLeatherLegs", "Leather Boots");
            Add("TrollLeather", "ArmorTrollLeatherLegs", "Troll Leather Boots");
            Add("Bronze", "ArmorBronzeLegs", "Bronze Plate Boots");
            Add("Bear", "ArmorBerserkerLegs", "Boots of the Bear");
            Add("Iron", "ArmorIronLegs", "Iron Boots");
            Add("Root", "ArmorRootLegs", "Root Boots");
            Add("Fenris", "ArmorFenringLegs", "Fenris Boots");
            Add("Wolf", "ArmorWolfLegs", "Wolf Hide Boots");
            Add("Vilebone", "ArmorBerserkerUndeadLegs", "Vilebone Boots");
            Add("Lox", "ArmorLoxLegs", "Lox Fur Boots");
            Add("Padded", "ArmorPaddedGreaves", "Padded Boots");
            Add("Carapace", "ArmorCarapaceLegs", "Carapace Boots");
            Add("EitrWeave", "ArmorMageLegs", "Eitr-weave Shoes");
            Add("Ask", "ArmorAshlandsMediumlegs", "Boots of Ask");
            Add("Flametal", "ArmorFlametalLegs", "Flametal Boots");
            Add("Embla", "ArmorMageLegs_Ashlands", "Shoes of Embla");
            Add("Protector", "ArmorDeepNorthHeavylegs", "Boots of the Protector");
            Add("Caller", "ArmorDeepNorthMagelegs", "Shoes of the Caller");
            Add("Vanguard", "ArmorDeepNorthMediumlegs", "Boots of the Vanguard");
        }

        /// <summary>True for any copy of a boots item, by its name token.</summary>
        public static bool Is(ItemDrop.ItemData item) => item != null && item.m_shared != null && byToken.ContainsKey(item.m_shared.m_name);

        public static BootSet Of(ItemDrop.ItemData item) =>
            item != null && item.m_shared != null && byToken.TryGetValue(item.m_shared.m_name, out BootSet set) ? set : null;

        /// <summary>The set whose leggings prefab has this name, or null.</summary>
        public static BootSet ByLegs(string legsPrefab) =>
            legsPrefab != null && byLegs.TryGetValue(legsPrefab, out BootSet set) ? set : null;

        /// <summary>The set whose leggings carry this name token (filled as the leggings are found), or null.</summary>
        public static BootSet ByLegsToken(string token) =>
            token != null && byLegsToken.TryGetValue(token, out BootSet set) ? set : null;

        /// <summary>Notes the leggings' name token once their prefab is found (<see cref="BootsItems"/>).</summary>
        public static void FoundLegs(BootSet set, string legsToken)
        {
            if (!string.IsNullOrEmpty(legsToken))
                byLegsToken[legsToken] = set;
        }

        /// <summary>The set whose boots prefab has this hash, or null.</summary>
        public static BootSet ByHash(int bootsHash) => byHash.TryGetValue(bootsHash, out BootSet set) ? set : null;

        /// <summary>The set whose leggings prefab has this hash, or null.</summary>
        public static BootSet ByLegsHash(int legsHash) => byLegsHash.TryGetValue(legsHash, out BootSet set) ? set : null;

        private static void Add(string key, string legs, string name)
        {
            string token = Language.Add("ee_boots_" + key.ToLowerInvariant(), name);
            var set = new BootSet(key, legs, token);
            all.Add(set);
            byToken[token] = set;
            byLegs[legs] = set;
            byHash[set.Hash] = set;
            byLegsHash[set.LegsHash] = set;
        }
    }
}
