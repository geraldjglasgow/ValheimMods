using System;
using System.Collections.Generic;

namespace DevBridge.Studio
{
    /// <summary>A biome an item belongs to and its place in the game's order (Meadows 0 to the Deep North 7).</summary>
    internal readonly struct Tier
    {
        internal readonly int Rank;
        internal readonly string Biome;

        internal Tier(int rank, string biome)
        {
            Rank = rank;
            Biome = biome;
        }
    }

    /// <summary>
    /// What the studio's item tiers start from (<see cref="ItemTiers"/>): the game's raw materials by the biome they are
    /// first found in, which recipes never move, the bosses by the biome their altar stands in, and spawn biomes read
    /// as the earliest biome in a flag set. The ocean counts as a Swamp-tier biome of its own.
    /// </summary>
    internal static class TierSeeds
    {
        private static readonly (Heightmap.Biome Flag, Tier Tier)[] Biomes =
        {
            (Heightmap.Biome.Meadows, new Tier(0, "Meadows")),
            (Heightmap.Biome.BlackForest, new Tier(1, "Black Forest")),
            (Heightmap.Biome.Swamp, new Tier(2, "Swamp")),
            (Heightmap.Biome.Ocean, new Tier(2, "Ocean")),
            (Heightmap.Biome.Mountain, new Tier(3, "Mountain")),
            (Heightmap.Biome.Plains, new Tier(4, "Plains")),
            (Heightmap.Biome.Mistlands, new Tier(5, "Mistlands")),
            (Heightmap.Biome.AshLands, new Tier(6, "Ashlands")),
            (Heightmap.Biome.DeepNorth, new Tier(7, "Deep North")),
        };

        internal static readonly Dictionary<string, Tier> Materials = Table(new Dictionary<Heightmap.Biome, string>
        {
            [Heightmap.Biome.Meadows] = "Wood Stone Flint Resin LeatherScraps DeerHide Feathers Raspberry Mushroom Dandelion HardAntler " +
                "Honey QueenBee NeckTail RawMeat DeerMeat BeechSeeds",
            [Heightmap.Biome.BlackForest] = "RoundLog FineWood CopperOre TinOre GreydwarfEye SurtlingCore TrollHide AncientSeed " +
                "BoneFragments Thistle Blueberries MushroomYellow CarrotSeeds Carrot CryptKey PineCone FirCone",
            [Heightmap.Biome.Swamp] = "IronScrap ElderBark Guck Bloodbag Ooze WitheredBone Root Entrails Chain TurnipSeeds Turnip Wishbone",
            [Heightmap.Biome.Ocean] = "Chitin SerpentScale SerpentMeat",
            [Heightmap.Biome.Mountain] = "SilverOre WolfPelt WolfFang WolfClaw WolfHairBundle WolfMeat Obsidian FreezeGland DragonTear " +
                "Crystal DragonEgg OnionSeeds Onion JuteRed",
            [Heightmap.Biome.Plains] = "BlackMetalScrap Flax Barley Needle LoxPelt LoxMeat Tar GoblinTotem YagluthDrop Cloudberry",
            [Heightmap.Biome.Mistlands] = "Sap Softtissue BlackMarble BlackCore Carapace Mandible ScaleHide Bilebag RoyalJelly " +
                "GiantBloodSack YggdrasilWood JuteBlue Thunderstone Wisp DvergrNeedle QueenDrop MushroomJotunPuffs MushroomMagecap BugMeat HareMeat",
            [Heightmap.Biome.AshLands] = "FlametalOreNew FlametalOre Blackwood AskHide AskBladder CharredBone CharcoalResin Grausten " +
                "MoltenCore CelestialFeather MorgenSinew MorgenHeart ProustitePowder SulfurStone GemstoneRed GemstoneGreen GemstoneBlue " +
                "BonemawSerpentTooth Bell Vineberry Fiddleheadfern MushroomSmokePuff",
        });

        internal static readonly Dictionary<string, Heightmap.Biome> Bosses = new Dictionary<string, Heightmap.Biome>
        {
            ["Eikthyr"] = Heightmap.Biome.Meadows,
            ["gd_king"] = Heightmap.Biome.BlackForest,
            ["Bonemass"] = Heightmap.Biome.Swamp,
            ["Dragon"] = Heightmap.Biome.Mountain,
            ["GoblinKing"] = Heightmap.Biome.Plains,
            ["SeekerQueen"] = Heightmap.Biome.Mistlands,
            ["Fader"] = Heightmap.Biome.AshLands,
        };

        /// <summary>The earliest biome in the flags, or null for none the studio knows.</summary>
        internal static Tier? Of(Heightmap.Biome flags)
        {
            foreach ((Heightmap.Biome flag, Tier tier) in Biomes)
                if ((flags & flag) != 0) return tier;
            return null;
        }

        private static Dictionary<string, Tier> Table(Dictionary<Heightmap.Biome, string> lines)
        {
            var table = new Dictionary<string, Tier>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<Heightmap.Biome, string> line in lines)
            foreach (string name in line.Value.Split(' '))
                table[name] = Of(line.Key).Value;
            return table;
        }
    }
}
