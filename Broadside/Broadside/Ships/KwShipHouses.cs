using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's cabins beyond their look (which is in the ship's, <see cref="KwBpMeshSet"/>): what the crew walks
    /// on and bumps into, as boxes like the rest of the ship's (<see cref="KwShipColliders"/>): each cabin's roof in slabs
    /// along its outline, named <see cref="RoofBox"/> (the view goes to the eyes under a low one, <see cref="KwShipEyes"/>);
    /// the hull's raised sides on each side at the hull, on their strakes' face (<see cref="KwHullSkin"/>, in pieces round
    /// their openings) and its ends the hull closes (<see cref="KwShipTransomWall"/>); and each of its own walls, cut from the wall's outline as its look
    /// is (<see cref="KwCabinWall.Boxes"/>), so an opening in a wall would leave its boxes open too. The corner posts are
    /// drawn only. From the blueprint's saved texts, in the ship's frame, on every peer, a dedicated server included.
    /// </summary>
    public static class KwShipHouses
    {
        /// <summary>The name of a cabin roof's boxes.</summary>
        public const string RoofBox = "cabin_roof";

        private const float Slab = 0.1f;

        public static void Colliders(Transform holder, KwBlueprint blueprint)
        {
            for (int i = 0; i < blueprint.Pieces.Count; i++)
            {
                KwBpPiece piece = blueprint.Pieces[i];
                if (KwHouses.IsPart(piece.Prefab) && KwCabinSpec.TryParse(KwShipParts.Saved(piece), out KwCabinSpec spec))
                    Cabin(holder, i, piece, spec);
            }
        }

        // A cabin: its roof's slabs, its raised sides, its walls (the sides and walls kept by its index: its openings change them).
        private static void Cabin(Transform holder, int index, KwBpPiece piece, KwCabinSpec spec)
        {
            List<(float X, float Left, float Right)> roof = KwCabinOutline.Roof(spec);
            foreach ((float a, float c, Vector2 sideA, Vector2 sideC) in KwCabinRoof.Pieces(roof))
            {
                float lo = Mathf.Max(sideA.x, sideC.x), hi = Mathf.Min(sideA.y, sideC.y);
                if (c - a >= 0.05f && hi - lo >= 0.2f)
                    Box(holder, piece, RoofBox, new Vector3((a + c) / 2f, (lo + hi) / 2f, spec.Top - Slab / 2f), new Vector3(hi - lo, Slab, c - a), Quaternion.identity);
            }
            KwShipColliders.Skin(holder, piece, KwShipTransomWall.Raised(spec), KwShipColliders.DeckName);
            KwSeaOpeningBoxes.Keep(holder, index, Opened(holder, piece, spec));
        }

        /// <summary>
        /// A cabin's boxes its openings cut: its raised sides on their strakes' face and its own walls, each cut round its
        /// openings (made again alone when one is cut at sea, <see cref="KwSeaOpeningBoxes"/>); the boxes made.
        /// </summary>
        public static List<GameObject> Opened(Transform holder, KwBpPiece piece, KwCabinSpec spec)
        {
            List<GameObject> made = KwShipColliders.Skin(holder, piece, KwHullSkin.Raised(spec), KwShipColliders.DeckName);
            foreach (KwCabinWall wall in KwCabinWalls.Of(spec))
                Wall(holder, piece, wall, made);
            return made;
        }

        // A wall's boxes: each a straight stretch of it over a run of rows, its thickness inward from its outer face.
        private static void Wall(Transform holder, KwBpPiece piece, KwCabinWall wall, List<GameObject> made)
        {
            foreach ((float u0, float u1, float z0, float z1, int segment) in wall.Boxes())
            {
                (Vector2 along, Vector2 _) = wall.Way(segment);
                Vector3 mid = wall.At((u0 + u1) / 2f, (z0 + z1) / 2f, KwCabinWall.Thick / 2f);
                Quaternion turn = Quaternion.LookRotation(KwMesh.ToUnity(new Vector3(along.x, along.y, 0f)), Vector3.up);
                made.Add(Box(holder, piece, "deck", mid, new Vector3(KwCabinWall.Thick, z1 - z0, u1 - u0), turn));
            }
        }

        // A box at a kit point of a piece (its centre), sized in its own Unity axes (across, up, along), turned on the piece.
        private static GameObject Box(Transform holder, KwBpPiece piece, string name, Vector3 kit, Vector3 size, Quaternion turn)
        {
            GameObject box = new GameObject(name) { layer = holder.gameObject.layer };
            box.transform.SetParent(holder, false);
            box.transform.SetLocalPositionAndRotation(piece.Position + piece.Rotation * KwMesh.ToUnity(kit), piece.Rotation * turn);
            box.AddComponent<BoxCollider>().size = size;
            return box;
        }
    }
}
