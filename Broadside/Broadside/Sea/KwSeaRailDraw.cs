using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A sea edit's railings redrawn on every peer, nothing else of the ship: each changed railing's own look (railings are
    /// drawn apart, one renderer each, <see cref="KwShipRailLooks"/>; only where there is a screen), its stretches' boxes
    /// (<see cref="KwShipRails"/>, made with the first railing of a ship launched without any), and the shield hooks, laid
    /// again in the blueprint's order (<see cref="KwShipShields"/>; the owner renumbered the hung shields to match). The
    /// ship's shadow boxes and its ladder over the side follow in the common step (<see cref="KwSeaRedraw"/>).
    /// </summary>
    public static class KwSeaRailDraw
    {
        public static void Redraw(KwSeaShip ship, KwSeaChanges mine)
        {
            KwShipRails? rails = ship.GetComponent<KwShipRails>();
            if (rails != null)
                rails.Refit(mine.Indices());
            else
                rails = KwShipRails.Add(ship.transform, ship.Blueprint);
            if (KwShipBody.Drawn)
                Looks(ship, mine, rails);
            KwShipShields.Of(ship.transform).Relay(ship.Fittings, ship.Blueprint);
        }

        // Each changed railing's look let go and its new one made, apart, in the ship's frame.
        private static void Looks(KwSeaShip ship, KwSeaChanges mine, KwShipRails? rails)
        {
            KwShipRailLooks looks = ship.Visual.GetComponent<KwShipRailLooks>() ?? Made(ship);
            if (rails != null)
                KwShipRailLooks.Watch(ship.transform, rails);
            foreach (KwSeaChange change in mine.All)
            {
                (Material? material, Mesh? mesh, Matrix4x4 at) = change.New != null ? Look(ship, change.New) : (null, null, Matrix4x4.identity);
                looks.Swap(change.Old, change.New, material, mesh, at);
            }
        }

        private static KwShipRailLooks Made(KwSeaShip ship)
        {
            KwShipRailLooks.Keep(ship.Visual, new List<(KwBpPiece Piece, Material Material, Mesh Mesh, Matrix4x4 At)>());
            return ship.Visual.GetComponent<KwShipRailLooks>();
        }

        /// <summary>A railing piece's own look as the ship's look makes it (seated on the ship's keels), in the ship's frame.</summary>
        public static (Material? Material, Mesh? Mesh, Matrix4x4 At) Look(KwSeaShip ship, KwBpPiece piece)
        {
            KwBpMeshSet set = new KwBpMeshSet { Keels = ship.Keels, Apart = _ => true };
            set.Add(piece, Vector3.zero);
            for (int i = 1; i < set.Parts.Count; i++)
                Object.Destroy(set.Parts[i].Mesh);
            return set.Parts.Count > 0 ? (set.Parts[0].Material, set.Parts[0].Mesh, set.Parts[0].At) : (null, null, Matrix4x4.identity);
        }
    }
}
