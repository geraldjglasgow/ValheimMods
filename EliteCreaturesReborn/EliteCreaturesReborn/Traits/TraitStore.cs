using System.Collections.Generic;

namespace EliteCreaturesReborn.Traits
{
    /// <summary>
    /// Moves <see cref="CreatureTraits"/> to and from a creature's ZDO. This is the single seam between the resolved
    /// traits and their persisted form; nothing else touches the trait ZDO keys. Reads work on any client (the ZDO
    /// is shared); writes only take on the owner, which is where resolution happens.
    /// </summary>
    public static class TraitStore
    {
        private static readonly int ResolvedHash = TraitKeys.Resolved.GetStableHashCode();
        private static readonly int StarsHash = TraitKeys.Stars.GetStableHashCode();
        private static readonly int MaskHash = TraitKeys.Mask.GetStableHashCode();
        private static readonly int AspectHash = TraitKeys.Aspect.GetStableHashCode();
        private static readonly int AspectExtraHash = TraitKeys.AspectExtra.GetStableHashCode();
        private static readonly int TierHash = TraitKeys.Tier.GetStableHashCode();
        private static readonly int SireStarsHash = TraitKeys.SireStars.GetStableHashCode();
        private static readonly int SireMaskHash = TraitKeys.SireMask.GetStableHashCode();
        private static readonly int BiomeHash = TraitKeys.Biome.GetStableHashCode();
        private static readonly int DevouredHealthHash = TraitKeys.DevouredHealth.GetStableHashCode();
        private static readonly int DevouredDamageHash = TraitKeys.DevouredDamage.GetStableHashCode();
        private static readonly int DevourReadyAtHash = TraitKeys.DevourReadyAt.GetStableHashCode();
        private static readonly KeyValuePair<int, int> PhantomOfKey = ZDO.GetHashZDOID(TraitKeys.PhantomOf);
        private static readonly KeyValuePair<int, int> CloneOfKey = ZDO.GetHashZDOID(TraitKeys.CloneOf);
        private static readonly KeyValuePair<int, int> DevouredByKey = ZDO.GetHashZDOID(TraitKeys.DevouredBy);

        public static bool IsResolved(ZDO zdo) => zdo != null && zdo.GetBool(ResolvedHash);

        public static CreatureTraits Load(ZDO zdo)
        {
            int stars = zdo.GetInt(StarsHash);
            int mask = zdo.GetInt(MaskHash);
            return new CreatureTraits(stars, mask)
            {
                Aspect = (Aspect)zdo.GetInt(AspectHash),
                ExtraAspects = zdo.GetInt(AspectExtraHash),
                PhantomCopy = zdo.GetZDOID(PhantomOfKey) != ZDOID.None,
                Decoy = zdo.GetZDOID(CloneOfKey) != ZDOID.None,
                Tier = zdo.GetInt(TierHash),
            };
        }

        public static void Save(ZDO zdo, CreatureTraits traits)
        {
            zdo.Set(TraitKeys.Stars, traits.Stars);
            zdo.Set(TraitKeys.Mask, traits.Mask);
            if (traits.Aspect != Aspect.None)
            {
                zdo.Set(TraitKeys.Aspect, (int)traits.Aspect); // only bosses carry one; creatures send no extra key
            }
            if (traits.ExtraAspects != 0)
            {
                zdo.Set(TraitKeys.AspectExtra, traits.ExtraAspects); // only a Bountiful boss carries more than one
            }
            if (traits.Tier > 0)
            {
                zdo.Set(TraitKeys.Tier, traits.Tier); // a tier-0 world sends no extra key
            }
            zdo.Set(TraitKeys.Resolved, true);
        }

        /// <summary>Breeding, at conception, on the pregnant parent's owner: remember the partner's traits (or that there
        /// was none) until the birth, since the partner may have wandered off by then.</summary>
        public static void SetSire(ZDO zdo, CreatureTraits? sire)
        {
            zdo.Set(TraitKeys.SireStars, sire != null ? sire.Stars + 1 : 0);
            zdo.Set(TraitKeys.SireMask, sire?.Mask ?? 0);
        }

        /// <summary>Breeding, at birth: the partner remembered at conception, cleared so the next pregnancy starts fresh.
        /// Null when there was no partner, or the pregnancy began before this was recorded.</summary>
        public static CreatureTraits? TakeSire(ZDO zdo)
        {
            int stars = zdo.GetInt(SireStarsHash);
            if (stars <= 0)
            {
                return null;
            }
            CreatureTraits sire = new CreatureTraits(stars - 1, zdo.GetInt(SireMaskHash));
            SetSire(zdo, null);
            return sire;
        }

        public static void SetBiome(ZDO zdo, Heightmap.Biome biome) => zdo.Set(TraitKeys.Biome, (int)biome);

        public static Heightmap.Biome GetBiome(ZDO zdo) => (Heightmap.Biome)zdo.GetInt(BiomeHash, (int)Heightmap.Biome.None);

        public static float GetDevouredHealth(ZDO zdo) => zdo.GetFloat(DevouredHealthHash);

        public static float GetDevouredDamage(ZDO zdo) => zdo.GetFloat(DevouredDamageHash);

        public static void AddDevoured(ZDO zdo, float health, float damage)
        {
            zdo.Set(TraitKeys.DevouredHealth, GetDevouredHealth(zdo) + health);
            zdo.Set(TraitKeys.DevouredDamage, GetDevouredDamage(zdo) + damage);
        }

        /// <summary>Which devourer instant-killed this prey; <see cref="ZDOID.None"/> when nothing devoured it. On the PREY's ZDO.</summary>
        public static ZDOID GetDevouredBy(ZDO zdo) => zdo.GetZDOID(DevouredByKey);

        /// <summary>Marks this prey as devoured by the given creature. Runs on the prey's owner (RPC_Damage), set at the bite
        /// so the death path feeds exactly that devourer even after the prey changes hands.</summary>
        public static void MarkDevouredBy(ZDO zdo, ZDOID devourer) => zdo.Set(TraitKeys.DevouredBy, devourer);

        /// <summary>The shared-clock instant a devourer may eat again, in whole milliseconds (0 when it never has). On the DEVOURER's ZDO.</summary>
        public static long GetDevourReadyAt(ZDO zdo) => zdo.GetLong(DevourReadyAtHash, 0L);

        /// <summary>Owner-only write, at the moment a meal is banked: the cooldown starts here and every client reads it.</summary>
        public static void SetDevourReadyAt(ZDO zdo, long whenMs) => zdo.Set(TraitKeys.DevourReadyAt, whenMs);
    }
}
