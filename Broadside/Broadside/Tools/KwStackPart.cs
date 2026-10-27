using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A piece as a cabin stack reads it (<see cref="KwCabinStack"/>, <see cref="KwCabinLift"/>): the piece itself (none in
    /// an offline check), its prefab, where it stands, its level heading and its saved text; read from that text, a cabin's
    /// spec, a roof railing's outline and a ladder's length. Read from the world once round an edit or a removal
    /// (<see cref="Near"/>), so the stack's rules are plain numbers.
    /// </summary>
    public sealed class KwStackPart
    {
        private static readonly string Rail = KwToolPieces.PartPrefix + KwBiomeCost.Rail, Ladder = KwToolPieces.PartPrefix + KwLadderTool.Tool;

        public KwStackPart(Piece? piece, string prefab, Vector3 position, Vector3 forward, string text)
        {
            (Piece, Prefab, Position, Text) = (piece, prefab, position, text);
            Vector3 level = new Vector3(forward.x, 0f, forward.z);
            Forward = level.sqrMagnitude > 1e-8f ? level.normalized : Vector3.forward;
            Cabin = KwHouses.IsPart(prefab) && KwCabinSpec.TryParse(text, out KwCabinSpec spec) ? spec : null;
            RoofRail = prefab == Rail && KwRoofRail.TryParse(text, out KwRoofRail rail) ? rail : null;
            Length = prefab == Ladder && KwLadder.TryLength(text, out float length) ? length : 0f;
        }

        public Piece? Piece { get; }
        public string Prefab { get; }
        public Vector3 Position { get; }
        public Vector3 Forward { get; }
        public string Text { get; }

        /// <summary>A cabin's spec, fitted as it builds; null for any other piece, or a cabin whose text does not read.</summary>
        public KwCabinSpec? Cabin { get; }

        /// <summary>A roof railing's outline and gaps; null for any other piece (a hull railing too).</summary>
        public KwRoofRail? RoofRail { get; }

        /// <summary>A ladder's length; 0 for any other piece.</summary>
        public float Length { get; }

        public bool IsLadder => Length > 0f;

        /// <summary>A cabin where it stands, in its frame (only for a cabin).</summary>
        public KwCabinAt At => new KwCabinAt(Cabin!, new KwStackFrame(Position, Forward));

        /// <summary>Every Broadside ship piece loaded within an undo's reach of a point.</summary>
        public static List<KwStackPart> Near(Vector3 point)
        {
            List<Piece> found = new List<Piece>();
            Piece.GetAllPiecesInRadius(point, KwUndo.Reach, found);
            List<KwStackPart> parts = new List<KwStackPart>();
            foreach (Piece piece in found)
            {
                KwStackPart? part = Of(piece);
                if (part != null)
                    parts.Add(part);
            }
            return parts;
        }

        // A loaded piece as a part, when it is a Broadside ship piece with its ZDO.
        private static KwStackPart? Of(Piece piece)
        {
            ZNetView? view = piece != null ? piece.m_nview : null;
            ZDO? zdo = view != null && view.IsValid() ? view.GetZDO() : null;
            string prefab = zdo != null ? Utils.GetPrefabName(piece!.gameObject) : "";
            if (zdo == null || !KwBlueprintStation.IsShipPiece(prefab))
                return null;
            Transform at = piece!.transform;
            return new KwStackPart(piece, prefab, at.position, at.forward, zdo.GetString(KwGenerated.Key));
        }
    }
}
