using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A saved ship's sails in its hammer entry's look (<see cref="KwBpGhost"/>), so the placement ghost and the entry's icon
    /// show them: every mast's sails fully set and every bowsprit's jibs cut to the blueprint's foremast
    /// (<see cref="KwJibStay.InBlueprint"/>), each cloth still (no wind) in its own material with its pattern
    /// painted on (<see cref="KwSailLooks"/>), its place baked into its mesh, a child of the entry; the entry lets the meshes
    /// go with its look. The spars are in the combined look already; each mast's ropes and their fittings, tied on to the
    /// blueprint's own pieces, are <see cref="KwRigGhost"/>'s. Clients only, as the entries are.
    /// </summary>
    public static class KwSailGhost
    {
        /// <summary>The blueprint's sails and jibs under the entry, placed from the anchor as its look is.</summary>
        public static void Dress(GameObject entry, KwBlueprint blueprint, Vector3 anchor, List<Mesh> meshes)
        {
            try
            {
                foreach (KwBpPiece piece in blueprint.Pieces)
                {
                    Matrix4x4 at = Matrix4x4.TRS(piece.Position - anchor, piece.Rotation, Vector3.one);
                    if (piece.Prefab == KwSailTool.Made)
                    {
                        Mast(entry, at, KwShipParts.Saved(piece), meshes);
                        KwRigGhost.Mast(entry, at, blueprint, piece, meshes);
                    }
                    else if (piece.Prefab == KwBowsprit.Made)
                        Jibs(entry, at, blueprint, piece, meshes);
                }
            }
            catch (Exception e)
            {
                Log.Warn($"Broadside: the sails of the saved ship {blueprint.Name} were left out of its ghost: {e.Message}");
            }
        }

        // A mast's sails, each fully set at its own place on the mast.
        private static void Mast(GameObject entry, Matrix4x4 at, string saved, List<Mesh> meshes)
        {
            if (!KwMastSpec.TryParse(saved, out KwMastSpec mast))
                return;
            foreach (KwSailLayout s in KwSailLayout.Of(mast))
                Cloth(entry, Baked(KwSailCanvas.Cloth(s, 1f), at * Matrix4x4.Translate(KwMesh.ToUnity(s.ClothOrigin))), KwSailLooks.Of(s.Spec), meshes);
        }

        // A bowsprit's jibs, cut to the blueprint's foremast, each set into its shape.
        private static void Jibs(GameObject entry, Matrix4x4 at, KwBlueprint blueprint, KwBpPiece piece, List<Mesh> meshes)
        {
            if (!KwBowsprit.TryParse(KwShipParts.Saved(piece), out KwBowsprit spec) || spec.Jibs == 0)
                return;
            KwJibStay? stay = KwJibStay.InBlueprint(blueprint, piece);
            if (stay == null)
                return;
            foreach (KwJibCut cut in KwJibPlan.Of(spec, stay))
            {
                Mesh cloth = KwSailCanvas.Cloth(KwJibDress.Layout(cut, spec, stay), 1f);
                Cloth(entry, Baked(cloth, at * KwJibDress.ClothAt(cut)), KwJibDress.Look(cut, spec), meshes);
            }
        }

        // One cloth under the entry, its mesh kept with the entry's.
        private static void Cloth(GameObject entry, Mesh mesh, Material? look, List<Mesh> meshes)
        {
            meshes.Add(mesh);
            GameObject part = new GameObject("sail") { layer = entry.layer };
            part.transform.SetParent(entry.transform, false);
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            part.AddComponent<MeshRenderer>().sharedMaterial = look;
        }

        /// <summary>A mesh moved, turned and stretched by a matrix (a jib's shear too) into its holder's frame, its normals made again.</summary>
        public static Mesh Baked(Mesh mesh, Matrix4x4 at)
        {
            Vector3[] vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
                vertices[i] = at.MultiplyPoint3x4(vertices[i]);
            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
