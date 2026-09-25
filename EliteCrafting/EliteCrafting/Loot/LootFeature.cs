using EliteCrafting.Rules;

namespace EliteCrafting.Loot
{
    /// <summary>
    /// Entry point of the Loot area, called once from plugin Awake after the rules, settings and words are loaded.
    /// Owns: creature stone drops and pre-rolled magic gear drops on the dying creature's owner (drops.md); chest and
    /// world-container drops on the container's owner (section 11); the loot-find stats (section 10) - published by each
    /// player's own client to its player ZDO, read by the creature's owner at the kill.
    /// The patches are <see cref="DeathPatch"/> (the roll, killer stats), <see cref="AllyHitPatch"/> (<c>ecf_ally_hit</c>),
    /// <see cref="GenerateDropListPatch"/> (Trophy Taker, Hoardfinder), <see cref="ChestFillPatch"/> (chests) and
    /// <see cref="FindEquipmentPatch"/> / <see cref="FindSpawnPatch"/> (republishing the find stats).
    /// Tables: the per-tier stone and rarity draws are precomputed by the rules (<c>EconomyRules.StoneDraw</c>,
    /// <c>GearRarityDraw</c>); the creature profiles and the gear pool are derived here and dropped on every rules change.
    /// </summary>
    public static class LootFeature
    {
        public static void Init()
        {
            ActiveRules.RulesChanged += OnRulesChanged;
            FindPublisher.Install();
        }

        private static void OnRulesChanged()
        {
            CreatureProfiles.Clear();
            GearPool.Invalidate();
        }
    }
}
