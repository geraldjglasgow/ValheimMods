using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A planking part or a cabin of a launched ship as the Doorway and Window tools work on it where the ship floats
    /// (<see cref="KwSeaOpenings"/>): its index among the ship's blueprint pieces, the piece (its place and turn in the
    /// ship's frame), its tool, saved text and tier; its walls as sites (<see cref="KwOpeningAim.Walls"/>, planking seated on
    /// the ship's keels as its look is, every site's <see cref="KwOpeningSite.Of"/> this part, so a plan tells the parts
    /// apart and knows them for a launched ship's) and its walls with the openings cut in them. Kit points and ways to the
    /// ship's frame and back. Pure.
    /// </summary>
    public sealed class KwSeaOpeningPart
    {
        public KwSeaOpeningPart(int index, KwBpPiece piece, string tool, KwKeelLines keels, int revision = 0)
        {
            (Index, Piece, Tool, Revision) = (index, piece, tool, revision);
            Saved = KwShipParts.Saved(piece);
            Tier = KwBiomeTiers.Of(piece.Data);
            Seat = new KwSeatAt(keels, piece.Position, piece.Rotation * Vector3.forward);
            Walls = KwOpeningAim.Walls(tool, Saved, Tier, null, Seat);
            Walls.ForEach(w => w.Of = this);
            Cut = KwOpeningSurfaces.Of(tool, Saved, Seat);
        }

        public int Index { get; }
        public KwBpPiece Piece { get; }

        /// <summary>The ship's revision it was read at: what an edit planned on it is asked with.</summary>
        public int Revision { get; }
        public string Tool { get; }
        public string Saved { get; }
        public KwBiomeTier Tier { get; }

        /// <summary>Where it stands among the ship's keels (planking is seated on them, as its look and its openings are).</summary>
        public KwSeatAt Seat { get; }

        /// <summary>Its walls as sites at no place yet (<see cref="KwOpeningSite.At"/> puts one at a place).</summary>
        public List<KwOpeningSite> Walls { get; }

        /// <summary>Its walls with the openings cut in them, each fitted to its wall (none: empty).</summary>
        public List<KwOpeningWall> Cut { get; }

        public bool Planking => Tool == "planking";

        /// <summary>A point of the ship's frame in the part's kit axes.</summary>
        public Vector3 Kit(Vector3 ship) => KwMesh.ToKit(KwSeaApply.Back(Piece.Rotation) * (ship - Piece.Position));

        /// <summary>A way of the ship's frame in the part's kit axes.</summary>
        public Vector3 KitWay(Vector3 way) => KwMesh.ToKit(KwSeaApply.Back(Piece.Rotation) * way);

        /// <summary>A kit point of the part in the ship's frame.</summary>
        public Vector3 Ship(Vector3 kit) => Piece.Position + Piece.Rotation * KwMesh.ToUnity(kit);

        /// <summary>A kit way of the part in the ship's frame.</summary>
        public Vector3 ShipWay(Vector3 kit) => Piece.Rotation * KwMesh.ToUnity(kit);

        /// <summary>One of its walls by name; null when it has none so named.</summary>
        public KwOpeningSite? Wall(string wall) => Walls.Find(w => w.Wall == wall);

        /// <summary>The launched ship's part a site is on; null for a part on the dock.</summary>
        public static KwSeaOpeningPart? Of(KwOpeningSite? site) => site?.Of as KwSeaOpeningPart;

        /// <summary>Whether a blueprint piece is one openings are cut in at sea: planking or a cabin (an old castle too).</summary>
        public static bool Takes(KwBpPiece piece) => ToolOf(piece) != null;

        /// <summary>The tool of a piece openings are cut in ("planking", "cabin", "castle"); null for any other.</summary>
        public static string? ToolOf(KwBpPiece piece)
        {
            if (piece.Prefab == KwToolPieces.PartPrefix + "planking")
                return "planking";
            return KwHouses.IsPart(piece.Prefab) ? piece.Prefab.Substring(KwToolPieces.PartPrefix.Length) : null;
        }
    }
}
