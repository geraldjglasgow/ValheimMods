using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OpenKeep.Blueprints.Copy
{
    /// <summary>
    /// The local player's Copy selection: loaded player-built pieces with their drawn boxes (<see cref="FixPiece"/>),
    /// from any number of buildings, and a version that changes with every edit so the glow and the HUD set themselves
    /// again. It lives on this machine only, until it is saved or cleared or the local player changes; pieces that are
    /// removed or unload drop out (looked at twice a second).
    /// </summary>
    public static class CopySelection
    {
        private const float LostCheck = 0.5f;

        private static readonly Dictionary<Piece, FixPiece> pieces = new Dictionary<Piece, FixPiece>();
        private static float nextLostCheck;
        private static int buildings;
        private static int buildingsVersion = -1;

        public static int Version { get; private set; }

        public static int Count => pieces.Count;

        public static ICollection<Piece> Pieces => pieces.Keys;

        public static ICollection<FixPiece> Boxes => pieces.Values;

        public static bool Contains(Piece piece) => piece != null && pieces.ContainsKey(piece);

        /// <summary>How many separate buildings it holds: groups of selected pieces joined through touching boxes (worked out once per version).</summary>
        public static int Buildings
        {
            get
            {
                if (buildingsVersion != Version)
                {
                    buildings = CountGroups();
                    buildingsVersion = Version;
                }
                return buildings;
            }
        }

        /// <summary>A group in or out at once: added when any of it is not selected, else let go. The number added (negative: let go).</summary>
        public static int Toggle(List<FixPiece> group)
        {
            List<FixPiece> live = group.FindAll(p => p.Piece != null);
            int added = live.Count(p => !pieces.ContainsKey(p.Piece));
            foreach (FixPiece p in live)
            {
                if (added == 0)
                    pieces.Remove(p.Piece);
                else
                    pieces[p.Piece] = p;
            }
            Version++;
            return added > 0 ? added : -live.Count;
        }

        public static void Clear()
        {
            if (pieces.Count == 0)
                return;
            pieces.Clear();
            Version++;
        }

        /// <summary>Twice a second: pieces removed or unloaded (destroyed here) drop out.</summary>
        public static void DropLost()
        {
            float now = Time.realtimeSinceStartup;
            if (pieces.Count == 0 || now < nextLostCheck)
                return;
            nextLostCheck = now + LostCheck;
            List<Piece> lost = pieces.Keys.Where(p => p == null).ToList();
            foreach (Piece piece in lost)
                pieces.Remove(piece);
            if (lost.Count > 0)
                Version++;
        }

        private static int CountGroups()
        {
            CopyGrid grid = new CopyGrid(pieces.Values);
            HashSet<Piece> seen = new HashSet<Piece>();
            int groups = 0;
            foreach (FixPiece p in pieces.Values)
            {
                if (seen.Contains(p.Piece))
                    continue;
                grid.Flood(p, seen);
                groups++;
            }
            return groups;
        }
    }
}
