using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Breeding speed, herd size and contentment, for one breeding check on the creature's owner (<see cref="BreedingCheck"/>).
    /// The game: 3 successful love checks (4 for lox) make a tamed animal pregnant, and it gives birth once
    /// m_pregnancyDuration has passed (60 s, 120 s for lox). A love check is skipped when a roll falls under
    /// m_pregnancyChance (so that value is really the skip chance), or while the animal is alerted or hungry. No love
    /// check runs while m_maxCreatures of its kind (its own prefab plus its offspring's) stand within m_totalCheckRange.
    /// For the one check, with the speed factor f = 1 + Breeding Speed's share + Content Breeding Bonus while the animal
    /// is content: the pregnancy duration and the skip chance are divided by f, and Herd Size's share of the keeper's
    /// level (rounded) is added to the crowding limit. The game's values are put back afterwards (<see cref="Restore"/>),
    /// so anything else that sets them is kept.
    /// </summary>
    public static class BreedingPace
    {
        private static readonly int ContentHash = Keys.ContentUntil.GetStableHashCode();

        /// <summary>Records the game's values and the keeper's level, then paces the check. Nothing is changed before the record.</summary>
        public static BreedingCall Apply(Procreation procreation)
        {
            float level = Keeper.BestLevel(procreation.transform.position);
            float factor = SpeedFactor(procreation.m_nview, level);
            int room = ExtraRoom(level);
            BreedingCall call = new BreedingCall
            {
                Level = level,
                Paced = factor > 1f || room > 0,
                PregnancyDuration = procreation.m_pregnancyDuration,
                PregnancyChance = procreation.m_pregnancyChance,
                MaxCreatures = procreation.m_maxCreatures,
            };
            if (!call.Paced)
                return call;
            procreation.m_pregnancyDuration /= factor;
            procreation.m_pregnancyChance /= factor;
            procreation.m_maxCreatures += room;
            return call;
        }

        public static void Restore(Procreation procreation, BreedingCall call)
        {
            if (!call.Paced)
                return;
            procreation.m_pregnancyDuration = call.PregnancyDuration;
            procreation.m_pregnancyChance = call.PregnancyChance;
            procreation.m_maxCreatures = call.MaxCreatures;
        }

        /// <summary>1 + Breeding Speed's share at <paramref name="level"/>, plus Content Breeding Bonus while the animal is content.</summary>
        public static float SpeedFactor(ZNetView nview, float level)
        {
            float factor = 1f + HusbandrySkill.Share(HusbandryBreedingSettings.BreedingSpeed.Value, level);
            if (IsContent(nview))
                factor += Mathf.Max(0f, HusbandryBreedingSettings.ContentBonus.Value) / 100f;
            return factor;
        }

        /// <summary>Herd Size times the keeper's level over 100, rounded half up: 4 at level 100 gives 1 from level 13.</summary>
        public static int ExtraRoom(float level)
        {
            float share = Mathf.Max(0, HusbandryBreedingSettings.HerdSize.Value) * Mathf.Clamp01(level / HusbandrySkill.MaxLevel);
            return Mathf.FloorToInt(share + 0.5f);
        }

        /// <summary>Petted recently: its <see cref="Keys.ContentUntil"/> lies ahead. Never while Content Duration is 0 (contentment off).</summary>
        public static bool IsContent(ZNetView nview)
        {
            if (HusbandryBreedingSettings.ContentDuration.Value <= 0f || nview == null || !nview.IsValid())
                return false;
            return nview.GetZDO().GetLong(ContentHash) > Herd.Now.Ticks;
        }
    }
}
