using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Where the ship's design waterline stands on its rudder (<see cref="KwRudderFit.Water"/>), as the launched ship floats:
    /// its hull's keel line plus its draught (<see cref="KwShipTuning.Draught"/>), the line the dock's waterline shows,
    /// given as a height over the rudder point. A launched ship's rudder has it from the ship's blueprint
    /// (<see cref="KwShipParts.Fit"/>), the same on every peer. On the dock the rudder's owner measures it from the ship on
    /// the dock when it measures the rudder's lay (<see cref="KwRudderBlade"/>) and keeps it in the rudder's ZDO
    /// (<see cref="Key"/>, so it travels in a blueprint too), and every peer builds the blade to what the ZDO holds; a
    /// rudder's ghost measures it for itself (<see cref="KwRudderPlace"/>). Not known (no dock or no hull parts on it), it is NaN.
    /// </summary>
    public static class KwRudderWater
    {
        public const string Key = "kw_rudder_water";
        private static readonly int KeyHash = Key.GetStableHashCode();

        /// <summary>The design waterline over a height (the rudder point's) in a blueprint's frame; NaN without hull parts.</summary>
        public static float Over(KwBlueprint ship, float height)
        {
            if (KwBpHull.Parts(ship).Count == 0)
                return float.NaN;
            KwBpHull hull = KwBpHull.Measure(ship);
            return hull.Keel + KwShipTuning.Draught(hull) - height;
        }

        /// <summary>
        /// The design waterline over a rudder point on the dock, from the ship on the dock nearest it (its pieces aboard the
        /// hull, as the dock's waterline and a saved blueprint have them); NaN with no dock there or no hull on it.
        /// </summary>
        public static float OnDock(Vector3 point)
        {
            KwBpDockInfo? dock = KwBpDock.Find(point);
            if (dock == null)
                return float.NaN;
            KwBlueprint ship = KwBpStrays.Trim(KwBlueprint.Capture("", dock.Frame, KwBpDock.Ship(dock)));
            return Over(ship, (Quaternion.Inverse(dock.Frame.rotation) * (point - dock.Frame.position)).y);
        }

        /// <summary>The waterline a rudder's ZDO holds; NaN with none.</summary>
        public static float Of(ZDO? zdo) => zdo != null ? zdo.GetFloat(KeyHash, float.NaN) : float.NaN;

        /// <summary>Writes a measured waterline into a rudder's ZDO when it differs from what is there (only its owner calls this).</summary>
        public static void Save(ZDO zdo, float water)
        {
            if (!float.IsNaN(water) && !Same(Of(zdo), water))
                zdo.Set(KeyHash, water);
        }

        /// <summary>Whether two waterlines are the same for the blade (within a centimetre, or both not known).</summary>
        public static bool Same(float a, float b) => float.IsNaN(a) ? float.IsNaN(b) : !float.IsNaN(b) && Mathf.Abs(a - b) < 0.01f;
    }
}
