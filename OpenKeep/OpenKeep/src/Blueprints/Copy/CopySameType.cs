using System.Collections.Generic;

namespace OpenKeep.Blueprints.Copy
{
    /// <summary>
    /// The pieces of one type around a piece: for G every piece of its building with the same prefab reached through
    /// touching boxes of that prefab (a wall run, a roof slope, a row of floor tiles, a fence), for Shift + G every piece of
    /// its building with that prefab. The last answer is kept, so hovering asks once per piece.
    /// </summary>
    public static class CopySameType
    {
        private static Piece lastPiece;
        private static List<FixPiece> lastBuilding;
        private static bool lastWhole;
        private static List<FixPiece> lastGroup;

        /// <summary>
        /// The pieces of the start's type in its building: every one with <paramref name="wholeBuilding"/>, else the ones
        /// joined to it (the start first); empty when the start is not in it.
        /// </summary>
        public static List<FixPiece> Of(List<FixPiece> building, Piece start, bool wholeBuilding)
        {
            if (ReferenceEquals(start, lastPiece) && ReferenceEquals(building, lastBuilding) && wholeBuilding == lastWhole && lastGroup != null)
                return lastGroup;
            lastGroup = Find(building, start, wholeBuilding);
            lastPiece = start;
            lastBuilding = building;
            lastWhole = wholeBuilding;
            return lastGroup;
        }

        private static List<FixPiece> Find(List<FixPiece> building, Piece start, bool wholeBuilding)
        {
            string prefab = Utils.GetPrefabName(start.gameObject);
            List<FixPiece> same = building.FindAll(p => p.Piece != null && Utils.GetPrefabName(p.Piece.gameObject) == prefab);
            FixPiece first = same.Find(p => ReferenceEquals(p.Piece, start));
            if (first == null)
                return new List<FixPiece>();
            return wholeBuilding ? same : new CopyGrid(same).Flood(first, new HashSet<Piece>());
        }
    }
}
