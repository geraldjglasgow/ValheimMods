using EliteCreaturesReborn.Raids;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// An arena creature's stars and mutations (features/arena.md section 3, "Stars and mutations"), on the machine that
    /// makes it, in the same frame as its Instantiate, before its controller's first roll. A creature rolls as ECR rolls
    /// any creature of the wave's biome (<see cref="CreatureRoll.For"/>, through the raids' roll,
    /// <see cref="RaiderTraits"/>), then takes the wave's extra stars and, at the wave's mutation chance, a mutation when
    /// it rolled none - never Gilded, whose worth is a purse an arena creature does not drop and who runs from every
    /// player. A boss rolls its stars and aspects as any boss does, with the extra stars past the Deep North's boss. The roll is forced
    /// on the creature's controller, so it is written as its one roll; up to ECR's maximum of stars.
    /// </summary>
    internal static class ArenaRoll
    {
        private const int NoGilded = 1 << (int)Mutation.Gilded;

        public static void Roll(Character creature, int start, int wave)
        {
            Heightmap.Biome biome = ArenaCreatures.Biome(ArenaTable.BiomeIndex(start, wave));
            if (creature.IsBoss())
            {
                RollBoss(creature, biome, ArenaTable.PastLastBiome(start, wave));
                return;
            }
            int stars = ArenaTable.ExtraStars(start, wave);
            RaiderTraits.Roll(creature, biome, stars, ArenaTable.MutationChance(wave), NoGilded);
        }

        // The boss's own draw - its stars and aspects - with the extra stars added; left to roll itself when the mod does
        // not touch bosses (boss stars and aspects both off), as it would anywhere.
        private static void RollBoss(Character boss, Heightmap.Biome biome, int extra)
        {
            EliteController? elite = boss.GetComponent<EliteController>();
            RuleSet rules = RuleState.Active;
            if (elite == null || (!rules.Boss.Enabled && !rules.Boss.Aspects.Enabled))
            {
                return;
            }
            CreatureTraits traits = CreatureRoll.For(boss, biome);
            if (extra > 0 && rules.Boss.Enabled)
            {
                traits.Stars = Mathf.Min(traits.Stars + extra, ArenaTable.MaxStars);
            }
            elite.ForceTraits(traits, biome);
        }
    }
}
