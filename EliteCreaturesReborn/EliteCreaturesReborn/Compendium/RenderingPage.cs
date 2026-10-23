using System.Collections.Generic;
using System.Text;
using EliteCreaturesReborn.Rendering;
using Translations;
using RenderMode = EliteCreaturesReborn.Rendering.RenderMode;
using UnityEngine;

namespace EliteCreaturesReborn.Compendium
{
    /// <summary>
    /// The "Rendering Table" page (a player, 2026-10-22: "ingredients" said too little about what a trophy gives): how a
    /// trophy renders, then for each biome its trophies, ores, ingredients and idols by the names this game gives them,
    /// read from <see cref="RenderTiers"/>. An item this game does not have is left out, as the table leaves it out of
    /// its rolls; a biome with no boss has no idol line. Written only while the table is switched on.
    /// </summary>
    internal static class RenderingPage
    {
        public static string Topic => Lang.Get("ecr_book_render_topic");

        public static string Text()
        {
            StringBuilder text = new StringBuilder(4096);
            PageText.Heading(text, Lang.Get("ecr_book_render_how"));
            text.Append(Lang.Format("ecr_book_render_rolls", RenderTiers.LeastPerRoll, RenderTiers.MostPerRoll,
                RenderTiers.BossRolls)).Append('\n');
            text.Append(Lang.Get("ecr_book_render_idols")).Append('\n');
            for (int tier = 0; tier < RenderTiers.TierCount; tier++)
            {
                Tier(text, tier);
            }
            return text.ToString();
        }

        private static void Tier(StringBuilder text, int tier)
        {
            PageText.Heading(text, PageText.BiomeName(PageText.Biomes[tier]));
            Line(text, "ecr_book_render_trophies", RenderTiers.TrophiesOf(tier));
            Line(text, "ecr_book_render_ores", RenderTiers.Outputs(RenderMode.Ores, tier));
            Line(text, "ecr_book_render_ingredients", RenderTiers.Outputs(RenderMode.Ingredients, tier));
            if (RenderTiers.HasBoss(tier))
            {
                Line(text, "ecr_book_render_idol", RenderTiers.IdolNames(tier));
            }
        }

        // "Ores: Copper ore, Tin ore"; nothing when this game has none of the items.
        private static void Line(StringBuilder text, string key, string[] prefabs)
        {
            List<string> names = Names(prefabs);
            if (names.Count > 0)
            {
                text.Append(Lang.Format(key, string.Join(Lang.Get("ecr_book_list_comma"), names))).Append('\n');
            }
        }

        private static List<string> Names(string[] prefabs)
        {
            List<string> names = new List<string>(prefabs.Length);
            foreach (string prefab in prefabs)
            {
                GameObject? item = RenderOutput.Prefab(prefab);
                if (item != null)
                {
                    names.Add(Localization.instance.Localize(item.GetComponent<ItemDrop>().m_itemData.m_shared.m_name));
                }
            }
            return names;
        }
    }
}
