using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Where the sea will stand on the ship on a dry dock once it is launched, for the Doorway and Window tools' note
    /// (<see cref="KwOpeningNotes"/>): worked out once when their window opens, as the dock's waterline is
    /// (<see cref="KwWaterlineLevel.OnDock"/> on the dock's pieces placed in its frame, as <see cref="KwDockWaterline"/>
    /// captures them), then asked for each opening planned: whether its sill's middle is under that line. Local to the
    /// player's game.
    /// </summary>
    public sealed class KwOpeningWater
    {
        private static readonly int SpanHash = KwGenerated.Key.GetStableHashCode();
        private readonly Transform frame;
        private readonly KwWaterline line;

        private KwOpeningWater(Transform frame, KwWaterline line)
        {
            this.frame = frame;
            this.line = line;
        }

        /// <summary>The waterline of the ship on the dock a part stands on; null with no dock, no hull, or one that does not float.</summary>
        public static KwOpeningWater? Of(Piece part)
        {
            KwBpDockInfo? dock = KwBpDock.Find(part.transform.position);
            if (dock == null)
                return null;
            try
            {
                KwBlueprint placed = KwBpStrays.Trim(Capture(dock.Frame, KwBpDock.Ship(dock)));
                return placed.Pieces.Count == 0 ? null : new KwOpeningWater(dock.Frame, KwWaterlineLevel.OnDock(placed));
            }
            catch (Exception e)
            {
                Log.Warn($"Broadside openings: no waterline for the note ({e.Message})");
                return null;
            }
        }

        /// <summary>Whether an opening's sill, at its middle along, would be under the water.</summary>
        public bool Below(KwOpeningSite site, KwOpening o)
        {
            if (frame == null || site.Piece == null)
                return false;
            Vector3 sill = KwOpeningHull.World(site.Piece, site.Surface.Point(o.U, o.Sill, 0f));
            Vector3 local = Quaternion.Inverse(frame.rotation) * (sill - frame.position);
            return local.y < line.At(local.z);
        }

        // The ship's pieces placed in the dock's frame with their spans (all the waterline reads), as the dock's waterline captures them.
        private static KwBlueprint Capture(Transform at, List<Piece> ship)
        {
            Quaternion back = Quaternion.Inverse(at.rotation);
            KwBlueprint placed = new KwBlueprint("");
            foreach (Piece piece in ship)
            {
                string span = piece.m_nview.GetZDO().GetString(SpanHash);
                List<KwBpValue> data = span.Length > 0 ? new List<KwBpValue> { new KwBpValue(SpanHash, span) } : new List<KwBpValue>();
                Transform part = piece.transform;
                placed.Pieces.Add(new KwBpPiece(KwBpDock.NameOf(piece)!, back * (part.position - at.position), back * part.rotation, data));
            }
            return placed;
        }
    }
}
