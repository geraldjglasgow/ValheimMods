using System.Collections.Generic;
using UnityEngine;

namespace EliteCreaturesReborn.Rendering
{
    /// <summary>
    /// Every fixed number of the Rendering Table (the user, 2026-10-20: "take trophies and convert them to items, random
    /// items, items in the tier the trophy is from; you can choose totems, ores, ingredients"). A trophy's tier is its
    /// creature's biome, 0 Meadows ... 7 the Deep North (homes read from the game's spawn lists, 2026-10-20: the Writhan
    /// lives in the Swamp). One trophy renders into 2-4 of one ore or ingredient of its tier, drawn at random, a boss
    /// trophy into five such rolls. Only a boss trophy makes a Forge of Potential idol of its tier (the game's
    /// <c>Upgrader&lt;tier&gt;Weapon</c> or <c>Armor</c>, Battle or Protection at random, as Hel's Arena pays them).
    /// Trophies not listed (Hildir's quest trophies, the Bog Witch's Kvastur, another mod's) are never taken.
    /// </summary>
    internal static class RenderTiers
    {
        /// <summary>The ore or ingredient rolls one boss trophy gives.</summary>
        public const int BossRolls = 5;

        /// <summary>One ore or ingredient roll: at least this many, at most <see cref="MostPerRoll"/>.</summary>
        public const int LeastPerRoll = 2;
        public const int MostPerRoll = 4;

        private static readonly string[][] TrophiesByTier =
        {
            new[] { "TrophyBoar", "TrophyNeck", "TrophyDeer" },
            new[]
            {
                "TrophyGreydwarf", "TrophyGreydwarfBrute", "TrophyGreydwarfShaman", "TrophyFrostTroll", "TrophyForestTroll",
                "TrophySkeleton", "TrophySkeletonPoison", "TrophyGhost", "TrophyBjorn",
            },
            new[]
            {
                "TrophySurtling", "TrophyBlob", "TrophyLeech", "TrophyAbomination", "TrophyWraith", "TrophyDraugr",
                "TrophyDraugrElite", "TrophyDraugrFem", "TrophySerpent", "TrophyWrithan",
            },
            new[] { "TrophyWolf", "TrophyHatchling", "TrophyUlv", "TrophyFenring", "TrophyCultist", "TrophySGolem" },
            new[]
            {
                "TrophyLox", "TrophyGoblin", "TrophyGoblinBrute", "TrophyGoblinShaman", "TrophyDeathsquito", "TrophyGrowth",
                "TrophyBjornUndead",
            },
            new[] { "TrophyHare", "TrophySeeker", "TrophySeekerBrute", "TrophyGjall", "TrophyTick", "TrophyDvergr" },
            new[]
            {
                "TrophyAsksvin", "TrophyVolture", "TrophyMorgen", "TrophyCharredArcher", "TrophyCharredMage",
                "TrophyCharredMelee", "TrophyFallenValkyrie", "TrophyBlob_Lava", "TrophyBonemawSerpent",
            },
            new[]
            {
                "TrophyBarka", "TrophyElaking", "TrophyMole", "TrophyMoose", "TrophySeal", "TrophyBlob_Frost",
                "TrophyBlob_Morkhalla", "TrophyJotunWarrior", "TrophyJotunWitch",
            },
        };

        private static readonly string[] BossTrophies =
        {
            "TrophyEikthyr", "TrophyTheElder", "TrophyBonemass", "TrophyDragonQueen", "TrophyGoblinKing", "TrophySeekerQueen",
            "TrophyFader",
        };

        private static readonly string[][] Ores =
        {
            new[] { "Flint", "Stone" },
            new[] { "CopperOre", "TinOre" },
            new[] { "IronScrap" },
            new[] { "SilverOre", "Obsidian" },
            new[] { "BlackMetalScrap" },
            new[] { "BlackMarble", "Softtissue" },
            new[] { "FlametalOreNew", "Grausten" },
            new[] { "GoldOre" },
        };

