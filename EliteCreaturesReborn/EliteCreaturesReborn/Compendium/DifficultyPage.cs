using System.Collections.Generic;
using System.Text;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Runtime;
using Translations;
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
        public static string Topic => Lang.Get("ecr_book_difficulty_topic");

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
            PageText.Heading(text, Lang.Format("ecr_book_difficulty", DifficultyNames.Shown(set.Difficulty)));
            text.Append(set.Tiers.Enabled
                ? Lang.Format("ecr_book_tier", tier, WorldTier.Ceiling())
                : Lang.Get("ecr_book_tier_off")).Append('\n');
            if (!set.CreatureStars)
            {
                text.Append(Lang.Get("ecr_book_creature_stars_off")).Append('\n');
            }
        }

        private static void Bosses(StringBuilder text, TierRules tiers)
        {
            if (!tiers.Enabled || tiers.BossKeys.Count == 0)
            {
                return;
            }
            PageText.Heading(text, Lang.Get("ecr_book_bosses"));
            Dictionary<string, string> known = WorldTier.KnownBosses();
            foreach (string key in tiers.BossKeys)
            {
                string state = WorldTier.IsDefeated(key) ? "<color=#7CC36E>" + Lang.Get("ecr_book_boss_defeated") + "</color>"
                    : PageText.Grey(Lang.Get("ecr_book_boss_not_yet"));
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
            PageText.Heading(text, Lang.Get("ecr_book_meaning"));
            if (set.Difficulty == Difficulty.Custom)
            {
                text.Append(Lang.Format("ecr_book_meaning_custom", DifficultyNames.Shown(Difficulty.Custom),
                    set.Tiers.StarBoostAt(tier).ToString("0.##"), set.Tiers.MutationBoostAt(tier).ToString("0.##")))
                    .Append('\n');
                return;
            }
            text.Append(Lang.Get("ecr_book_meaning_preset")).Append('\n');
        }

        private static void BiomeTable(StringBuilder text, RuleSet set, int tier, Heightmap.Biome here)
        {
            PageText.Heading(text, Lang.Format("ecr_book_biomes", tier));
            bool stars = set.CreatureStars;
            if (stars)
            {
                PageText.Row(text, TableEdges, Lang.Get("ecr_book_col_biome"), Lang.Get("ecr_book_col_most_stars"),
                    Lang.Get("ecr_book_col_with_stars"), Lang.Get("ecr_book_col_mutated"));
            }
            else
            {
                PageText.Row(text, MutationEdges, Lang.Get("ecr_book_col_biome"), Lang.Get("ecr_book_col_mutated"));
            }
            foreach (Heightmap.Biome biome in PageText.Biomes)
            {
                BiomeRow(text, BiomeOdds.For(set, tier, biome), biome, biome == here, stars);
            }
            text.Append(PageText.Grey(Lang.Get("ecr_book_biomes_note"))).Append('\n');
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
            PageText.Heading(text, Lang.Get("ecr_book_game"));
            text.Append(Lang.Format("ecr_book_game_rates", (Game.m_enemyDamageRate * 100f).ToString("0"),
                (Game.m_playerDamageRate * 100f).ToString("0"))).Append(' ');
            text.Append(Lang.Get(creatureStars ? "ecr_book_game_stars_ours" : "ecr_book_game_stars_game")).Append('\n');
        }
    }
}
