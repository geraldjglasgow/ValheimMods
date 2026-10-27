using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The ship's own top speed against the game's karve (the top speed line's tooltip, <see cref="KwShipTopSpeed"/>):
    /// from its build, times the helmsman's Sailing speed factor (GrindstoneSkills, <see cref="KwGrindstoneLink"/>; 1
    /// without it). Under sail it is the blueprint table's "x% of a karve's" (<see cref="KwBpHandling.Relative"/>): the
    /// ship's figures from its blueprint as it is now (<see cref="KwShipStats"/>, weighed as <see cref="KwSeaRedraw"/> weighs
    /// it), its top speed under full sail at the best angle to a full wind (<see cref="KwSailSpeeds.Best"/>) against a
    /// karve's in the same step. Under oars (slow ahead or astern) the oars push as a karve's, so it is the hull's own
    /// speed factor (<see cref="KwShipStats.Speed"/>: the drag along the ship against the karve's). Worked out once per
    /// ship and again after each sea edit (<see cref="KwSeaShip.Version"/>), never per refresh; the karve's once a session.
    /// </summary>
    internal static class KwShipPace
    {
        private static float karve = -1f;
        private static KwSeaShip? measured;
        private static int version = int.MinValue;
        private static float sailing = 1f, rowing = 1f;

        /// <summary>The ship's own top speed against a karve's, from its build; 0 under sail when no sail pulls it.</summary>
        public static float Build(KwSeaShip ship, bool rowed)
        {
            if (ship != measured || ship.Version != version)
                Measure(ship);
            return rowed ? rowing : sailing;
        }

        // The ship's figures from its blueprint as it is now: its sailing top speed against a karve's, its hull's speed factor.
        private static void Measure(KwSeaShip ship)
        {
            (measured, version) = (ship, ship.Version);
            KwShipHydro? hydro = ship.GetComponent<KwShipHydro>();
            if (hydro == null || ship.Blueprint == null || ship.Hull == null)
            {
                (sailing, rowing) = (1f, 1f);
                return;
            }
            KwShipStats stats = KwShipStats.Of(ship.Blueprint, ship.Hull, KwShipWeight.Sized(hydro.Slices, ship.Hull));
            float drag = KwShipStats.KarveDrag / (stats.Speed * stats.Speed) * KwShipDrive.DragShare;
            float top = stats.SailFactor > 0f ? KwSailSpeeds.Best(stats.Mix, stats.SailFactor, drag).Speed : 0f;
            sailing = top / Karve();
            rowing = stats.Speed;
        }

        // A karve's top speed in the same step: its one square sail, its sail force and drag, in full wind (as KwBpHandling's).
        private static float Karve()
        {
            if (karve <= 0f)
                karve = Mathf.Max(0.01f, KwSailSpeeds.Best(KwSailMix.Only(KwSailKind.Square), KwShipStats.KarveFactor, KwShipStats.KarveDrag * KwShipDrive.DragShare).Speed);
            return karve;
        }
    }
}
