using EliteCreaturesReborn.Traits;
using UnityEngine;

namespace EliteCreaturesReborn.Scaling
{
    /// <summary>
    /// Health another mod gave a creature before this mod rolled it, kept as a factor on this mod's own figure: Epic Loot's
    /// bounty targets (twice or three times their health, their minions half again), which this mod's health used to
    /// replace (Discord, 2026-11-13). Measured once, by the owner on the fresh roll, against the game's own maximum for
    /// the creature's level, before this mod's stars park that level; kept in the ZDO only when it is not 1, so it holds
    /// on every later load and owner (the game resets a full-health creature's maximum as it loads).
    /// </summary>
    internal static class OutsideHealth
    {
        private static readonly int Hash = TraitKeys.OutsideHealth.GetStableHashCode();

        /// <summary>OWNER ONLY, on a fresh roll. A creature this mod made itself (<paramref name="rolled"/> false: a
        /// console spawn, a raider, a newborn, an altar's boss) carries no other mod's health.</summary>
        public static void Measure(Character creature, ZDO zdo, bool rolled)
        {
            float game = creature.GetMaxHealthBase() * Mathf.Max(1, creature.GetLevel());
            float factor = rolled && game > 0f ? creature.GetMaxHealth() / game : 1f;
            if (Mathf.Abs(factor - 1f) > 0.01f)
            {
                zdo.Set(Hash, Mathf.Clamp(factor, 0.05f, 100f));
            }
        }

        public static float Of(ZDO? zdo) => zdo == null ? 1f : zdo.GetFloat(Hash, 1f);
    }
}
