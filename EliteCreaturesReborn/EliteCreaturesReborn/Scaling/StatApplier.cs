using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Traits;
using UnityEngine;

namespace EliteCreaturesReborn.Scaling
{
    /// <summary>
    /// Applies the one-shot scaling that is not a live multiplier: the creature's visual size and its maximum health.
    /// Both replace vanilla's level scaling rather than stacking on it - the creature is kept at vanilla level 1, so the
    /// figures here are the whole story. Movement and swing speed are live and handled by the controller and a patch.
    /// </summary>
    public static class StatApplier
    {
        public static void ApplySize(Character character, BiomeRules rules, CreatureTraits traits)
        {
            float size = StatMath.SizeMultiplier(rules, traits);
            if (!Mathf.Approximately(size, 1f))
            {
                character.transform.localScale *= size;
            }
        }

        // OWNER ONLY: SetMaxHealth writes the ZDO's s_maxHealth, which the game replicates - so every other machine reads
        // the same scaled maximum (and a correct health-bar fraction) without this ever running there. The refill on a
        // fresh roll is likewise an owner-only SetHealth. A non-owner call would be dropped by the game, so callers gate.
        public static void ApplyHealth(Character character, BiomeRules rules, CreatureTraits traits, bool freshlyResolved)
        {
            ZDO zdo = character.GetComponent<ZNetView>().GetZDO();
            float devoured = zdo != null ? TraitStore.GetDevouredHealth(zdo) : 0f;
            float max = character.GetMaxHealthBase() * StatMath.HealthMultiplier(rules, traits) + devoured;
            character.SetMaxHealth(max);
            if (freshlyResolved)
            {
                character.SetHealth(max);
            }
        }
    }
}
