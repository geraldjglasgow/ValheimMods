using System.Text;
using EliteCreaturesReborn.Rules;
using UnityEngine;

namespace EliteCreaturesReborn.Compendium
{
    /// <summary>
    /// The "Stars" page: what each star count does to a creature, read from the server's `star power` lines - the biome
    /// the player stands in first, then any biome whose lines differ from it, then the bosses' own table. Rows run to the
    /// most stars this server's difficulty can give anywhere, so a row the player meets later is already there.
    /// </summary>
    internal static class StarsPage
    {
        public const string Topic = "Elite: Stars";

        private static readonly int[] Edges = { 0, 14, 29, 44, 58, 72, 86 };
        private static readonly int[] BossEdges = { 0, 16, 34, 52, 70 };

        public static string Text(RuleSet set, Heightmap.Biome here)
        {
            StringBuilder text = new StringBuilder(2048);
            if (set.CreatureStars)
            {
                CreatureTables(text, set, here);
            }
            else
            {
                PageText.Heading(text, "Creature stars are off");
                text.Append("Creatures keep the game's own stars on this server, with the game's own numbers for them.\n");
            }
            BossTable(text, set.Boss);
            return text.ToString();
        }

        private static void CreatureTables(StringBuilder text, RuleSet set, Heightmap.Biome here)
        {
            int most = BiomeOdds.MostStars(set);
            StarPower mine = set.For(here).Star;
            Table(text, $"Stars in the {PageText.BiomeName(here)} (where you are)", mine, most);
            foreach (Heightmap.Biome biome in PageText.Biomes)
            {
                StarPower other = set.For(biome).Star;
                if (biome != here && !Same(mine, other, most))
                {
                    Table(text, $"Stars in the {PageText.BiomeName(biome)}", other, most);
                }
            }
            text.Append(PageText.Grey("Each number multiplies the game's own creature. Speed is how fast it moves, attack "
                + "how fast it swings. A large star on a nameplate counts as five.")).Append('\n');
        }

        private static void Table(StringBuilder text, string title, StarPower star, int most)
        {
            PageText.Heading(text, title);
            PageText.Row(text, Edges, "Stars", "Health", "Damage", "Attack", "Speed", "Size", "Loot");
            for (int stars = 0; stars <= most; stars++)
            {
                PageText.Row(text, Edges, StarsWord(stars), PageText.Times(star.HpAt(stars)),
                    PageText.Times(star.AttackAt(stars)), PageText.Times(star.SwingSpeedAt(stars)),
                    PageText.Times(star.SpeedAt(stars)), PageText.Times(1f + star.GrowthAt(stars)),
                    PageText.Times(star.DropsAt(stars)));
            }
        }

        private static bool Same(StarPower a, StarPower b, int most)
        {
            for (int stars = 0; stars <= most; stars++)
            {
                if (!Mathf.Approximately(a.HpAt(stars), b.HpAt(stars))
                    || !Mathf.Approximately(a.AttackAt(stars), b.AttackAt(stars))
                    || !Mathf.Approximately(a.SwingSpeedAt(stars), b.SwingSpeedAt(stars))
                    || !Mathf.Approximately(a.SpeedAt(stars), b.SpeedAt(stars))
                    || !Mathf.Approximately(a.GrowthAt(stars), b.GrowthAt(stars))
                    || !Mathf.Approximately(a.DropsAt(stars), b.DropsAt(stars)))
                {
                    return false;
                }
            }
            return true;
        }

        // Bosses roll their own stars from one table for the whole world, and their stars never multiply their drops.
        private static void BossTable(StringBuilder text, BossRules boss)
        {
            PageText.Heading(text, "Boss stars");
            if (!boss.Enabled)
            {
                text.Append("Off on this server: bosses keep the game's own level.\n");
                return;
            }
            PageText.Row(text, BossEdges, "Stars", "Chance", "Health", "Damage", "Size");
            float total = Total(boss.StarChances);
            for (int stars = 0; stars <= boss.StarCeiling; stars++)
            {
                float chance = total > 0f ? Mathf.Max(0f, boss.StarChances[stars]) * 100f / total : 0f;
                PageText.Row(text, BossEdges, StarsWord(stars), PageText.Percent(chance),
                    PageText.Times(boss.Star.HpAt(stars)), PageText.Times(boss.Star.AttackAt(stars)),
                    PageText.Times(1f + boss.Star.GrowthAt(stars)));
            }
            string again = boss.HarderEachKill ? " Each time a boss is killed, its next fight leans toward more stars." : "";
            text.Append(PageText.Grey("The altar shows a boss's stars before you summon it. A boss's stars never multiply "
                + "its drops." + again)).Append('\n');
        }

        private static float Total(float[] chances)
        {
            float total = 0f;
            foreach (float chance in chances)
            {
                total += Mathf.Max(0f, chance);
            }
            return total;
        }

        private static string StarsWord(int stars) => stars == 0 ? "none" : stars.ToString() + PageText.Star;
    }
}
