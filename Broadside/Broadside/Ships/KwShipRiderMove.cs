using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A rider moved about its ship while it rides (<see cref="KwShipRider"/>). Its owner may write a new pose in the ship's
    /// frame into its ZDO at any time (<see cref="KwShipRider.PosKey"/>, <see cref="KwShipRider.RotKey"/>; the Asset Lab's
    /// ship cannons, hauled about a deck, write theirs ten times a second), as the game's synced transform works: the
    /// owner moves the piece itself and writes where it is, and every other peer, seeing the ZDO change, takes the new
    /// pose as the rider's record and eases the piece there from where it is drawn over <see cref="Span"/> seconds, so a
    /// pose written every tenth of a second reads as steady movement. The ZDO is looked at again only when its data
    /// revision changes: one compare a frame.
    /// </summary>
    public sealed class KwShipRiderMove
    {
        /// <summary>Seconds a peer takes to ease a rider to a pose written on another (a little over a tenth of a second).</summary>
        public const float Span = 0.12f;

        private uint seen;
        private float began = -1f;
        private Vector3 from;
        private Quaternion fromTurn = Quaternion.identity;

        /// <summary>Whether there is anything to do this frame: the ZDO changed since it was last looked at, or the piece is easing.</summary>
        public bool Due(ZDO zdo) => began >= 0f || zdo.DataRevision != seen;

        /// <summary>Whether the ZDO changed since it was last looked at (it is looked at now).</summary>
        public bool Changed(ZDO zdo)
        {
            if (zdo.DataRevision == seen)
                return false;
            seen = zdo.DataRevision;
            return true;
        }

        /// <summary>Easing begins from where the piece is drawn now, under its ship.</summary>
        public void Begin(Transform piece) => (from, fromTurn, began) = (piece.localPosition, piece.localRotation, Time.time);

        /// <summary>One frame's ease toward the record (nothing when not easing), done once <see cref="Span"/> has passed.</summary>
        public void Ease(Transform piece, Vector3 to, Quaternion toTurn)
        {
            if (began < 0f)
                return;
            float t = Mathf.Clamp01((Time.time - began) / Span);
            if (t >= 1f)
                began = -1f;
            piece.SetLocalPositionAndRotation(Vector3.Lerp(from, to, t), Quaternion.Slerp(fromTurn, toTurn, t));
        }
    }
}
