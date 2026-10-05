using System.Collections.Generic;
using OpenKeep.Blueprints.Sites;
using OpenKeep.Core;

namespace OpenKeep.Blueprints.Planner
{
    /// <summary>The planner's view of a site's pieces: which of a set still wait to be built, and a piece's name as players read it.</summary>
    public static class PlannerPieces
    {
        /// <summary>The pieces of the set that are indices of the site's blueprint and not built yet, each once, in blueprint order.</summary>
        public static List<int> Unbuilt(SiteState state, IEnumerable<int> pieces)
        {
            List<int> left = new List<int>();
            int count = state?.Blueprint?.Pieces.Count ?? 0;
            if (pieces == null || count == 0)
                return left;
            bool[] built = state.Built;
            HashSet<int> seen = new HashSet<int>();
            foreach (int i in pieces)
            {
                if (i >= 0 && i < count && !(i < built.Length && built[i]) && seen.Add(i))
                    left.Add(i);
            }
            left.Sort();
            return left;
        }

        /// <summary>The piece's name ("Wood wall"), or its prefab name when this game does not have the piece.</summary>
        public static string Name(Blueprint bp, int index)
        {
            if (bp == null || index < 0 || index >= bp.Pieces.Count)
                return "";
            string prefab = bp.Pieces[index].Prefab;
            Piece piece = MaterialBill.PieceOf(prefab);
            return piece != null ? Language.Localize(piece.m_name) : prefab;
        }
    }
}
