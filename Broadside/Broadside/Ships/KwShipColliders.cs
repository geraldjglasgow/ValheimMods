using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What a ship's crew walks on and bumps into: boxes, since a moving ship's colliders must be convex. Every deck is a
    /// run of thin slabs following its outline from station to station; every planking part's sides and bottom are boxes
    /// lying on its planking's own face, stretch by stretch between its stations (<see cref="KwHullSkin"/>, in pieces round
    /// its openings), its ends closed where it has no neighbour (a flat stern's transom, a stem, <see cref="KwShipTransomWall"/>),
    /// and its hold's floor (<see cref="KwSole"/>) as slabs the same way, left out under the low decks; every keel a beam
    /// under the hull. Built from the blueprint's spans (the shapes the parts are drawn from), in the ship's frame, on the
    /// vehicle layer like the game's own ships' colliders.
    /// </summary>
    public static class KwShipColliders
    {
        /// <summary>The name of every box but a planking part's own and a cabin roof's (<see cref="KwShipEyes"/> looks for it overhead).</summary>
        public const string DeckName = "deck";

        private const float Thick = 0.1f;

        /// <summary>
        /// Every box, made with the holder switched off and switched on once (each box added live to the ship's body cost
        /// about three times as much: a thousand boxes a ship, 2026-10-27), then their copies for dropped items.
        /// </summary>
        public static void Add(Transform holder, KwBlueprint blueprint)
        {
            bool was = holder.gameObject.activeSelf;
            holder.gameObject.SetActive(false);
            try
            {
                Boxes(holder, blueprint);
            }
            finally
            {
                holder.gameObject.SetActive(was);
            }
            KwShipItemFloors.Add(holder);
        }

        private static void Boxes(Transform holder, KwBlueprint blueprint)
        {
            List<(Vector3 At, Vector3 Forward, KwDef Def)> decks = KwSole.Decks(blueprint);
            foreach ((KwBpPiece piece, string tool, KwSpan span) in KwBpHull.Parts(blueprint))
            {
                if (tool == "deck")
                    Deck(holder, piece, span.Def(tool), blueprint);
                if (tool != "planking")
                    continue;
                KwDef def = span.Def(tool);
                KwSeaOpeningBoxes.Keep(holder, blueprint.Pieces.IndexOf(piece), Sides(holder, piece, span));
                Skin(holder, piece, KwShipTransomWall.Add(def), DeckName);
                Floor(holder, piece, span, decks);
            }
            foreach (KwBpPiece piece in blueprint.Pieces)
                Keel(holder, piece);
            KwShipHouses.Colliders(holder, blueprint);
            KwMastTopColliders.Add(holder, blueprint);
        }

        /// <summary>
        /// A planking part's sides and bottom strip, its seated span's, cut round its openings (the boxes its openings change,
        /// made again alone when one is cut at sea, <see cref="KwSeaOpeningBoxes"/>).
        /// </summary>
        public static List<GameObject> Sides(Transform holder, KwBpPiece piece, KwSpan span) =>
            Skin(holder, piece, KwHullSkin.Planking(span.Def("planking"), KwOpeningBoxes.Planking(span)), KwHullSkin.Name);

        /// <summary>Boxes on a part's face (<see cref="KwSkinBox"/>, its kit axes), named, placed as the piece is; the boxes made.</summary>
        public static List<GameObject> Skin(Transform holder, KwBpPiece piece, IReadOnlyList<KwSkinBox> boxes, string name)
        {
            List<GameObject> made = new List<GameObject>();
            foreach (KwSkinBox box in boxes)
                made.Add(Box(holder, piece, box.Centre, box.Size, box.Turn, name));
            return made;
        }

        // A deck's slabs round its hatch openings; a shut hatch's leaves are walked on over an opening.
        private static void Deck(Transform holder, KwBpPiece piece, KwDef def, KwBlueprint blueprint)
        {
            System.Func<float, float> z = KwMesh.DeckHeight(def);
            Slabs(holder, piece, KwShapes.DeckEdge(def.Stations, z, KwShapes.InsideWidth), z, KwShipDeckHoles.For(blueprint, piece, def));
        }

        // A planking part's hold floor, as its look lays it: its slabs, left out under the low decks.
        private static void Floor(Transform holder, KwBpPiece piece, KwSpan span, List<(Vector3 At, Vector3 Forward, KwDef Def)> decks)
        {
            KwDef? floor = KwSole.Def(span, span.Stations(), 0);
            if (floor != null)
                Slabs(holder, piece, floor.Edge!, KwMesh.DeckHeight(floor), KwSole.Gaps(piece.Position, piece.Rotation * Vector3.forward, floor, decks));
        }

        // Slabs under a deck's (or floor's) top, piece by piece round its openings (as its boards are cut), each as wide as
        // its narrower end.
        private static void Slabs(Transform holder, KwBpPiece piece, List<Vector2> edge, System.Func<float, float> z, List<Rect> holes)
        {
            foreach ((float a, float c, Vector2 sideA, Vector2 sideC) in KwDeckHoles.Pieces(edge, holes))
            {
                float lo = Mathf.Max(sideA.x, sideC.x), hi = Mathf.Min(sideA.y, sideC.y), mid = (a + c) / 2f;
                if (c - a < 0.05f || hi - lo < 0.2f)
                    continue;
                Box(holder, piece, new Vector3(mid, (lo + hi) / 2f, z(mid) - Thick / 2f), new Vector3(hi - lo, Thick, c - a), Quaternion.identity);
            }
        }

        // A keel's beam: its length, its own width and depth, under its keel line.
        private static void Keel(Transform holder, KwBpPiece piece)
        {
            if (!KwShipParts.Keel(piece, out float length, out KwKeelBody body))
                return;
            Box(holder, piece, new Vector3(length / 2f, 0f, body.Depth / 2f), new Vector3(2f * body.Half, body.Depth, length), Quaternion.identity);
        }

        /// <summary>A box at a kit point of a piece (its centre), sized in Unity axes (across, up, along), turned on the piece; the box made.</summary>
        public static GameObject Box(Transform holder, KwBpPiece piece, Vector3 kit, Vector3 size, Quaternion turn, string name = DeckName)
        {
            GameObject box = new GameObject(name) { layer = holder.gameObject.layer };
            box.transform.SetParent(holder, false);
            box.transform.SetLocalPositionAndRotation(piece.Position + piece.Rotation * KwMesh.ToUnity(kit), piece.Rotation * turn);
            box.AddComponent<BoxCollider>().size = size;
            return box;
        }
    }
}
