using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Traits;
using UnityEngine;

namespace EliteCreaturesReborn.Scaling
{
    /// <summary>
    /// Applies the one-shot scaling that is not a live multiplier: the creature's visual size and its maximum health.
    /// Both replace vanilla's level scaling rather than stacking on it - the creature is kept at vanilla level 1, so the
    /// figures here are the whole story. A creature that keeps its game level (this mod's stars off for its kind) is
    /// the exception: its health starts from the game's figure for that level, and its size from the game's own look.
    /// A boss's aspect multiplies the starred health (Twin), or replaces it outright
    /// on a Phantom copy. Movement and swing speed are live and handled by the controller and a patch.
    /// </summary>
    public static class StatApplier
    {
        public static void ApplySize(Character character, BiomeRules rules, CreatureTraits traits)
        {
            float size = StatMath.SizeMultiplier(rules, traits) * AspectMath.SizeFactor(traits);
            if (!Mathf.Approximately(size, 1f))
            {
                character.transform.localScale *= size;
            }
        }

        // OWNER ONLY: SetMaxHealth writes the ZDO's s_maxHealth, which the game replicates - so every other machine reads
        // the same scaled maximum (and a correct health-bar fraction) without this ever running there. The refill on a
        // fresh roll is likewise owner-only. A non-owner call would be dropped by the game, so callers gate. A creature
        // that keeps its game level and gets nothing from this mod's health keeps the maximum the game (or another mod)
        // gave it.
        public static void ApplyHealth(Character character, BiomeRules rules, CreatureTraits traits, bool freshlyResolved)
        {
            ZDO zdo = character.GetComponent<ZNetView>().GetZDO();
            float devoured = zdo != null ? TraitStore.GetDevouredHealth(zdo) : 0f;
            float levelled = LevelledBase(character, traits);
            float max = traits.PhantomCopy
                ? AspectMath.PhantomHealth()
                : levelled * StatMath.HealthMultiplier(rules, traits) * AspectMath.HealthFactor(traits) + devoured;
            if (!RuleState.Active.KeepsLevel(traits.Stars, character.IsBoss()) || !Mathf.Approximately(max, levelled))
            {
                character.SetMaxHealth(max);
            }
            if (zdo != null && (freshlyResolved || PinnedAt(zdo, max)))
            {
                zdo.RemoveFloat(ZDOVars.s_health);
            }
        }

        // The game's own maximum for a creature with none of this mod's stars: its base times its level, as the game sets
        // it up. A starred creature is at level 1 and its stars replace the level, so its base alone.
        private static float LevelledBase(Character character, CreatureTraits traits) =>
            character.GetMaxHealthBase() * (traits.Stars == 0 ? Mathf.Max(1, character.GetLevel()) : 1);

        // Full the game's way: no stored health, so health follows the maximum. Up to 3.7.0 a fresh roll wrote the
        // number instead, which pinned it, and a mod that raised the maximum afterwards left the creature part-hurt. A
        // creature still holding exactly that number is released when it next loads.
        private static bool PinnedAt(ZDO zdo, float max) =>
            Mathf.Approximately(zdo.GetFloat(ZDOVars.s_health, -1f), max);
    }
}
