using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OpenKeep.Blueprints.Copy
{
    /// <summary>
    /// The building around a piece: <see cref="GroundFixBuilding.WithInside"/> (every player-built piece joined to it
    /// through touching drawn boxes, and everything standing inside it), copyable pieces only. An answer is kept for every piece in it while it is used, so sweeping
    /// the crosshair over one building looks it up once. Looking up a large compound takes milliseconds, so the hover
    /// looks up a new building at most every few tenths of a second (longer when the last took long); a click looks it
    /// up at once.
    /// </summary>
    public static class CopyBuildings
    {
        /// <summary>An answer unused this long is dropped (a building changed by others is then looked up again), seconds.</summary>
        private const float KeepFor = 5f;
        private const int MostKept = 8;

        /// <summary>One looked-up building: its pieces, the same as a set, and when it was last used.</summary>
        private sealed class Found
        {
            public List<FixPiece> Pieces;
            public HashSet<Piece> Members;
            public float Used;
        }

        private static readonly List<Found> found = new List<Found>();
        private static float nextLookup;

        /// <summary>The building of the piece; null when it is not known yet and may not be looked up this frame (<paramref name="now"/> false).</summary>
        public static List<FixPiece> Of(Piece piece, bool now)
        {
            float time = Time.realtimeSinceStartup;
            found.RemoveAll(f => time > f.Used + KeepFor);
            Found known = found.Find(f => f.Members.Contains(piece));
            if (known == null && !now && time < nextLookup)
                return null;
            if (known == null)
                known = LookUp(piece, time);
            known.Used = time;
            return known.Pieces;
        }

        public static void Clear() => found.Clear();

        private static Found LookUp(Piece piece, float time)
        {
            List<FixPiece> pieces = GroundFixBuilding.WithInside(piece).FindAll(p => BlueprintCapture.Copyable(p.Piece));
            Found made = new Found { Pieces = pieces, Members = new HashSet<Piece>(pieces.Select(p => p.Piece)) };
            found.Insert(0, made);
            if (found.Count > MostKept)
                found.RemoveAt(found.Count - 1);
            float cost = Time.realtimeSinceStartup - time;
            nextLookup = Time.realtimeSinceStartup + Mathf.Clamp(cost * 10f, 0.1f, 1f);
            return made;
        }
    }
}
