using System.Collections.Generic;
using UnityEngine;

namespace EarthWright.Menu
{
    /// <summary>
    /// The game's own hoe and cultivator pieces by prefab name, recorded from the tools' build tables every time the
    /// object database is ready and before EarthWright changes those tables. They are the templates EarthWright's
    /// entries are cloned from, and they stay findable after an entry toggle has taken them out of a table.
    /// </summary>
    public static class GamePieces
    {
        private static readonly Dictionary<string, GameObject> pieces = new Dictionary<string, GameObject>();

        /// <summary>Records every piece of the table that is not one of EarthWright's own.</summary>
        public static void Capture(PieceTable table)
        {
            if (table == null)
                return;
            foreach (GameObject piece in table.m_pieces)
            {
                if (piece != null && !EntryRegistry.IsOurs(piece.name) && piece.GetComponent<Piece>() != null)
                    pieces[piece.name] = piece;
            }
        }

        /// <summary>The game's piece prefab with this name, or null.</summary>
        public static GameObject Get(string name)
        {
            return name != null && pieces.TryGetValue(name, out GameObject piece) && piece != null ? piece : null;
        }

        public static Piece PieceOf(string name)
        {
            GameObject prefab = Get(name);
            return prefab != null ? prefab.GetComponent<Piece>() : null;
        }
    }
}
