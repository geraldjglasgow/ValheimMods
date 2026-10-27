using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What the Cabin tool is aimed at, as the game's placement ray finds it: a deck section (a deck part's top), or a
    /// built cabin's roof (its top, <see cref="KwCabinRoofs.OnRoof"/>), which counts as a deck section to build a cabin on.
    /// A cabin's walls or the underside of its roof are not sections: a click there edits the cabin (<see cref="KwCabinEdit"/>).
    /// </summary>
    public static class KwCabinPick
    {
        private static readonly string Deck = KwToolPieces.PartPrefix + "deck";

        /// <summary>The deck part or cabin roof under the crosshair and the point on it; null off one.</summary>
        public static KwClick? Section(Player player)
        {
            Transform eye = GameCamera.instance.transform;
            if (!Physics.Raycast(eye.position, eye.forward, out RaycastHit hit, 50f, player.m_placeRayMask) || hit.normal.y < 0.7f)
                return null;
            Piece? piece = hit.collider.GetComponentInParent<Piece>();
            return piece != null && Is(piece, hit.point) ? new KwClick(piece, hit.point) : (KwClick?)null;
        }

        /// <summary>Whether a point on a piece is a section's top: any point of a deck part, the top of a cabin's roof.</summary>
        public static bool Is(Piece piece, Vector3 point)
        {
            string name = Utils.GetPrefabName(piece.gameObject);
            return name == Deck || (KwHouses.IsPart(name) && KwCabinRoofs.OnRoof(piece, point));
        }

        /// <summary>Whether a piece is a built cabin (its roof a section), rather than a deck part.</summary>
        public static bool IsCabin(Piece piece) => KwHouses.IsPart(Utils.GetPrefabName(piece.gameObject));
    }
}
