using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Where the tool is aimed and how the area turns there. The aim: the game camera's ray against the world, pieces,
    /// terrain and the water's surface (so an area over the sea sits at the water, not on the sea floor), within 30 m of the
    /// player's eyes and half the area's size more (a 64 m area's middle up to 46 m off). The frame (the area's yaw, folded into a quarter turn): a Broadside dock section's own way
    /// (<see cref="KwPierDeck"/>), else the turn of the building piece aimed at, else of the nearest building piece in the
    /// area (when locking), else the camera's heading to 15 degrees.
    /// </summary>
    public static class KwDecorAim
    {
        private const float Reach = 30f, Ray = 80f, Snap = 15f;
        private static readonly List<Piece> near = new List<Piece>();
        private static int mask;

        private static int Mask => mask != 0 ? mask : mask = KwDecorSurface.RayMask | LayerMask.GetMask("Water");

        /// <summary>The aimed point and the piece there, if any; false when the aim meets nothing within reach.</summary>
        public static bool Find(Player player, float half, out Vector3 point, out Piece? piece)
        {
            (point, piece) = (Vector3.zero, null);
            Transform? eye = GameCamera.instance != null ? GameCamera.instance.transform : null;
            if (eye == null || !Physics.Raycast(eye.position, eye.forward, out RaycastHit hit, Ray, Mask, QueryTriggerInteraction.Ignore))
                return false;
            float reach = Reach + half / 2f;
            if ((hit.point - player.m_eye.position).sqrMagnitude > reach * reach)
                return false;
            (point, piece) = (hit.point, hit.collider.GetComponentInParent<Piece>());
            return true;
        }

        /// <summary>The frame while aiming: the piece aimed at, else the camera's heading.</summary>
        public static float Quick(Piece? piece) => piece != null && Building(piece) ? Of(piece) : Heading();

        /// <summary>
        /// The frame when an area is locked: the piece aimed at, the nearest building piece in the area, the camera; and the
        /// point the area's cells are laid out from (that piece's edge or middle, so its edges fall on the cells' sides; the aim
        /// with no piece).
        /// </summary>
        public static float Full(Vector3 at, Piece? piece, float half, out Vector3 anchor)
        {
            Piece? from = piece != null && Building(piece) ? piece : Nearest(at, half);
            anchor = from != null ? Anchor(from) : at;
            return from != null ? Of(from) : Heading();
        }

        private static Piece? Nearest(Vector3 at, float half)
        {
            Piece.GetAllPiecesInRadius(at, half, near);
            Piece? best = null;
            foreach (Piece other in near)
            {
                if (other != null && Building(other) && (best == null || Flat(other.transform.position - at) < Flat(best.transform.position - at)))
                    best = other;
            }
            near.Clear();
            return best;
        }

        // A dock section's right edge (its sides then lie on the cells' sides at any width in half metres), else the piece's middle.
        private static Vector3 Anchor(Piece piece)
        {
            KwPierDeck? deck = KwPierDeck.Of(piece);
            return deck != null ? deck.Start + deck.Right * (deck.Width / 2f) : piece.transform.position;
        }

        private static bool Building(Piece piece) =>
            piece.GetComponent<WearNTear>() != null && !KwDecorSurface.Decoration(piece) && piece.GetComponent<Ship>() == null;

        private static float Of(Piece piece)
        {
            KwPierDeck? deck = KwPierDeck.Of(piece);
            Vector3 way = deck != null ? deck.Forward : piece.transform.forward;
            return Fold(Mathf.Atan2(way.x, way.z) * Mathf.Rad2Deg);
        }

        private static float Heading()
        {
            Transform? eye = GameCamera.instance != null ? GameCamera.instance.transform : null;
            float yaw = eye != null ? eye.eulerAngles.y : 0f;
            return Fold(Mathf.Round(yaw / Snap) * Snap);
        }

        private static float Fold(float yaw) => Mathf.Repeat(yaw, 90f);

        private static float Flat(Vector3 d) => d.x * d.x + d.z * d.z;
    }
}
