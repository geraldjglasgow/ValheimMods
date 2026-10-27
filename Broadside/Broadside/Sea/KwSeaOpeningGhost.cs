using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The Doorway and Window tools' ghost on a launched ship (<see cref="KwOpeningGhost"/> for the dock's): a part a plan
    /// changes stood in for under the ship's look, so it rides the moving ship, by the part as it would be drawn there
    /// (<see cref="KwSeaOpeningLooks.Meshes"/>: its strakes with the openings cut, its bands, its floor, every frame) and the
    /// leaves of the openings shown (the new ones; all of them where one is changed or filled in, the part's own leaves then
    /// hidden), shut, part open in an edit window or swung open for a placing. The part's own look (drawn on its own,
    /// <see cref="KwSeaOpeningLooks"/>) is hidden on this screen meanwhile and shown again when the ghost goes. Only drawn,
    /// local to this player.
    /// </summary>
    public static class KwSeaOpeningGhost
    {
        private static readonly List<(KwSeaShip Ship, int Index, KwBpPiece Piece, bool All)> standing = new List<(KwSeaShip, int, KwBpPiece, bool)>();

        /// <summary>A part of the tools' launched ship stood in for by its new text, its own look hidden through the ghost's own hiding.</summary>
        public static void Stand(KwPartCut cut, KwSeaOpeningPart part, bool all, float open, List<GameObject> stood, Action<Renderer?> off)
        {
            KwSeaShip? ship = KwOpeningReads.Sea?.Ship;
            string? text = cut.Site.Text(cut.Openings);
            if (ship == null || text == null || !KwShipBody.Drawn)
                return;
            GameObject root = new GameObject("KW_opening_ghost") { layer = ship.Visual.gameObject.layer };
            root.transform.SetParent(ship.Visual, false);
            stood.Add(root);
            KwBpPiece piece = KwSeaPiece.With(part.Piece, text, null);
            Body(root.transform, ship, part.Index, piece);
            string? leaves = all ? text : cut.Site.Text(cut.Shown.ConvertAll(s => s.Opening));
            if (leaves != null)
                Leaves(root.transform, part, cut.Site, leaves, open);
            standing.Add((ship, part.Index, piece, all));
            Conceal(ship, part.Index, all, off);
        }

        /// <summary>Every part stood in for hidden again (a redraw may have made it anew); false when its ship is gone.</summary>
        public static bool Keep(Action<Renderer?> off)
        {
            foreach ((KwSeaShip ship, int index, KwBpPiece piece, bool all) in standing)
            {
                if (ship == null)
                    return false;
                Conceal(ship, index, all, off);
                KwSeaOpeningPrepared.Touch(ship, index, piece);
            }
            return true;
        }

        /// <summary>The ghost gone (its objects and the hiding are <see cref="KwOpeningGhost"/>'s).</summary>
        public static void Forget() => standing.Clear();

        // The part as the ship would draw it, in the ship's frame; its meshes kept for the redraw (KwSeaOpeningPrepared), not
        // by its objects.
        private static void Body(Transform root, KwSeaShip ship, int index, KwBpPiece piece)
        {
            foreach ((Material material, Mesh mesh) in KwSeaOpeningPrepared.For(ship, index, piece))
            {
                GameObject look = new GameObject("body") { layer = root.gameObject.layer };
                look.transform.SetParent(root, false);
                look.AddComponent<MeshFilter>().sharedMesh = mesh;
                MeshRenderer renderer = look.AddComponent<MeshRenderer>();
                (renderer.sharedMaterial, renderer.shadowCastingMode) = (material, UnityEngine.Rendering.ShadowCastingMode.Off);
            }
            KwShipMaterials.Apply(root);
        }

        // The leaves shown, under a holder placed as the part is, seated where the part is.
        private static void Leaves(Transform root, KwSeaOpeningPart part, KwOpeningSite site, string text, float open)
        {
            Transform at = new GameObject("leaves") { layer = root.gameObject.layer }.transform;
            at.SetParent(root, false);
            at.SetLocalPositionAndRotation(part.Piece.Position, part.Piece.Rotation);
            KwOpeningGhostLooks.Leaves(at, site, text, open, part.Seat);
        }

        // The part's own look hidden, and its own leaves too where the ghost draws all of them.
        private static void Conceal(KwSeaShip ship, int index, bool all, Action<Renderer?> off)
        {
            KwSeaOpeningLooks.Renderers(ship, index).ForEach(off);
            Transform? leaves = all ? ship.Fittings.Find("openings" + index) : null;
            if (leaves != null)
            {
                foreach (Renderer renderer in leaves.GetComponentsInChildren<Renderer>())
                    off(renderer);
            }
        }
    }
}
