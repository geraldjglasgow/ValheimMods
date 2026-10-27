using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's planking parts and cabins, each drawn on its own (<see cref="KwSeaOpenings.DrawsOwn"/>: left out of
    /// the ship's combined look and its hull chunks), so a door or window cut, changed or filled in at sea draws that one
    /// part again and its ghost can hide just that part (the user, 2026-10-26: "add in a porthole window to put in another
    /// cannon"): a part's look as the ship's look draws it (<see cref="KwBpMeshSet"/>: its strakes with its openings cut, its
    /// painted bands, its hold's floor giving way to the low decks, its openings' frames; a cabin's looks), combined into one
    /// mesh per material, one renderer each under the ship's look, in the ship's frame, its shadow left to the ship's boxes
    /// (<see cref="KwShipShadow"/>), the ship's copies of its materials (<see cref="KwShipMaterials"/>), and its holes cut
    /// into it as into the hull's other looks (<see cref="KwHoleLooks.Own"/>). Only where there is a screen.
    /// </summary>
    public static class KwSeaOpeningLooks
    {
        public const string Holder = "parts";
        private static (KwSeaShip? Ship, int Version, List<(Vector3 At, Vector3 Forward, KwDef Def)> Decks) decks;

        /// <summary>Every planking part and cabin of a ship drawn, once as it is built.</summary>
        public static void Build(KwSeaShip ship)
        {
            if (!KwShipBody.Drawn)
                return;
            List<(Vector3 At, Vector3 Forward, KwDef Def)> decks = Decks(ship);
            for (int i = 0; i < ship.Blueprint.Pieces.Count; i++)
            {
                KwBpPiece piece = ship.Blueprint.Pieces[i];
                if (KwSeaOpeningPart.Takes(piece))
                    Draw(ship, i, piece, decks);
            }
        }

        /// <summary>Parts drawn again (none for one removed); how long it took, in milliseconds.</summary>
        public static double Redraw(KwSeaShip ship, List<(int Index, KwBpPiece? Piece)> parts)
        {
            if (!KwShipBody.Drawn || parts.Count == 0)
                return 0.0;
            Stopwatch watch = Stopwatch.StartNew();
            List<(Vector3 At, Vector3 Forward, KwDef Def)> decks = Decks(ship);
            foreach ((int index, KwBpPiece? piece) in parts)
                Draw(ship, index, piece != null && KwSeaOpeningPart.Takes(piece) ? piece : null, decks);
            return watch.Elapsed.TotalMilliseconds;
        }

        /// <summary>The ship's decks its holds' floors give way to (<see cref="KwSole.Decks"/>), read once each time the ship is drawn anew.</summary>
        public static List<(Vector3 At, Vector3 Forward, KwDef Def)> Decks(KwSeaShip ship)
        {
            if (decks.Ship != ship || decks.Version != ship.Version || decks.Decks == null)
                decks = (ship, ship.Version, KwSole.Decks(ship.Blueprint));
            return decks.Decks;
        }

        /// <summary>The renderers of a part drawn on its own (for the tools' ghost to hide).</summary>
        public static List<Renderer> Renderers(KwSeaShip ship, int index) => KwHoleLooks.Renderers(ship.Visual, index);

        /// <summary>A part's look as the ship draws it: one mesh per material, in the ship's frame.</summary>
        public static List<(Material Material, Mesh Mesh)> Meshes(KwSeaShip ship, KwBpPiece piece, List<(Vector3 At, Vector3 Forward, KwDef Def)> decks)
        {
            KwBpMeshSet set = new KwBpMeshSet { Keels = ship.Keels, Decks = decks };
            set.Add(piece, Vector3.zero);
            return set.Combine("KW_ship_part");
        }

        // One part's renderers made (none for null) and handed to the hull's looks in place of its old ones.
        private static void Draw(KwSeaShip ship, int index, KwBpPiece? piece, List<(Vector3 At, Vector3 Forward, KwDef Def)> decks)
        {
            Transform holder = HolderOf(ship.Visual);
            List<KwHolePart> made = new List<KwHolePart>();
            if (piece != null)
            {
                foreach ((Material material, Mesh mesh) in KwSeaOpeningPrepared.Take(ship, index, piece) ?? Meshes(ship, piece, decks))
                    made.Add(Part(holder, index, material, mesh));
            }
            KwHoleLooks.Own(ship.Visual, index, made);
        }

        // One renderer of a part, its shadow off, in the ship's copy of its material.
        private static KwHolePart Part(Transform holder, int index, Material material, Mesh mesh)
        {
            GameObject look = new GameObject("part" + index) { layer = holder.gameObject.layer };
            look.transform.SetParent(holder, false);
            MeshFilter filter = look.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = look.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            KwShipMaterials.Apply(look.transform);
            return new KwHolePart(filter, mesh);
        }

        // The holder of the parts' renderers under the ship's look (made the first time).
        private static Transform HolderOf(Transform visual)
        {
            Transform? holder = visual.Find(Holder);
            if (holder != null)
                return holder;
            holder = new GameObject(Holder) { layer = visual.gameObject.layer }.transform;
            holder.SetParent(visual, false);
            return holder;
        }
    }
}
