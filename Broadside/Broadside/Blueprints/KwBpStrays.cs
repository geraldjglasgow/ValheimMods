using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Which of a dock's pieces belong to its ship: with hull parts on the dock, only the pieces within the hull's length
    /// and beam and a margin round it (room for a rudder, a bowsprit, a rail); a piece left lying further along the dock
    /// is no part of the ship, so it is not saved with it, and a file saved with one builds without it. A dock with no hull
    /// parts keeps every piece.
    /// </summary>
    public static class KwBpStrays
    {
        private const float Margin = 3f;

        /// <summary>The dock's ship pieces that are aboard its hull.</summary>
        public static List<Piece> Aboard(KwBpDockInfo dock)
        {
            List<Piece> pieces = KwBpDock.Ship(dock);
            KwBlueprint blueprint = KwBlueprint.Capture("", dock.Frame, pieces);
            if (KwBpHull.Parts(blueprint).Count == 0)
                return pieces;
            KwBpHull hull = KwBpHull.Measure(blueprint);
            Quaternion back = Quaternion.Inverse(dock.Frame.rotation);
            return pieces.FindAll(p => On(hull, back * (p.transform.position - dock.Frame.position)));
        }

        /// <summary>The blueprint without the pieces that lie away from its hull (itself when it has no hull parts).</summary>
        public static KwBlueprint Trim(KwBlueprint blueprint)
        {
            if (KwBpHull.Parts(blueprint).Count == 0)
                return blueprint;
            KwBpHull hull = KwBpHull.Measure(blueprint);
            KwBlueprint kept = new KwBlueprint(blueprint.Name);
            kept.Pieces.AddRange(blueprint.Pieces.FindAll(p => On(hull, p.Position)));
            return kept;
        }

        private static bool On(KwBpHull hull, Vector3 at) =>
            at.z > hull.Aft - Margin && at.z < hull.Fore + Margin && Mathf.Abs(at.x) < hull.Beam / 2f + Margin;
    }
}
