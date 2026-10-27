using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's shadow, cast by plain boxes instead of its drawn pieces. The kit draws its pieces two-sided (every
    /// face twice, back to back), and the game's shadows push a face's caster back along its normal: a back face's caster
    /// comes out over the face in front of it, so a ship shades its own deck and sides in thin bands that flicker as it
    /// rocks. So the drawn ship casts no shadow (it still takes the sails' and the crew's), and a set of boxes casts one in
    /// its place, shadows only: the boxes the crew walks on (decks, sides, keels, <see cref="KwShipColliders"/>) and a box
    /// for each mast and each square sail's yard on it. Made once with the ship's look, on every client.
    /// </summary>
    public static class KwShipShadow
    {
        private static Mesh? cube;

        public static void Add(Transform ship, KwBlueprint blueprint)
        {
            Transform visual = ship.Find(KwShipPrefab.Visual), colliders = ship.Find(KwShipPrefab.Colliders);
            Material? material = visual.GetComponentInChildren<MeshRenderer>()?.sharedMaterial;
            if (material == null)
                return;
            List<CombineInstance> boxes = new List<CombineInstance>();
            Matrix4x4 into = visual.worldToLocalMatrix;
            foreach (BoxCollider box in colliders.GetComponentsInChildren<BoxCollider>())
                boxes.Add(Box(into * box.transform.localToWorldMatrix * Matrix4x4.TRS(box.center, Quaternion.identity, box.size)));
            Masts(blueprint, into * ship.localToWorldMatrix, boxes);
            foreach (MeshRenderer look in visual.GetComponentsInChildren<MeshRenderer>())
                look.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Caster(visual, material, boxes);
        }

        // A box per mast (its height and thickness) and per square sail's yard on it, in the ship's frame.
        private static void Masts(KwBlueprint blueprint, Matrix4x4 frame, List<CombineInstance> boxes)
        {
            foreach (KwBpPiece piece in blueprint.Pieces)
            {
                if (piece.Prefab != KwSailTool.Made || !KwMastSpec.TryParse(KwShipParts.Saved(piece), out KwMastSpec mast))
                    continue;
                Matrix4x4 at = frame * Matrix4x4.TRS(piece.Position, piece.Rotation, Vector3.one);
                float r = KwSailLayout.RadiusOf(mast.Mast);
                boxes.Add(Box(at * Matrix4x4.TRS(new Vector3(0f, mast.Mast / 2f, 0f), Quaternion.identity, new Vector3(2f * r, mast.Mast, 2f * r))));
                foreach (KwSailLayout s in KwSailLayout.Of(mast))
                {
                    if (s.Square)
                        boxes.Add(Box(at * Matrix4x4.TRS(KwMesh.ToUnity(new Vector3(s.YardX, 0f, s.At)), Quaternion.identity, new Vector3(s.Width, 2f * s.Spar, 2f * s.Spar))));
                }
            }
        }

        private static CombineInstance Box(Matrix4x4 at) => new CombineInstance { mesh = Cube(), transform = at };

        // The boxes in one mesh, drawn into the shadows only.
        private static void Caster(Transform visual, Material material, List<CombineInstance> boxes)
        {
            Mesh mesh = new Mesh { name = "KW_ship_shadow", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.CombineMeshes(boxes.ToArray(), true, true);
            GameObject shadow = new GameObject("shadow") { layer = visual.gameObject.layer };
            shadow.transform.SetParent(visual, false);
            shadow.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = shadow.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            shadow.AddComponent<KwMeshOwner>();
        }

        // Unity's own unit cube: one-sided faces, wound outward.
        private static Mesh Cube()
        {
            if (cube != null)
                return cube;
            GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube = primitive.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(primitive);
            return cube;
        }
    }
}
