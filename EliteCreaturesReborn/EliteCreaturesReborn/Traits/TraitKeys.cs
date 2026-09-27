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

        /// <summary>World tiers: the tier a wild creature was rolled at, for `elite inspect`. Written only when above 0.</summary>
        public const string Tier = "ecr_tier";

        /// <summary>Breeding: the partner's stars plus one, on the pregnant parent's ZDO from conception to birth; 0 or absent
        /// means no partner was beside it. Plus one so a plain partner still reads as a partner.</summary>
        public const string SireStars = "ecr_sire_stars";

        /// <summary>Breeding: the partner's mutation set, beside <see cref="SireStars"/>.</summary>
        public const string SireMask = "ecr_sire_mask";

        /// <summary>Breeding: an egg's inherited traits, in the egg item's own custom data rather than a ZDO, so they travel
        /// with it through inventories and chests. "stars,mask,biome", invariant integers.</summary>
        public const string EggTraits = "ecr_egg";

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

        /// <summary>Phantom: on the boss, how many of its `split at` marks it has split at, so a hand-over repeats none.</summary>
        public const string PhantomSplits = "ecr_phantom_splits";

        /// <summary>Summoner: how many waves the boss has called, so a hand-over neither repeats nor skips one.</summary>
        public const string SummonWaves = "ecr_waves";

        /// <summary>Boss damage board: on a boss, the health each player has taken off it, written by its owner at each
        /// hit so a hand-over mid-fight keeps the count.</summary>
        public const string BossDamage = "ecr_boss_dmg";

        /// <summary>Adaptive: the damage type the boss resists right now, by its <c>AdaptiveType</c> value (0 or absent for
        /// none). Written by the boss's owner only when it changes, so every client glows the same colour and a new owner
        /// carries on from it.</summary>
        public const string Adapted = "ecr_adaptive";

        /// <summary>Fixated: on the boss, the marked player's character as a ZDOID (stored under this name plus "_u" and
        /// "_i"), written by the boss's owner so every machine sees the same mark and a new owner keeps it.</summary>
        public const string Fixated = "ecr_fixated";

        /// <summary>Fixated: when the mark last landed or moved (shared-clock ms), so a client says the chat line only for
        /// a change that just happened, never for a mark it meets later.</summary>
        public const string FixatedAt = "ecr_fixated_at";

        /// <summary>Blinking: when the last tell went out (shared-clock ms), so a new owner never blinks twice.</summary>
        public const string BlinkedAt = "ecr_blink_at";

        /// <summary>Stormbound: when the latest storm's lightning falls (shared-clock ms), on the boss. Written by its
        /// owner with the circles, so every client strikes at the same moment, a player arriving mid-storm still sees it,
        /// and a new owner carries on the rhythm instead of calling a second storm at once.</summary>
        public const string StormAt = "ecr_storm_at";

        /// <summary>Stormbound: the latest storm, packed, on the boss - the radius and damage its owner called it with and
        /// its circle centres, so every client draws and judges with the same numbers; read with <see cref="StormAt"/>.</summary>
        public const string StormCircles = "ecr_storm";

        /// <summary>Relentless: the ZDOID of the target it has picked, written by its owner and cleared when lost, so a
        /// new owner takes up the same hunt.</summary>
        public const string Quarry = "ecr_quarry";

        /// <summary>Gravitic: when the boss last roared (shared-clock ms), written by its owner, so a new owner keeps the
        /// rhythm and never roars twice.</summary>
        public const string GravityAt = "ecr_gravity_at";

        /// <summary>Colossal: on a boss's ragdoll, the size its corpse keeps, written by the ragdoll's owner so every machine
        /// grows its copy to match the boss that fell.</summary>
        public const string CorpseScale = "ecr_corpse_scale";
    }
}
