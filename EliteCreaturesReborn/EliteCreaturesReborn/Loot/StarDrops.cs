using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Runtime;
using UnityEngine;

namespace EliteCreaturesReborn.Loot
{
    /// <summary>
    /// What a kill's stars add to its loot: the star `drops` line (Scaled) and the extra rolls (Rolled, Curated). A
    /// creature that keeps its game level (this mod's stars off for its kind, <see cref="RuleSet.KeepsLevel"/>) has none
    /// of this mod's stars to pay for - the game already scaled its pile by that level - so it takes neither.
    /// </summary>
    internal static class StarDrops
    {
        /// <summary>The quantity multiplier: the creature's own `drops` line, else its biome's or the boss table's.</summary>
        public static float Multiplier(CreatureLootRule? rule, EliteController controller, bool isBoss, int stars)
        {
            if (RuleState.Active.KeepsLevel(stars, isBoss))
            {
                return 1f;
            }
            return rule?.Drops != null ? LineAt(rule.Drops, stars) : LiveDropsLine(controller, isBoss, stars);
        }

        /// <summary>How many times the table is rolled again.</summary>
        public static int ExtraRolls(LootRules loot, bool isBoss, int stars) =>
            RuleState.Active.KeepsLevel(stars, isBoss) ? 0 : DropRoller.ExtraRolls(loot, stars);

        /// <summary>
        /// The star `drops` line from the rules active right now, not the snapshot the creature resolved with. Loot
        /// is applied at death, so a rule-file edit re-tunes what an already-spawned creature pays - the same hot
        /// reload every other loot setting gets by reading RuleState at death. Falls back to the resolve-time
        /// snapshot only when the ZDO is already gone.
        /// </summary>
        private static float LiveDropsLine(EliteController controller, bool isBoss, int stars)
        {
            if (isBoss)
            {
                return RuleState.Active.Boss.Star.DropsAt(stars);
            }
            ZDO? zdo = controller.View != null && controller.View.IsValid() ? controller.View.GetZDO() : null;
            if (zdo == null)
            {
                return controller.Rules.Star.DropsAt(stars);
            }
            return RuleState.Active.For(Traits.TraitStore.GetBiome(zdo)).Star.DropsAt(stars);
        }

        private static float LineAt(float[] line, int stars)
        {
            if (line.Length == 0)
            {
                return 1f;
            }
            return line[Mathf.Clamp(stars, 0, line.Length - 1)];
        }
    }
}
