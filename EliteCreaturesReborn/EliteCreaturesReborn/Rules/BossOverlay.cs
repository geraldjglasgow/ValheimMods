using System.Collections.Generic;
using YamlDotNet.RepresentationModel;

namespace EliteCreaturesReborn.Rules
{
    /// <summary>
    /// Reads the file's top-level <c>bosses</c> block onto the boss defaults: the stars switch, the star distribution,
    /// the boss star-power lines and the <c>aspects</c> block. Anything the block leaves out keeps its default, the same way a biome block does.
    /// </summary>
    internal static class BossOverlay
    {
        public static void Apply(BossRules boss, YamlMappingNode block, List<string> errors)
        {
            boss.Enabled = YamlRead.Bool(block, "stars", boss.Enabled, errors);
            float[]? chances = YamlRead.Floats(YamlRead.Child(block, "star chances"), errors, "boss star chances");
            if (chances != null && chances.Length > 0)
            {
                boss.StarChances = chances;
            }
            ApplyPower(boss.Star, block, errors);
            if (YamlRead.Child(block, Fields.Aspects) is YamlNode aspects
                && YamlRead.Map(aspects, errors, "'aspects'") is YamlMappingNode map)
            {
                AspectOverlay.Apply(boss.Aspects, map, errors);
            }
        }

        private static void ApplyPower(StarPower star, YamlMappingNode block, List<string> errors)
        {
            if (!(YamlRead.Child(block, "star power") is YamlMappingNode map))
            {
                return;
            }
            star.Growth = Line(map, Fields.Growth, star.Growth, errors);
            star.Hp = Line(map, Fields.Hp, star.Hp, errors);
            star.Attack = Line(map, Fields.Attack, star.Attack, errors);
            star.SwingSpeed = Line(map, Fields.SwingSpeed, star.SwingSpeed, errors);
            star.Speed = Line(map, Fields.Speed, star.Speed, errors);
            star.Drops = Line(map, Fields.Drops, star.Drops, errors);
        }

        private static float[] Line(YamlMappingNode map, string key, float[] current, List<string> errors)
        {
            float[]? values = YamlRead.Floats(YamlRead.Child(map, key), errors, $"bosses star power {key}");
            return values != null && values.Length > 0 ? values : current;
        }
    }
}
