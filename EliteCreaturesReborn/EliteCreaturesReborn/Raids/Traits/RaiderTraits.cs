using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;
using UnityEngine;

namespace EliteCreaturesReborn.Raids
{
    /// <summary>
    /// A raider's stars and mutations (features/raids.md section 4.4): it rolls as any creature of the base's biome does
    /// - the difficulty, the world tier, its own `creatures:` entry, what its body rules out - then takes the heat's extra
    /// stars (two more again for the Warlord), never past the most a creature of the biome can roll there now
    /// (<see cref="StarCap"/>), and, at the heat's mutated share, a mutation when it rolled none, picked by
    /// the biome's own leanings and never Gilded. <see cref="RaiderSpawn"/> calls it on the machine that spawns the
    /// raider, in the same frame as the Instantiate, and the result is handed to the creature's controller as a forced
    /// roll, exactly as the Summoner's adds and `elite spawn` do, so it is written to the ZDO as the creature's one roll.
    /// With this mod's creature stars off, the extra stars are added to the game's level instead, which the creature then
    /// keeps.
    /// </summary>
    public static class RaiderTraits
    {
        /// <summary>Rolls the raider and forces the roll on it; null for a creature the mod does not roll (a boss, or one
        /// without the mod's controller), which is left as it is.</summary>
        public static CreatureTraits? Roll(Character creature, Heightmap.Biome biome, RaidBand band, bool warlord) =>
            Roll(creature, biome, band.ExtraStars + (warlord ? RaidTable.WarlordExtraStars : 0), band.MutatedPercent, 0, true);

        /// <summary>
        /// The same roll with the extra stars and the mutated share given outright, and the mutations in
        /// <paramref name="barredMask"/> struck from the base roll: Hel's Trial rolls its creatures through here
        /// (Arena/Trial/ArenaRoll), with its wave's stars and mutation chance and no Gilded, its stars rising past the
        /// difficulty's cap up to <see cref="RaidTable.MaxRaiderStars"/>. A raid bars nothing and keeps to the cap.
        /// </summary>
        public static CreatureTraits? Roll(Character creature, Heightmap.Biome biome, int extraStars, float mutatedPercent,
            int barredMask, bool toDifficultyCap = false)
        {
            EliteController? elite = creature != null ? creature.GetComponent<EliteController>() : null;
            if (elite == null || creature!.IsBoss())
            {
                return null;
            }
            CreatureTraits traits = BaseRoll(creature, biome);
            traits.Mask &= ~barredMask;
            AddStars(creature, traits, extraStars, toDifficultyCap ? DifficultyCap(creature, biome, traits.Tier) : RaidTable.MaxRaiderStars);
            if (traits.Mask == 0 && mutatedPercent > 0f && Util.Dice.Percent(mutatedPercent))
            {
                traits.Mask = RaiderMutation.Pick(creature, biome, traits.Stars);
            }
            elite.ForceTraits(traits, biome);
            return traits;
        }

        // The roll a wild creature of this kind gets in this biome now: the controller's own roll, with whatever another
        // mod registered for the prefab (a custom creature's fixed mutations) applied.
        private static CreatureTraits BaseRoll(Character creature, Heightmap.Biome biome) =>
            CreatureRoll.For(creature, biome);

        // The most stars a wild creature of this kind rolls in the biome at the tier it was just rolled at.
        private static int DifficultyCap(Character creature, Heightmap.Biome biome, int tier)
        {
            RuleSet set = RuleState.Active;
            return StarCap.For(set, set.For(biome, Utils.GetPrefabName(creature.gameObject)), tier, biome);
        }

        // The mod's stars when they are on, up to the cap; otherwise the game's level, which a creature with no stars of
        // the mod keeps.
        private static void AddStars(Character creature, CreatureTraits traits, int extra, int cap)
        {
            if (extra <= 0)
            {
                return;
            }
            if (RuleState.Active.CreatureStars)
            {
                traits.Stars = Mathf.Max(traits.Stars, Mathf.Min(traits.Stars + extra, cap));
                return;
            }
            creature.SetLevel(Mathf.Min(Mathf.Max(1, creature.GetLevel()) + extra, RaidTable.MaxRaiderStars + 1));
        }
    }
}
