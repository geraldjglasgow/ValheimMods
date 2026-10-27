using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A mast's ropes and the fittings they are made fast to, as meshes where nothing moves: in a saved ship's hammer ghost
    /// (<see cref="KwSailGhost"/>, tied on to the blueprint's own pieces) and in the sail tool's see-through preview
    /// (<see cref="KwSailPreview"/>, tied on to the dock's pieces round the place the mast would stand), its sails fully set
    /// (<see cref="KwRigRopes.All"/>), each rope a thin tube in the game's rope. Clients only, as the ghosts and previews are.
    /// </summary>
    public static class KwRigGhost
    {
        /// <summary>A blueprint mast's ropes and fittings under a hammer entry, placed from its anchor as its look is.</summary>
        public static void Mast(GameObject entry, Matrix4x4 at, KwBlueprint blueprint, KwBpPiece piece, List<Mesh> meshes)
        {
            try
            {
                if (!KwMastSpec.TryParse(KwShipParts.Saved(piece), out KwMastSpec mast))
                    return;
                List<KwSailLayout> sails = KwSailLayout.Of(mast);
                foreach ((Mesh mesh, Material? look) in Meshes(new KwRigPlan(new KwRigShip(blueprint.Pieces, piece), sails), sails))
                    Part(entry, KwSailGhost.Baked(mesh, at), look, meshes);
            }
            catch (Exception e)
            {
                Log.Warn($"Broadside: a mast's ropes were left out of the saved ship {blueprint.Name}'s ghost: {e.Message}");
            }
        }

        /// <summary>The sail tool's preview's cloths with the mast's ropes and fittings added, tied on to the dock round the place.</summary>
        public static List<(Mesh Mesh, Material? Look, Matrix4x4 At)> Preview(List<(Mesh Mesh, Material? Look, Matrix4x4 At)> cloths, KwMastSpec mast, Vector3 at, Quaternion turn)
        {
            try
            {
                List<KwSailLayout> sails = KwSailLayout.Of(mast);
                KwRigPlan plan = new KwRigPlan(KwShipBody.Drawn ? KwRigSource.Dock(at, turn) : null, sails);
                foreach ((Mesh mesh, Material? look) in Meshes(plan, sails))
                    cloths.Add((mesh, look, Matrix4x4.identity));
            }
            catch (Exception e)
            {
                Log.Warn($"Broadside: the mast's ropes were left out of its preview: {e.Message}");
            }
            return cloths;
        }

        /// <summary>Every rope as tubes in the game's rope, and the fittings' meshes (the mast's kit placed in Unity axes).</summary>
        public static List<(Mesh Mesh, Material? Look)> Meshes(KwRigPlan plan, List<KwSailLayout> sails)
        {
            MeshBuilder tubes = new MeshBuilder(Vector3.zero);
            foreach (KwRigRope rope in KwRigRopes.All(plan, sails))
                KwTieShapes.Tube(tubes, rope.Points, rope.Width);
            List<(Mesh Mesh, Material? Look)> meshes = KwTieLook.Meshes(plan.Ties);
            meshes.Add((tubes.ToMesh("kw_ropes"), KwSailCloth.Rope));
            return meshes;
        }

        // One mesh under the entry, kept with the entry's meshes.
        private static void Part(GameObject entry, Mesh mesh, Material? look, List<Mesh> meshes)
        {
            meshes.Add(mesh);
            GameObject part = new GameObject("rigging") { layer = entry.layer };
            part.transform.SetParent(entry.transform, false);
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            part.AddComponent<MeshRenderer>().sharedMaterial = look;
        }
    }
}
