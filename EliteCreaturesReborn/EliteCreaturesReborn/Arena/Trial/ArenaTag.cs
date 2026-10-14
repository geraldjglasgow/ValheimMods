namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// The tag that makes a creature an arena creature, in its own ZDO (features/arena.md section 4: "arena creatures are
    /// tagged in their ZDO with the trial's id"), as <see cref="Raids.RaiderTag"/> tags a raider: the trial it fights in,
    /// the wave it fights in, whether it is the wave's boss, and the biome tier its kill's souls go by. Written by its
    /// owner in the frame it is made (<see cref="ArenaSpawn"/>) or as it is adopted (<see cref="ArenaFighter"/>: whatever appears in the hall during
    /// a trial - a boss's next phase, a Summoner's adds, a Splintering creature's copies), so it travels with the creature
    /// to whichever machine owns it next. Every read is one ZDO lookup.
    /// </summary>
    public static class ArenaTag
    {
        /// <summary>Tags a creature for <paramref name="trial"/>. Owner only.</summary>
        public static void Write(ZDO creature, long trial, bool boss, int biomeNumber, int wave)
        {
            creature.Set(ArenaKeys.CreatureTrialHash, trial);
            creature.Set(ArenaKeys.CreatureBossHash, boss);
            creature.Set(ArenaKeys.CreatureTierHash, biomeNumber);
            creature.Set(ArenaKeys.CreatureWaveHash, wave);
        }

        /// <summary>Takes the tag off: a creature the players have tamed is theirs now. Owner only.</summary>
        public static void Clear(ZDO creature) => creature.Set(ArenaKeys.CreatureTrialHash, 0L);

        /// <summary>The trial it fights in; 0 for a creature that is no arena creature.</summary>
        public static long Trial(ZDO? zdo) => zdo != null ? zdo.GetLong(ArenaKeys.CreatureTrialHash) : 0L;

        /// <summary>True for an arena creature, of any trial.</summary>
        public static bool IsArena(ZDO? zdo) => Trial(zdo) != 0L;

        public static bool IsBoss(ZDO zdo) => zdo.GetBool(ArenaKeys.CreatureBossHash);

        /// <summary>The wave it fights in; once the trial has cleared or swept that wave, it vanishes.</summary>
        public static int Wave(ZDO zdo) => zdo.GetInt(ArenaKeys.CreatureWaveHash);

        /// <summary>The biome tier its kill's souls go by, at least 1.</summary>
        public static int Tier(ZDO zdo) => UnityEngine.Mathf.Max(1, zdo.GetInt(ArenaKeys.CreatureTierHash));
    }
}
