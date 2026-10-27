using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's boxes that its openings cut, kept by the blueprint index of their part (on the ship's colliders
    /// holder, every peer, a dedicated server too): each planking part's sides and bottom strip (<see cref="KwShipColliders.Sides"/>)
    /// and each cabin's raised sides and own walls (<see cref="KwShipHouses.Opened"/>). When a door or window is cut,
    /// changed or filled in at sea, only that part's boxes are made again from its new text, the old ones switched off and
    /// let go, and the ship's hole boxes told which went and which came (<see cref="KwHoleBoxes.Rebox"/>), so shots and the
    /// crew pass through the new opening and the planking closed again stops them.
    /// </summary>
    public sealed class KwSeaOpeningBoxes : MonoBehaviour
    {
        private readonly Dictionary<int, List<GameObject>> kept = new Dictionary<int, List<GameObject>>();

        /// <summary>A part's boxes kept by its index (as the ship's boxes are made).</summary>
        public static void Keep(Transform holder, int index, List<GameObject> made)
        {
            if (index < 0)
                return;
            Of(holder).kept[index] = made;
        }

        private static KwSeaOpeningBoxes Of(Transform holder)
        {
            KwSeaOpeningBoxes? boxes = holder.GetComponent<KwSeaOpeningBoxes>();
            return boxes != null ? boxes : holder.gameObject.AddComponent<KwSeaOpeningBoxes>();
        }

        /// <summary>A part's boxes made again from its piece as it is now (none for one removed); how many went and came.</summary>
        public static (int Gone, int Made) Redraw(KwSeaShip ship, int index, KwBpPiece? piece)
        {
            Transform holder = ship.Colliders;
            KwSeaOpeningBoxes boxes = Of(holder);
            List<GameObject> gone = boxes.kept.TryGetValue(index, out List<GameObject> old) ? old : new List<GameObject>();
            List<GameObject> made = piece != null ? Made(ship, holder, piece) : new List<GameObject>();
            boxes.kept[index] = made;
            KwHoleBoxes.Rebox(ship.transform, Colliders(gone), Colliders(made));
            foreach (GameObject box in gone)
            {
                if (box == null)
                    continue;
                box.SetActive(false);
                Destroy(box);
            }
            return (gone.Count, made.Count);
        }

        // A part's boxes its openings cut, from its saved text: planking seated on the ship's keels as its look is.
        private static List<GameObject> Made(KwSeaShip ship, Transform holder, KwBpPiece piece)
        {
            string saved = KwShipParts.Saved(piece);
            if (piece.Prefab == KwToolPieces.PartPrefix + "planking" && KwSpan.TryParse(saved, out KwSpan span))
                return KwShipColliders.Sides(holder, piece, new KwSeatAt(ship.Keels, piece.Position, piece.Rotation * Vector3.forward).Seated(span));
            if (KwHouses.IsPart(piece.Prefab) && KwCabinSpec.TryParse(saved, out KwCabinSpec spec))
                return KwShipHouses.Opened(holder, piece, spec);
            return new List<GameObject>();
        }

        private static List<BoxCollider> Colliders(List<GameObject> boxes)
        {
            List<BoxCollider> found = new List<BoxCollider>();
            foreach (GameObject box in boxes)
            {
                if (box != null && box.TryGetComponent(out BoxCollider collider))
                    found.Add(collider);
            }
            return found;
        }
    }
}
