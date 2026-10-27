using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A rudder's steering gear on the dock, measured by the rudder's owner (<see cref="KwRudderBlade"/>): the ship's pieces
    /// loaded round the rudder (within <see cref="Around"/>) read as blueprint pieces in the rudder's own frame, the hull
    /// parts' saved texts with them, and the gear laid out from them as a launched ship's would be
    /// (<see cref="KwRudderGearFind"/>). Without a captain's wheel among them nothing more is read. The pieces' signature
    /// (<see cref="Signature"/>) tells the owner when to measure again: a wheel, deck, planking, hatch, ladder, mast, keel,
    /// cabin or anchor windlass placed, removed (an edit too: its part is spawned again, as a doorway or window cut is) or
    /// loaded near, or a cabin's saved text changed in place.
    /// </summary>
    public static class KwRudderGearDock
    {
        private const float Around = 32f;
        private static readonly List<Piece> Near = new List<Piece>();

        /// <summary>The gear for a rudder on the dock (null with no wheel near) and, when it is linked, the rudder's lay for it.</summary>
        public static KwRudderGear? Measure(Transform rudder, KwRudderTier tier, KwRudderFit fit, out KwRudderLay lay)
        {
            lay = KwRudderLay.Plain;
            Near.Clear();
            Piece.GetAllPiecesInRadius(rudder.position, Around, Near);
            bool wheel = Near.Exists(p => KwWheel.Is(KwBpDock.NameOf(p) ?? ""));
            List<KwBpPiece> pieces = wheel ? Pieces(rudder) : new List<KwBpPiece>();
            Near.Clear();
            if (!wheel)
                return null;
            KwBpPiece self = new KwBpPiece("", Vector3.zero, Quaternion.identity, new List<KwBpValue>());
            return KwRudderGearFind.Find(new KwGearShip(pieces, self), tier, fit, out lay);
        }

        /// <summary>A number that changes when the pieces the gear is laid out from change round a rudder (cheap: their ids).</summary>
        public static int Signature(Transform rudder)
        {
            Near.Clear();
            Piece.GetAllPiecesInRadius(rudder.position, Around, Near);
            int sum = 17, mix = 0, count = 0;
            foreach (Piece piece in Near)
            {
                ZDO? zdo = piece.m_nview != null ? piece.m_nview.GetZDO() : null;
                string? prefab = KwBpDock.NameOf(piece);
                if (zdo == null || prefab == null || !Wanted(prefab))
                    continue;
                int id = unchecked(zdo.m_uid.GetHashCode() * 7 + (KwHouses.IsPart(prefab) ? zdo.GetString(KwGenerated.Key).GetHashCode() : 0));
                (sum, mix, count) = (unchecked(sum + id), mix ^ unchecked(id * 31), count + 1);
            }
            Near.Clear();
            return unchecked(sum * 397 ^ mix * 13 ^ count);
        }

        // The ship pieces near that the gear looks at, in the rudder's frame: the hull parts with their saved texts, the
        // hatches and the wheels.
        private static List<KwBpPiece> Pieces(Transform rudder)
        {
            Quaternion back = Quaternion.Inverse(rudder.rotation);
            List<KwBpPiece> pieces = new List<KwBpPiece>(Near.Count);
            foreach (Piece piece in Near)
            {
                string? prefab = KwBpDock.NameOf(piece);
                if (prefab == null || !Wanted(prefab))
                    continue;
                Transform at = piece.transform;
                List<KwBpValue> data = prefab.StartsWith(KwToolPieces.PartPrefix, System.StringComparison.Ordinal)
                    ? KwBpData.Capture(piece.m_nview.GetZDO()) : new List<KwBpValue>();
                pieces.Add(new KwBpPiece(prefab, back * (at.position - rudder.position), back * at.rotation, data));
            }
            return pieces;
        }

        private static bool Wanted(string prefab) =>
            KwWheel.Is(prefab) || prefab.StartsWith(KwHatch.Prefix, System.StringComparison.Ordinal)
            || prefab == KwToolPieces.PartPrefix + "deck" || prefab == KwToolPieces.PartPrefix + "planking" || prefab == KwKeel.MadeName
            || prefab == KwSailTool.Made || prefab == KwToolPieces.PartPrefix + KwLadderTool.Tool || KwHouses.IsPart(prefab) || KwAnchor.Is(prefab);
    }
}
