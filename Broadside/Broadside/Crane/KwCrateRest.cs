using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Where a crate let go comes to rest: straight down, upright and keeping its turn, onto the first solid thing under
    /// its box (the ground, a floor, a deck, another piece; the ground's height when nothing is found), or afloat where
    /// that would be under water, its bottom <see cref="Draft"/> under the calm surface.
    /// </summary>
    public static class KwCrateRest
    {
        /// <summary>How deep a floating crate sits in the water (m).</summary>
        public const float Draft = 0.35f;

        public static (Vector3 At, bool Afloat) Below(KwCrate crate)
        {
            Transform t = crate.transform;
            Quaternion upright = Quaternion.Euler(0f, t.eulerAngles.y, 0f);
            KwCrane? crane = crate.HangingFrom();
            float free = KwCargoCast.FreeBelow(t.position + upright * crate.BoxCentre, crate.Half, upright, t, crane != null ? crane.transform : null);
            Vector3 at = t.position + Vector3.down * Mathf.Max(0f, free);
            if (free >= KwCargoCast.Far - 1f && ZoneSystem.instance != null)
                at.y = ZoneSystem.instance.GetGroundHeight(t.position);
            float? water = KwCargoCast.Water(at, 0f);
            return water.HasValue && water.Value - Draft > at.y ? (new Vector3(at.x, water.Value - Draft, at.z), true) : (at, false);
        }
    }
}
