using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// One patch over a hole, drawn (<see cref="KwHullPatchLooks"/>): a holder on the hull's outside at the hole's middle,
    /// <see cref="Stand"/> off the face, turned square to it (its z out of the face, its y up the face, as the hole's frame,
    /// <see cref="KwHoleShape"/>), wide enough to cover the hole's furthest corner at the radius it was patched at and
    /// <see cref="Overlap"/> more. It wears the workshop's patch when the mod embeds the repair kit's bundle
    /// (<see cref="KwRepairKitLook.PatchModel"/>: the model's thinnest way across taken as its way out of the hull, its
    /// larger way across scaled to the patch's width, its back laid on the face), else the generated boards
    /// (<see cref="KwHullPatchMesh"/>) in the plank wood; either way in the ship's material copies (<see cref="KwShipMaterials"/>).
    /// No collider.
    /// </summary>
    public static class KwHullPatchBoard
    {
        public const float Stand = 0.012f, Overlap = 0.07f;

        public static GameObject Make(Transform visual, KwHullHole hole, float covering)
        {
            KwHoleShape shape = KwHoleShape.Of(hole);
            float reach = KwHullPatchText.Reach(hole, covering, Overlap);
            GameObject board = new GameObject("hull_patch") { layer = visual.gameObject.layer };
            board.transform.SetParent(visual, false);
            board.transform.SetLocalPositionAndRotation(shape.Centre + shape.Normal * Stand, Quaternion.LookRotation(shape.Normal, shape.Up));
            GameObject? model = KwRepairKitLook.PatchModel();
            if (model == null || !Workshop(board.transform, model, reach))
                Generated(board.transform, reach, hole.Id);
            KwShipMaterials.Apply(board.transform);
            return board;
        }

        private static void Generated(Transform board, float reach, int seed)
        {
            GameObject look = new GameObject("look") { layer = board.gameObject.layer };
            look.transform.SetParent(board, false);
            look.AddComponent<MeshFilter>().sharedMesh = KwHullPatchMesh.Boards(reach, seed);
            MeshRenderer renderer = look.AddComponent<MeshRenderer>();
            if (KwPrefabs.Planks != null)
                renderer.sharedMaterial = KwPrefabs.Planks;
        }

        // The workshop's patch laid on the face and sized to the hole; false when it draws nothing.
        private static bool Workshop(Transform board, GameObject model, float reach)
        {
            Bounds? bounds = Measure(model);
            if (!bounds.HasValue)
                return false;
            Quaternion turn = Out(bounds.Value.size);
            Bounds laid = Turned(bounds.Value, turn);
            float scale = 2f * reach / Mathf.Max(0.01f, Mathf.Max(laid.size.x, laid.size.y));
            GameObject worn = Object.Instantiate(model, board, false);
            worn.name = "look";
            foreach (Transform t in worn.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = board.gameObject.layer;
            worn.transform.localRotation = turn;
            worn.transform.localScale = Vector3.one * scale;
            worn.transform.localPosition = -new Vector3(laid.center.x, laid.center.y, laid.min.z) * scale;
            if (KwPrefabs.Planks != null)
                KwRepairKitLook.Dress(worn, KwPrefabs.Planks);
            return true;
        }

        /// <summary>A model's meshes' bounds in its own frame (its children's poses taken in); null with none.</summary>
        public static Bounds? Measure(GameObject model)
        {
            (Bounds all, bool any) = (default, false);
            Matrix4x4 into = model.transform.worldToLocalMatrix;
            foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null)
                    continue;
                Matrix4x4 m = into * filter.transform.localToWorldMatrix;
                foreach (Vector3 corner in Corners(filter.sharedMesh.bounds))
                {
                    Vector3 p = m.MultiplyPoint3x4(corner);
                    if (any)
                        all.Encapsulate(p);
                    else
                        (all, any) = (new Bounds(p, Vector3.zero), true);
                }
            }
            return any ? all : (Bounds?)null;
        }

        /// <summary>The turn that lays a model's thinnest way across (taken as facing its positive way) out of the face, +z.</summary>
        public static Quaternion Out(Vector3 size)
        {
            if (size.z <= size.x && size.z <= size.y)
                return Quaternion.identity;
            return size.y <= size.x ? Quaternion.FromToRotation(Vector3.up, Vector3.forward) : Quaternion.FromToRotation(Vector3.right, Vector3.forward);
        }

        private static Bounds Turned(Bounds bounds, Quaternion turn)
        {
            Bounds turned = new Bounds(turn * bounds.center, Vector3.zero);
            foreach (Vector3 corner in Corners(bounds))
                turned.Encapsulate(turn * corner);
            return turned;
        }

        private static IEnumerable<Vector3> Corners(Bounds b)
        {
            for (int i = 0; i < 8; i++)
                yield return new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
        }
    }
}
