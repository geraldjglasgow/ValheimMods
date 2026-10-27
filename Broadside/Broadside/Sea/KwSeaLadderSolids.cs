using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What stands round a launched ship's ladders besides its hull and its floors, for the ladder tool at sea
    /// (<see cref="KwSeaLadderShip"/>): each fitting as the anchor's cable sees it (<see cref="KwAnchorFixtures"/>: masts as
    /// columns, a hatch's coaming and open lid, a post cap, a rudder, a windlass, a wheel), kept with its piece's place in the
    /// blueprint so the hatch a ladder hangs in is left out of its own way; and the words the hover says of one.
    /// </summary>
    public static class KwSeaLadderSolids
    {
        private static readonly string[] Hull = { "planking", "deck", "rail" };
        private static readonly string[] Articles = { "a ", "an ", "the ", "another " };

        /// <summary>Whether a piece is a fitting (not the hull's planking, decks or railings, not a cabin).</summary>
        public static bool Fitting(string prefab)
        {
            if (KwHouses.IsPart(prefab))
                return false;
            string tool = prefab.StartsWith(KwToolPieces.PartPrefix, System.StringComparison.Ordinal) ? prefab.Substring(KwToolPieces.PartPrefix.Length) : "";
            return System.Array.IndexOf(Hull, tool) < 0;
        }

        /// <summary>A piece's solids, each with its place in the blueprint.</summary>
        public static void Add(List<(int Index, KwAnchorSolid Solid)> into, int index, KwBpPiece piece, KwBlueprint ship)
        {
            List<KwAnchorSolid> made = new List<KwAnchorSolid>();
            KwAnchorFixtures.Add(made, piece, KwSeaLadderShip.Pose(piece), ship);
            foreach (KwAnchorSolid solid in made)
                into.Add((index, solid));
        }

        /// <summary>Whether a solid is a hatch's coaming (a ladder stands on its top as on a floor).</summary>
        public static bool Coaming(KwAnchorSolid solid) => solid.What == "a hatch";

        /// <summary>A solid in the player's words: "mast", "post cap", "captain's wheel"; the hull's side as a deck's end.</summary>
        public static string Word(KwAnchorSolid solid)
        {
            if (solid is KwAnchorHull)
                return KwLadderBound.DeckEnd;
            string what = solid.What;
            foreach (string article in Articles)
            {
                if (what.StartsWith(article, System.StringComparison.Ordinal))
                    what = what.Substring(article.Length);
            }
            return what == "fitting" || what.Length == 0 ? KwLadderBound.Obstacle : what;
        }
    }
}
