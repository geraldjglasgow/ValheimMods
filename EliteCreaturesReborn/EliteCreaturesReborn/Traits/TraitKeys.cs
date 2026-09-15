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

        /// <summary>Devouring: which devourer instant-killed THIS prey, as its ZDOID, set on the prey's ZDO at the bite so
        /// it survives a handover. Set only on a genuine devour, so the prey's death path feeds that devourer and no other.</summary>
        public const string DevouredBy = "ecr_devoured_by";
    }
}
