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

        // Sailing.
        /// <summary>float on a player's own ZDO, written by that player's client: their Sailing level.</summary>
        public const string SailingLevel = "grindstone_sailing_level";
        /// <summary>float on a ship's ZDO: the Sailing level of the player who built it, written when it is placed.</summary>
        public const string ShipwrightLevel = "grindstone_shipwright";
        /// <summary>On a ship, to everybody who has it loaded: () - a lookout pulse goes out from the ship.</summary>
        public const string RpcLookout = "grindstone_Lookout";

        /// <summary>The Sailing skill's identity: its SkillType number is this name's stable hash.</summary>
        public const string SailingSkillName = "grindstone_sailing";

        // Woodcutting. Log keys are written by the log's owner when it spawns; halves inherit them from their log.
        /// <summary>long on a log's ZDO: player ID of the woodcutter who felled its tree (0 when unknown).</summary>
        public const string WoodPlayer = "grindstone_wood_player";
        /// <summary>float on a log's ZDO: that woodcutter's Woodcutting level when the tree fell.</summary>
        public const string WoodLevel = "grindstone_wood_level";
        /// <summary>int on a log's ZDO: its domino depth; 0 for a tree felled by a swing, 1 for a tree a felled log knocked over.</summary>
        public const string WoodChain = "grindstone_wood_chain";
        /// <summary>bool on a log's ZDO: the log was split cleanly, so its wood gets the clean split bonus.</summary>
        public const string CleanSplit = "grindstone_clean_split";
        /// <summary>float 0..1 on a log's ZDO: how big its tree was within its kind's size range (old growth).</summary>
        public const string WoodSize = "grindstone_wood_size";
        /// <summary>Routed RPC to everybody. ZPackage: long playerId, int kind, string species, float amount, int chain; only that player's client acts on it.</summary>
        public const string RpcWoodCredit = "grindstone_WoodCredit";
        /// <summary>Routed RPC to everybody: (Vector3 position, string text); each client near the spot shows the text there.</summary>
        public const string RpcWoodCallout = "grindstone_WoodCallout";
        /// <summary>Player custom data: comma-separated species (log prefab names) of every tree the character has felled.</summary>
        public const string FelledTrees = "grindstone_felled";
        /// <summary>bool on a sapling's ZDO: Replanting planted it, so a felled log lying on it delays it instead of killing it.</summary>
        public const string Replanted = "grindstone_replanted";
        /// <summary>Charter sync key of the Finds YAML files (GrindstoneSkills.Finds*.yml).</summary>
        public const string FindsSync = "grindstone_finds";

        // Pickaxes. Nothing is stored on rocks: vein stars are computed from the world seed and the position.
        /// <summary>On a MineRock5, to its owner: (int area, ZDOID miner) - the miner's next hit on that chunk is a clean strike. Sent before the hit, kept in memory for 30 s.</summary>
        public const string RpcCleanStrike = "grindstone_CleanStrike";
        /// <summary>Routed RPC to everybody: (Vector3 position, string text); each client near the spot shows the text there, if its Pickaxes callouts are on.</summary>
        public const string RpcMineCallout = "grindstone_MineCallout";
        /// <summary>Player custom data: comma-separated identities of every ore deposit the character has hit: its name token ("$piece_deposit_copper"), else its kind (prefab name without "_frac").</summary>
        public const string MinedDeposits = "grindstone_mined";
        /// <summary>Charter sync key of the Mine Finds YAML files (GrindstoneSkills.MineFinds*.yml).</summary>
        public const string MineFindsSync = "grindstone_mine_finds";

        // Foraging.
        /// <summary>The Foraging skill's identity: its SkillType number is this name's stable hash.</summary>
        public const string ForagingSkillName = "grindstone_foraging";
        /// <summary>float on a player's own ZDO, written by that player's client: their Foraging level (CustomSkills publishes it).</summary>
        public const string ForagingLevel = "grindstone_foraging_level";
        /// <summary>On a forage pickable, to its owner: (float level) - the sender's next pick of it rolls stars at this effective level. Sent just before the game's RPC_Pick, kept in memory for 10 s.</summary>
        public const string RpcForageMark = "grindstone_ForageMark";
        /// <summary>Player custom data: comma-separated item prefab names of every kind of forage the character has picked.</summary>
        public const string ForagedKinds = "grindstone_foraged";
        /// <summary>Charter sync key of the Forage YAML files (GrindstoneSkills.Forage*.yml).</summary>
        public const string ForageSync = "grindstone_forage";
    }
}
