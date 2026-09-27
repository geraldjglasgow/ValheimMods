namespace GrindstoneSkills
{
    /// <summary>
    /// Every ZDO key, RPC name and player custom-data key GrindstoneSkills uses, in one place. All are GrindstoneSkills' own
    /// names, prefixed so they never meet another mod's.
    /// </summary>
    public static class Keys
    {
        // Cooking station slots, one set per slot index: append the slot number.
        /// <summary>long: player ID of the cook who put the food on.</summary>
        public const string SlotCook = "grindstone_cook";
        /// <summary>float: that cook's Cooking level when the food went on.</summary>
        public const string SlotLevel = "grindstone_level";
        /// <summary>float: stars of the raw input (dough, unbaked pies); 0 for plain raw food.</summary>
        public const string SlotInput = "grindstone_input";
        /// <summary>int: stars rolled when the dish turned done; -1 while not rolled.</summary>
        public const string SlotStars = "grindstone_stars";

        // Fermenter, one set per barrel.
        /// <summary>int: stars of the mead base in the barrel.</summary>
        public const string BaseStars = "grindstone_base_stars";
        /// <summary>float: Cooking level of the player who put the base in.</summary>
        public const string BaseLevel = "grindstone_base_level";

        /// <summary>int 0..3 on a kitchen's ZDO: dishes below this many stars are thrown away.</summary>
        public const string MinStars = "grindstone_min_stars";

        // RPCs. Payloads with more than four values travel as one ZPackage, written and read in the listed order.
        /// <summary>On a cooking station, to the owner. ZPackage: string item, bool cheated, float inputStars, float cookLevel, long cookPlayerId.</summary>
        public const string RpcAddItem = "grindstone_AddItem";
        /// <summary>On a fermenter, to the owner. ZPackage: int nameHash, bool cheated, int baseStars, float cookLevel.</summary>
        public const string RpcAddBase = "grindstone_AddBase";
        /// <summary>On a kitchen, to the owner: (int minStars).</summary>
        public const string RpcSetFilter = "grindstone_SetFilter";
        /// <summary>Routed RPC to everybody: (long cookPlayerId, string dishPrefab); only that player's client acts on it.</summary>
        public const string RpcCookCredit = "grindstone_CookCredit";

        // Player custom data (Player.m_customData, saved with the character).
        /// <summary>Prefix; append the food's prefab name. Value: the stars of that active food.</summary>
        public const string ActiveFood = "grindstone_food_";
        /// <summary>Comma-separated prefab names of every dish the character has made.</summary>
        public const string MadeDishes = "grindstone_made";
    }
}
