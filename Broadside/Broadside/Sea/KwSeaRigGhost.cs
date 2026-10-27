using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The rig's ghosts on a launched ship, under its visual holder so they ride the moving ship: a mast's spars and a
    /// bowsprit's spar see-through as the dock's tools show them (<see cref="KwSeaGhost"/>), with the sails' cloths, the
    /// mast's ropes and fittings tied on to the ship as it would be with that mast, and the jibs on their stay, in their
    /// patterned looks (<see cref="KwSailPreview.ShowOn"/>); a post cap solid in place. Each made again only when what it
    /// shows changes. Local to this screen; nothing on a dedicated server.
    /// </summary>
    public static class KwSeaRigGhost
    {
        public static void Mast(KwSeaShip ship, KwBpPiece piece, int index)
        {
            string text = KwSeaPiece.Text(piece), key = "rigmast|" + index + "|" + text + "|" + Place(piece);
            KwSeaGhost.Show(ship, key, new[] { piece }, false);
            if (KwShipBody.Drawn && KwMastSpec.TryParse(text, out KwMastSpec mast))
                KwSailPreview.ShowOn(ship.Visual, key, piece.Position, piece.Rotation, () => Cloths(ship, piece, index, mast));
        }

        public static void Sprit(KwSeaShip ship, KwBpPiece piece, KwBowsprit spec, KwJibStay? stay)
        {
            string key = "rigsprit|" + KwSeaPiece.Text(piece) + "|" + (stay != null ? stay.Key : "") + "|" + Place(piece);
            KwSeaGhost.Show(ship, key, new[] { piece }, false);
            if (KwShipBody.Drawn)
                KwSailPreview.ShowOn(ship.Visual, key, piece.Position, piece.Rotation, () => KwBowspritTool.Jibs(spec, stay));
        }

        public static void Cap(KwSeaShip ship, KwBpPiece piece, bool solid) =>
            KwSeaGhost.Show(ship, "rigcap|" + KwSeaPiece.Text(piece) + "|" + KwBiomeTiers.Of(piece.Data).Id + "|" + Place(piece), new[] { piece }, solid);

        public static void Clear()
        {
            KwSeaGhost.Clear();
            KwSailPreview.Hide();
        }

        /// <summary>The tool no longer aims at the ship: the shared ghost goes by itself when not shown again (<see cref="KwSeaGhost"/>).</summary>
        public static void Unhover()
        {
        }

        private static string Place(KwBpPiece piece) => piece.Position.ToString("F3") + piece.Rotation.ToString("F4");

        // Every sail's cloth set full where it hangs, and the mast's ropes and fittings tied on to the ship with this mast in
        // it (in place of the one it changes, or added).
        private static List<(Mesh Mesh, Material? Look, Matrix4x4 At)> Cloths(KwSeaShip ship, KwBpPiece piece, int index, KwMastSpec mast)
        {
            List<KwSailLayout> sails = KwSailLayout.Of(mast);
            List<(Mesh Mesh, Material? Look, Matrix4x4 At)> made = sails.ConvertAll(s => (KwSailCanvas.Cloth(s, 1f), KwSailLooks.Of(s.Spec), Matrix4x4.Translate(KwMesh.ToUnity(s.ClothOrigin))));
            List<KwBpPiece> pieces = new List<KwBpPiece>(ship.Blueprint.Pieces);
            if (index >= 0 && index < pieces.Count)
                pieces[index] = piece;
            else
                pieces.Add(piece);
            KwRigPlan plan = new KwRigPlan(new KwRigShip(pieces, piece), sails);
            foreach ((Mesh mesh, Material? look) in KwRigGhost.Meshes(plan, sails))
                made.Add((mesh, look, Matrix4x4.identity));
            return made;
        }
    }
}
