using EliteCreaturesReborn.Traits;

namespace EliteCreaturesReborn.Visuals
{
    /// <summary>
    /// A starred creature's corpse keeps its star look. The game dresses a ragdoll from the dying creature's level, which
    /// is 1 for every elite. Its tint is corrected where the game reads it (<see cref="Patches.StarLookColorPatch"/>) and
    /// travels on the ragdoll's record by the game's own route. The rest of the look is ours: on the creature's owner,
    /// as the game makes the ragdoll, the star count is written on the ragdoll's record in the same frame, and every
    /// other machine dresses its copy from it as the copy wakes.
    /// </summary>
    internal static class StarCorpse
    {
        /// <summary>Owner side, as the creature makes its ragdoll.</summary>
        public static void Made(Character creature, Ragdoll ragdoll)
        {
            int stars = StarLook.StarsOf(creature);
            ZNetView nview = ragdoll.GetComponent<ZNetView>();
            if (stars <= 0 || nview == null || !nview.IsValid())
            {
                return;
            }
            nview.GetZDO().Set(TraitKeys.CorpseStars, stars);
            Show(ragdoll, stars);
        }

        /// <summary>
        /// Every machine, as a ragdoll wakes. The owner's own copy wakes before the stars are written, reads none, and is
        /// dressed in <see cref="Made"/> instead.
        /// </summary>
        public static void Wake(Ragdoll ragdoll)
        {
            ZNetView nview = ragdoll.GetComponent<ZNetView>();
            int stars = nview != null && nview.IsValid() ? nview.GetZDO().GetInt(TraitKeys.CorpseStars) : 0;
            if (stars > 0)
            {
                Show(ragdoll, stars);
            }
        }

        private static void Show(Ragdoll ragdoll, int stars)
        {
            foreach (LevelEffects effects in ragdoll.GetComponentsInChildren<LevelEffects>())
            {
                StarLook.Show(effects, stars);
            }
        }
    }
}