        private static readonly string[][] Ingredients =
        {
            new[]
            {
                "Wood", "Resin", "LeatherScraps", "DeerHide", "Feathers", "Raspberry", "Mushroom", "Dandelion", "Honey",
                "NeckTail", "RawMeat", "DeerMeat",
            },
            new[]
            {
                "RoundLog", "FineWood", "GreydwarfEye", "TrollHide", "BoneFragments", "Thistle", "Blueberries",
                "MushroomYellow", "Carrot", "SurtlingCore",
            },
            new[]
            {
                "ElderBark", "Guck", "Bloodbag", "Ooze", "Root", "Entrails", "Chain", "Turnip", "Chitin", "SerpentScale",
                "SerpentMeat", "WrithanRoots",
            },
            new[]
            {
                "WolfPelt", "WolfFang", "WolfClaw", "WolfHairBundle", "WolfMeat", "FreezeGland", "Crystal", "Onion", "JuteRed",
            },
            new[] { "Flax", "Barley", "Needle", "LoxPelt", "LoxMeat", "Tar", "Cloudberry" },
            new[]
            {
                "Sap", "Carapace", "Mandible", "ScaleHide", "Bilebag", "RoyalJelly", "GiantBloodSack", "YggdrasilWood",
                "JuteBlue", "MushroomJotunPuffs", "MushroomMagecap", "BugMeat", "HareMeat", "Wisp",
            },
            new[]
            {
                "Blackwood", "AskHide", "AskBladder", "CharredBone", "CharcoalResin", "MorgenSinew", "CelestialFeather",
                "ProustitePowder", "SulfurStone", "Vineberry", "Fiddleheadfern", "MushroomSmokePuff", "BonemawSerpentTooth",
            },
            new[]
            {
                "MooseHide", "MooseMeat", "MooseSinew", "SealHide", "SealBlubber", "BarkaBranch", "ElakingHairBundle",
                "MoleClaws", "OozeMork", "Leatherstraps", "NornThread",
            },
        };

        private static readonly Dictionary<string, int> Tiers = Index(TrophiesByTier);
        private static readonly Dictionary<string, int> Bosses = Index(Single(BossTrophies));

        /// <summary>The trophy's tier (0 Meadows ... 7 Deep North) and whether it is a boss's; false for any item the
        /// table does not take.</summary>
        public static bool TryTier(ItemDrop.ItemData? item, out int tier, out bool boss)
        {
            string? prefab = item?.m_dropPrefab != null ? item.m_dropPrefab.name : null;
            tier = 0;
            boss = prefab != null && Bosses.TryGetValue(prefab, out tier);
            return boss || (prefab != null && Tiers.TryGetValue(prefab, out tier));
        }

        /// <summary>The item names a roll at this tier draws from, in the given mode (not for idols).</summary>
        public static string[] Outputs(RenderMode mode, int tier)
        {
            string[][] table = mode == RenderMode.Ores ? Ores : Ingredients;
            return table[Mathf.Clamp(tier, 0, table.Length - 1)];
        }

        /// <summary>A Forge of Potential idol of this tier: Battle or Protection, at random.</summary>
        public static string Idol(int tier) => IdolNames(tier)[Random.value < 0.5f ? 0 : 1];

        /// <summary>Both Forge of Potential idols of this tier, Battle first.</summary>
        public static string[] IdolNames(int tier)
        {
            string name = "Upgrader" + Mathf.Clamp(tier, 0, TrophiesByTier.Length - 1);
            return new[] { name + "Weapon", name + "Armor" };
        }

        /// <summary>How many tiers there are, 0 Meadows to the Deep North.</summary>
        public static int TierCount => TrophiesByTier.Length;

        /// <summary>The creature trophies of a tier, its boss's (if it has one) last.</summary>
        public static string[] TrophiesOf(int tier)
        {
            string[] trophies = TrophiesByTier[tier];
            return HasBoss(tier) ? Append(trophies, BossTrophies[tier]) : trophies;
        }

        /// <summary>Whether the tier has a boss trophy, so an idol can be made of it (the Deep North has none yet).</summary>
        public static bool HasBoss(int tier) => tier >= 0 && tier < BossTrophies.Length;

        private static string[] Append(string[] names, string last)
        {
            string[] all = new string[names.Length + 1];
            names.CopyTo(all, 0);
            all[names.Length] = last;
            return all;
        }

        // Bosses: one tier each, in the order of their biomes.
        private static string[][] Single(string[] names)
        {
            string[][] tiers = new string[names.Length][];
            for (int i = 0; i < names.Length; i++)
            {
                tiers[i] = new[] { names[i] };
            }
            return tiers;
        }

        private static Dictionary<string, int> Index(string[][] tiers)
        {
            Dictionary<string, int> index = new Dictionary<string, int>();
            for (int tier = 0; tier < tiers.Length; tier++)
            {
                foreach (string name in tiers[tier])
                {
                    index[name] = tier;
                }
            }
            return index;
        }
    }
}
