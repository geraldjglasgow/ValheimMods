using System.Collections.Generic;

namespace OpenKeep.Blueprints.Copy
{
    /// <summary>
    /// The joined pieces of one type around a piece, for G: every piece of its building with the same prefab reached
    /// through touching boxes of that prefab (a wall run, a roof slope, a row of floor tiles, a fence). The last answer
    /// is kept, so hovering asks once per piece.
    /// </summary>
    public static class CopySameType
    {
        private static Piece lastPiece;
        private static List<FixPiece> lastBuilding;
        private static List<FixPiece> lastGroup;

        /// <summary>The joined pieces of the start's type in its building (the start first); empty when the start is not in it.</summary>
        public static List<FixPiece> Of(List<FixPiece> building, Piece start)
        {
            if (ReferenceEquals(start, lastPiece) && ReferenceEquals(building, lastBuilding) && lastGroup != null)
                return lastGroup;
            lastGroup = Find(building, start);
            lastPiece = start;
            lastBuilding = building;
            return lastGroup;
        }

        private static List<FixPiece> Find(List<FixPiece> building, Piece start)
        {
            string prefab = Utils.GetPrefabName(start.gameObject);
            List<FixPiece> same = building.FindAll(p => p.Piece != null && Utils.GetPrefabName(p.Piece.gameObject) == prefab);
            FixPiece first = same.Find(p => ReferenceEquals(p.Piece, start));
            return first == null ? new List<FixPiece>() : new CopyGrid(same).Flood(first, new HashSet<Piece>());
        }
    }
}
