using EliteCreaturesReborn.Util;
using LocalEffects;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Hall
{
    /// <summary>
    /// Low mist on Hel's river (features/arena.md section 1, "Light and mood"), clients only: the river bundle's own
    /// <c>ecr_helarena_rivermist</c> (<see cref="HallRiverFx"/>: about 12 cards 14 to 24 m across in a 50 x 50 m patch
    /// 0.5 to 2.3 m over the liquid, cold blue-grey at alpha 0.14, looping and prewarmed, 14 particles at most), copied
    /// local-only through LocalEffects at every one of the land's 36 <c>mist_*</c> points (on the liquid at y -8, each
    /// turned to face the arena): 504 particles at most, large, slow and mostly far. The copies are children of their
    /// points, so they go with the arena.
    /// </summary>
    internal static class HallMist
    {
        private const int Points = 36;

        public static void Lay()
        {
            GameObject? mist = HallRiverFx.Mist;
            if (mist == null)
            {
                Log.Warn("Hel's arena: the river's mist is missing; no mist on the liquid.");
                return;
            }
            for (int k = 0; k < Points; k++)
            {
                if (!HallPoints.TryGet("mist_" + k.ToString("00"), out Transform at))
                {
                    continue;
                }
                GameObject? copy = LocalEffect.Attach(mist, at, at.position, endless: true);
                if (copy != null)
                {
                    copy.transform.rotation = at.rotation;
                }
            }
        }
    }
}
