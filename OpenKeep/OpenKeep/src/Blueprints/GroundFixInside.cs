using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// What lies inside a building (<see cref="GroundFixBuilding.WithInside"/>, the Copy tool's Shift + click): pieces
    /// not joined to it whose centre lies within the footprint of one of its pieces (a floor under them, a roof over
    /// them) and whose bottom is between the building's lowest bottom and highest top: furniture, chests, racks and
    /// lights that stand free inside.
    /// </summary>
    public static class GroundFixInside
    {
        /// <summary>Adds the pieces of the grid that lie inside the building and are not in it yet.</summary>
        public static void Add(Dictionary<long, List<FixPiece>> grid, List<FixPiece> building, HashSet<Piece> seen)
        {
            Dictionary<long, List<FixPiece>> covers = new Dictionary<long, List<FixPiece>>();
            foreach (FixPiece p in building)
                GroundFixBuilding.Add(covers, p);
            float low = building.Min(p => p.Bottom), high = building.Max(p => p.Bottom + p.Height);
            List<FixPiece> inside = grid.Values.SelectMany(cell => cell)
                .Where(p => !seen.Contains(p.Piece) && p.Bottom >= low - 0.5f && p.Bottom <= high && Covered(covers, p)).ToList();
            foreach (FixPiece p in inside)
            {
                if (building.Count >= BlueprintRules.FixMaxPieces)
                    return;
                if (seen.Add(p.Piece))
                    building.Add(p);
            }
        }

        /// <summary>A piece of the building has the piece's centre within its footprint.</summary>
        private static bool Covered(Dictionary<long, List<FixPiece>> covers, FixPiece p)
        {
            foreach (FixPiece cover in GroundFixBuilding.Near(covers, p))
            {
                if (cover.Covers(p.Centre.x, p.Centre.z))
                    return true;
            }
            return false;
        }
    }
}
