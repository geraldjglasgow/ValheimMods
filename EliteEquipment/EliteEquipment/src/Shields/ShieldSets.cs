using System.Collections.Generic;
using EliteEquipment.Core;

namespace EliteEquipment.Shields
{
    /// <summary>
    /// The shields that give every biome one of each kind. Bucklers (the game has the Bronze, Iron, Carapace and Nord
    /// Bucklers for the Black Forest, Swamp, Mistlands and Deep North) follow the game's own (read in game 2026-10-09):
    /// parry 2.5x, +6 block a level, -5% movement, durability as the biome's shield, a point lighter than it; block on the
    /// line through the Iron Buckler (28) and the Carapace Buckler (78), the Flametal's between the Carapace's and the
    /// Nord's (88); deflection 10 under the biome's shield; ten of the metal and four of the wood (+5 and +1 a level), plus
    /// the shield's third material. The Bronze Shield fills the Black Forest's round shield: as far over the Bronze
    /// Buckler (16) as the Banded Shield (42) is over the Iron Buckler (28), deflection between the Wood (20) and Banded
    /// (40), parry 1.5x, beside the Bronze Buckler at the forge. The Silver and Carapace Tower Shields fill the towers'
    /// gaps: block and deflection halfway between the game towers either side (Iron 52 / 100, Black Metal 104 / 150,
    /// Flametal 140 / 150), no parry, -10% movement, a tower's cost (15 wood and 10 metal, +10 and +5 a level). Gloss and
    /// metal gloss are the biome shield's material's (read in game 2026-10-10); the Silver and Black Metal Bucklers and
    /// both towers wear their game shield's own material (Look).
    /// </summary>
    public static class ShieldSets
    {
        private const string MetalBase = "ShieldIronBuckler";
        private const string Parries = " Blocks less than a shield, parries far better.";

        public static readonly IReadOnlyList<ShieldSet> All = new List<ShieldSet>
        {
            new ShieldSet
            {
                Kind = ShieldKind.Buckler, Prefab = "EE_BucklerWood", Asset = "ee_buckler_wood", Base = "ShieldWood", Shield = "ShieldWood",
                Name = "Wood Buckler", Description = "A small wooden buckler bound in rawhide." + Parries,
                Block = 4f, Deflection = 20f, DeflectionPerLevel = 0f, Weight = 3f, Durability = 200f, Gloss = 0.1f, MetalGloss = 0f,
                Cost = new[] { new RecipePart("Wood", 8, 4), new RecipePart("LeatherScraps", 4, 2), new RecipePart("Resin", 2, 1) },
            },
            new ShieldSet
            {
                Kind = ShieldKind.Round, Prefab = "EE_ShieldBronze", Asset = "ee_shield_bronze", Base = "ShieldBanded", Shield = "ShieldBronzeBuckler",
                Name = "Bronze Shield", Description = "A round shield of planks in a bronze rim, with a bronze boss. Sturdier than wood.",
                Block = 30f, Deflection = 30f, DeflectionPerLevel = 5f, Weight = 5f, Durability = 200f, Gloss = 0.2f, MetalGloss = 0.62f,
                Cost = new[] { new RecipePart("Wood", 10, 5), new RecipePart("Bronze", 8, 4) },
            },
            new ShieldSet
            {
                Kind = ShieldKind.Buckler, Prefab = "EE_BucklerSilver", Asset = "ee_buckler_silver", Base = MetalBase, Shield = "ShieldSilver", Look = "ShieldSilver",
                Name = "Silver Buckler", Description = "Fine wood in a band of silver, a silver wolf's head at its heart." + Parries,
                Block = 45f, Deflection = 30f, DeflectionPerLevel = 5f, Weight = 4f, Durability = 200f, Gloss = 0.29f, MetalGloss = 0.65f,
                Cost = new[] { new RecipePart("Silver", 10, 5), new RecipePart("FineWood", 4, 1) },
            },
            new ShieldSet
            {
                Kind = ShieldKind.Tower, Prefab = "EE_TowerSilver", Asset = "ee_tower_silver", Base = "ShieldIronTower", Shield = "ShieldSilver", Look = "ShieldSilver",
                Name = "Silver Tower Shield", Description = "A tall board of fine wood in a silver frame, a silver wolf's head at its brow. Slow, but it stops almost anything.",
                Block = 78f, Deflection = 125f, DeflectionPerLevel = 5f, Weight = 5f, Durability = 200f, Gloss = 0.29f, MetalGloss = 0.65f,
                Cost = new[] { new RecipePart("FineWood", 15, 10), new RecipePart("Silver", 10, 5) },
            },
            new ShieldSet
            {
                Kind = ShieldKind.Buckler, Prefab = "EE_BucklerBlackmetal", Asset = "ee_buckler_blackmetal", Base = MetalBase, Shield = "ShieldBlackmetal", Look = "ShieldBlackmetal",
                Name = "Black Metal Buckler", Description = "A black metal buckler hung on chain." + Parries,
                Block = 61f, Deflection = 40f, DeflectionPerLevel = 5f, Weight = 4f, Durability = 200f, Gloss = 0.19f, MetalGloss = 0.65f,
                Cost = new[] { new RecipePart("BlackMetal", 10, 5), new RecipePart("FineWood", 4, 1), new RecipePart("Chain", 2, 1) },
            },
            new ShieldSet
            {
                Kind = ShieldKind.Tower, Prefab = "EE_TowerCarapace", Asset = "ee_tower_carapace", Base = "ShieldBlackmetalTower", Shield = "ShieldCarapace", Look = "ShieldCarapace",
                Name = "Carapace Tower Shield", Description = "Overlapping carapace plates in a tall kite. Slow, but it stops almost anything.",
                Block = 122f, Deflection = 150f, DeflectionPerLevel = 5f, Weight = 5f, Durability = 250f, Gloss = 0.5f, MetalGloss = 0f,
                Cost = new[] { new RecipePart("Carapace", 25, 12), new RecipePart("ScaleHide", 5, 3), new RecipePart("Eitr", 10, 3) },
            },
            new ShieldSet
            {
                Kind = ShieldKind.Buckler, Prefab = "EE_BucklerFlametal", Asset = "ee_buckler_flametal", Base = MetalBase, Shield = "ShieldFlametal",
                Name = "Flametal Buckler", Description = "A flametal buckler over blackwood, still warm from the forge." + Parries,
                Block = 83f, Deflection = 40f, DeflectionPerLevel = 5f, Weight = 4f, Durability = 300f, Gloss = 0.26f, MetalGloss = 0.77f,
                Cost = new[] { new RecipePart("FlametalNew", 10, 5), new RecipePart("Blackwood", 4, 1), new RecipePart("AskHide", 2, 1) },
            },
        };
    }
}
