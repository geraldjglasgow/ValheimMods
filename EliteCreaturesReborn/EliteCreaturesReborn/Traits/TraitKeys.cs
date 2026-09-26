namespace EliteCreaturesReborn.Traits
{
    /// <summary>
    /// The ZDO key strings this slice owns. All are prefixed "ecr_" (Elite Creatures Reborn) so they never collide
    /// with the game's keys or another slice's. String overloads of the ZDO getters/setters are used, so no manual
    /// hashing is needed.
    /// </summary>
    public static class TraitKeys
    {
        /// <summary>Set once a creature's traits have been rolled, so it is never re-rolled on the owner.</summary>
        public const string Resolved = "ecr_resolved";

        /// <summary>The rolled star count (0..ceiling). Vanilla level is kept at Stars+1 alongside it.</summary>
        public const string Stars = "ecr_stars";

        /// <summary>The mutation set as a bitmask.</summary>
        public const string Mask = "ecr_mask";

        /// <summary>The biome the creature was rolled in, so its rules stay fixed as it wanders. Stored as the flag value.</summary>
        public const string Biome = "ecr_biome";

        /// <summary>Devouring: accumulated maximum-health eaten, added on top of star scaling.</summary>
        public const string DevouredHealth = "ecr_dev_hp";

        /// <summary>Devouring: accumulated per-hit damage eaten.</summary>
        public const string DevouredDamage = "ecr_dev_dmg";

        /// <summary>Splintering: how many generations deep this creature is in its cascade.</summary>
        public const string Generation = "ecr_gen";

        /// <summary>Splintering: the ZDOID of the cascade's root, as a string, for the live-descendant cap.</summary>
        public const string CascadeRoot = "ecr_root";

        /// <summary>Miasmic: the owner's rolling record of recent cloud drops, so every client draws the same trail.</summary>
        public const string MiasmaTrail = "ecr_miasma";

        /// <summary>Miasmic: a monotonic drop counter, so every cloud has a unique id no trim can reissue.</summary>
        public const string MiasmaSeq = "ecr_miasma_seq";

        /// <summary>Devouring: the shared-clock instant (whole milliseconds) at which a devourer may eat again. Written on
        /// the devourer's ZDO by its owner when it banks a meal; read anywhere to gate its next bite through the cooldown.</summary>
        public const string DevourReadyAt = "ecr_dev_ready";

        /// <summary>Respawning: the world time (ticks) a dungeon loot chest was last filled, for its regeneration timer.</summary>
        public const string LootFilledAt = "ecr_loot_at";

        /// <summary>Devouring: which devourer instant-killed THIS prey, as its ZDOID, set on the prey's ZDO at the bite so
        /// it survives a handover. Set only on a genuine devour, so the prey's death path feeds that devourer and no other.</summary>
        public const string DevouredBy = "ecr_devoured_by";

        /// <summary>Thieving: the pouch of stolen items, as one packed byte array. Owner-written, everyone-read, so the
        /// nameplate icons and `elite inspect` agree with what the creature actually holds on every machine.</summary>
        public const string Pouch = "ecr_stolen";

        /// <summary>Boss aspects: the boss's rolled aspect, by its enum value. Written only when it is not None.</summary>
        public const string Aspect = "ecr_aspect";

        /// <summary>Boss aspects: the aspect currently on an altar, on the altar's own ZDO; -1 (absent) until first rolled.</summary>
        public const string AltarAspect = "ecr_altar_aspect";

        /// <summary>Boss aspects: the world time (whole milliseconds) of an altar's next shift, on the altar's ZDO.</summary>
        public const string AltarShiftAt = "ecr_altar_shift";

        /// <summary>Twin: the partner boss's ZDOID, on each of the two, so either can find the other to share its health.</summary>
        public const string TwinPartner = "ecr_twin";

        /// <summary>Phantom: on a copy, the ZDOID of the boss that brought it. Its presence is what makes a copy a copy.</summary>
        public const string PhantomOf = "ecr_phantom_of";

        /// <summary>Summoner: how many waves the boss has called, so a hand-over neither repeats nor skips one.</summary>
        public const string SummonWaves = "ecr_waves";
    }
}
