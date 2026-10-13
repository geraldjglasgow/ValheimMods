using System.Collections.Generic;
using System.Text;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Runtime;
using UnityEngine;

namespace EliteCreaturesReborn.Compendium
{
    /// <summary>
    /// The "World difficulty" page: the server's difficulty and world tier, the bosses that count toward it, what that
    /// gives each biome right now, what each difficulty means, and how the game's own World Difficulty stacks with it.
    /// Everything is read from the rules the server sent and the world's own keys, as `elite tier` reads them.
    /// </summary>
    internal static class DifficultyPage
    {
        public const string Topic = "Elite: World difficulty";

        private static readonly int[] TableEdges = { 0, 36, 56, 78 };
        private static readonly int[] MutationEdges = { 0, 50 };
        private static readonly int[] BossEdges = { 0, 50 };

        public static string Text(RuleSet set, int tier, Heightmap.Biome here)
        {
            StringBuilder text = new StringBuilder(2048);
            Summary(text, set, tier);
            Bosses(text, set.Tiers);
            Meaning(text, set, tier);
            BiomeTable(text, set, tier, here);
            DifficultyLadder.Append(text, set.Difficulty);
            GameDifficulty(text, set.CreatureStars);
            return text.ToString();
        }

        private static void Summary(StringBuilder text, RuleSet set, int tier)
        {
            PageText.Heading(text, "Difficulty: " + DifficultyNames.Name(set.Difficulty));
            text.Append(set.Tiers.Enabled
                ? $"World tier {tier} of {WorldTier.Ceiling()}: one tier for every boss below that has fallen. Each tier "
                    + "makes creatures stronger.\n"
                : "World tiers are off on this server: the world stays at tier 0.\n");
            if (!set.CreatureStars)
            {
                text.Append("Creature stars are off on this server: creatures keep the game's own stars, and only their "
                    + "mutations come from Elite Creatures.\n");
            }
        }

        private static void Bosses(StringBuilder text, TierRules tiers)
        {
            if (!tiers.Enabled || tiers.BossKeys.Count == 0)
            {
                return;
            }
            PageText.Heading(text, "Bosses");
            Dictionary<string, string> known = WorldTier.KnownBosses();
            foreach (string key in tiers.BossKeys)
            {
                string state = WorldTier.IsDefeated(key) ? "<color=#7CC36E>defeated</color>" : PageText.Grey("not yet");
                PageText.Row(text, BossEdges, BossName(key, known), state);
            }
        }

        // The boss's name in the player's language; the key itself when no boss the game knows sets it.
        private static string BossName(string key, Dictionary<string, string> known)
        {
            if (!known.TryGetValue(key, out string prefab) || ZNetScene.instance == null)
            {
                return key;
            }
            GameObject boss = ZNetScene.instance.GetPrefab(prefab);
            Character? character = boss != null ? boss.GetComponent<Character>() : null;
            return character != null ? Localization.instance.Localize(character.m_name) : prefab;
        }

        private static void Meaning(StringBuilder text, RuleSet set, int tier)
        {
            PageText.Heading(text, "What it means");
            if (set.Difficulty == Difficulty.Custom)
            {
                text.Append("Custom: this server sets each biome's star and mutation chances itself. Each world tier raises "
                    + $"them; now stars x{set.Tiers.StarBoostAt(tier):0.##} and mutations "
                    + $"x{set.Tiers.MutationBoostAt(tier):0.##}.\n");
                return;
            }
            text.Append("The difficulty decides how many stars and mutations creatures get as the world's bosses fall. The "
                + "biome you're entering is the gentlest. A biome you've cleared gets harder with every boss killed after "
                + "it, so by the end of the game the Meadows is the hardest place. Biomes you haven't reached match the one "
                + "you're entering.\nUnder the most stars allowed, most creatures are plain and each extra star is rarer "
                + "than the one before. Each star a creature has adds a quarter to its chance of a mutation.\n");
        }

        private static void BiomeTable(StringBuilder text, RuleSet set, int tier, Heightmap.Biome here)
        {
            PageText.Heading(text, $"Each biome at world tier {tier}");
            bool stars = set.CreatureStars;
            if (stars)
            {
                PageText.Row(text, TableEdges, "Biome", "Most stars", "With stars", "Mutated");
            }
            else
            {
                PageText.Row(text, MutationEdges, "Biome", "Mutated");
            }
            foreach (Heightmap.Biome biome in PageText.Biomes)
            {
                BiomeRow(text, BiomeOdds.For(set, tier, biome), biome, biome == here, stars);
            }
            text.Append(PageText.Grey("Your biome is in orange. Mutated: creatures with no stars that carry a mutation."))
                .Append('\n');
        }

        private static void BiomeRow(StringBuilder text, BiomeOdds odds, Heightmap.Biome biome, bool here, bool stars)
        {
            if (here)
            {
                text.Append("<color=orange>");
            }
            string name = PageText.BiomeName(biome);
            string mutated = PageText.Percent(odds.Mutated);
            if (stars)
            {
                PageText.Row(text, TableEdges, name, odds.Cap.ToString(), PageText.Percent(odds.Starred), mutated);
            }
            else
            {
                PageText.Row(text, MutationEdges, name, mutated);
            }
            if (here)
            {
                text.Insert(text.Length - 1, "</color>");
            }
        }

        private static void GameDifficulty(StringBuilder text, bool creatureStars)
        {
            PageText.Heading(text, "The game's own World Difficulty");
            text.Append("Separate from all this, and it stacks: on this world enemies deal "
                + $"{Game.m_enemyDamageRate * 100f:0}% of their normal damage and you deal "
                + $"{Game.m_playerDamageRate * 100f:0}% of yours. ");
            text.Append(creatureStars
                ? "Its extra star chance does nothing while Elite Creatures gives the stars.\n"
                : "Its extra star chance applies as usual, since the game gives the stars here.\n");
        }
    }
}
