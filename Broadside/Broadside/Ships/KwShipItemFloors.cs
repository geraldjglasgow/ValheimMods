using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What a dropped item lands on aboard a launched ship (the user, 2026-10-26: "I just dropped an item ... it went
    /// straight through the ship all the way to the sea floor"). The ship's boxes are on the vehicle layer, like the game's
    /// own ships', and the game's physics never lets the item layer meet the vehicle layer, so items fell through every
    /// deck. Each box the ship's colliders were built with (decks, hold floors, roofs, the hull's sides and walls, keels;
    /// not the railings, which break) gets a copy on the item layer under its own holder beside them, built once with them:
    /// the item layer meets items, terrain and pieces but not characters, the camera, the building rays or the shot trace,
    /// so only dropped items feel them. They move with the ship's body and are part of it; a hole shot through later leaves
    /// its copy, so a dropped item does not fall out through a hole. On every peer (an item's physics run on its owner).
    /// </summary>
    public static class KwShipItemFloors
    {
        public const string Name = "item_floors";

        /// <summary>A copy on the item layer of each box directly under the ship's colliders' holder; any earlier copies gone.</summary>
        public static void Add(Transform colliders)
        {
            Transform ship = colliders.parent;
            Transform? old = ship.Find(Name);
            if (old != null)
            {
                old.name = Name + "_gone";
                Object.Destroy(old.gameObject);
            }
            int layer = LayerMask.NameToLayer("item");
            GameObject holder = new GameObject(Name) { layer = layer };
            holder.transform.SetParent(ship, false);
            holder.transform.SetLocalPositionAndRotation(colliders.localPosition, colliders.localRotation);
            foreach (Transform child in colliders)
            {
                if (child.TryGetComponent(out BoxCollider box))
                    Copy(holder.transform, child, box, layer);
            }
        }

        // One box's copy, placed and sized as it is.
        private static void Copy(Transform holder, Transform from, BoxCollider box, int layer)
        {
            GameObject copy = new GameObject(from.name) { layer = layer };
            copy.transform.SetParent(holder, false);
            copy.transform.SetLocalPositionAndRotation(from.localPosition, from.localRotation);
            copy.transform.localScale = from.localScale;
            BoxCollider made = copy.AddComponent<BoxCollider>();
            (made.center, made.size) = (box.center, box.size);
        }
    }
}
