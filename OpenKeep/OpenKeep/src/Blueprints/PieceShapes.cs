using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// What a piece prefab looks like without being one: a template of plain mesh renderers (its first level of detail,
    /// active parts only, sharing the game's meshes and materials) for the preview, and its bounds in the prefab's own
    /// space for the ground under it. A template has no network view, collider or script, so a copy of it is only ever
    /// a picture on this machine. Built once per prefab and kept for the game run.
    /// </summary>
    public sealed class PieceShape
    {
        /// <summary>The inactive template to copy, or null when the prefab has nothing to draw.</summary>
        public GameObject Template;

        /// <summary>The drawn bounds in the prefab's space (pivot at the origin, yaw 0).</summary>
        public Bounds Bounds;
    }

    public static class PieceShapes
    {
        private static readonly Dictionary<string, PieceShape> shapes = new Dictionary<string, PieceShape>();
        private static GameObject root;
        private static ZNetScene scene;

        /// <summary>The prefab's shape, or null when the game has no such prefab (a piece of a mod that is not installed).</summary>
        public static PieceShape Of(string prefabName)
        {
            ForgetOtherWorld();
            if (shapes.TryGetValue(prefabName, out PieceShape known))
                return known;
            GameObject prefab = scene != null ? scene.GetPrefab(prefabName) : null;
            PieceShape shape = prefab != null && prefab.GetComponent<Piece>() != null ? Build(prefab) : null;
            shapes[prefabName] = shape;
            return shape;
        }

        private static PieceShape Build(GameObject prefab)
        {
            GameObject template = new GameObject("ok_blueprint_" + prefab.name);
            template.transform.SetParent(Root(), false);
            Matrix4x4 toPrefab = prefab.transform.worldToLocalMatrix;
            bool any = false;
            Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
            foreach (Renderer renderer in Drawn(prefab))
            {
                Mesh mesh = MeshOf(renderer);
                Matrix4x4 m = toPrefab * renderer.transform.localToWorldMatrix;
                AddCopy(template, mesh, renderer.sharedMaterials, m);
                Encapsulate(ref bounds, ref any, mesh.bounds, m);
            }
            if (!any)
                Object.Destroy(template);
            return new PieceShape { Template = any ? template : null, Bounds = bounds };
        }

        /// <summary>A new world has its own prefab list (mods may make theirs again): the shapes are made again from it.</summary>
        private static void ForgetOtherWorld()
        {
            if (ZNetScene.instance == scene)
                return;
            scene = ZNetScene.instance;
            foreach (PieceShape shape in shapes.Values)
            {
                if (shape?.Template != null)
                    Object.Destroy(shape.Template);
            }
            shapes.Clear();
        }

        /// <summary>The renderers a player sees on a fresh piece: active, enabled, meshes, only the first level of detail.</summary>
        private static IEnumerable<Renderer> Drawn(GameObject prefab)
        {
            HashSet<Renderer> lower = LowerDetail(prefab);
            foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.enabled && !lower.Contains(renderer) && MeshOf(renderer) != null && ActiveUpTo(renderer.transform, prefab.transform))
                    yield return renderer;
            }
        }

        /// <summary>Renderers that only belong to the second or a later level of detail.</summary>
        private static HashSet<Renderer> LowerDetail(GameObject prefab)
        {
            HashSet<Renderer> lower = new HashSet<Renderer>();
            HashSet<Renderer> first = new HashSet<Renderer>();
            foreach (LODGroup group in prefab.GetComponentsInChildren<LODGroup>(true))
            {
                LOD[] lods = group.GetLODs();
                for (int i = 0; i < lods.Length; i++)
                    (i == 0 ? first : lower).UnionWith(lods[i].renderers);
            }
            lower.ExceptWith(first);
            return lower;
        }

        private static Mesh MeshOf(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned)
                return skinned.sharedMesh;
            if (!(renderer is MeshRenderer))
                return null;
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            return filter != null ? filter.sharedMesh : null;
        }

        private static bool ActiveUpTo(Transform t, Transform top)
        {
            for (; t != null; t = t.parent)
            {
                if (!t.gameObject.activeSelf)
                    return false;
                if (t == top)
                    return true;
            }
            return true;
        }

        private static void AddCopy(GameObject template, Mesh mesh, Material[] materials, Matrix4x4 m)
        {
            GameObject part = new GameObject("part");
            part.transform.SetParent(template.transform, false);
            part.transform.localPosition = m.GetColumn(3);
            part.transform.localRotation = m.rotation;
            part.transform.localScale = m.lossyScale;
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = part.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        /// <summary>Adds the eight corners of a mesh's bounds, moved into the prefab's space.</summary>
        private static void Encapsulate(ref Bounds bounds, ref bool any, Bounds local, Matrix4x4 m)
        {
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = local.center + Vector3.Scale(local.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 p = m.MultiplyPoint3x4(corner);
                if (!any)
                    bounds = new Bounds(p, Vector3.zero);
                else
                    bounds.Encapsulate(p);
                any = true;
            }
        }

        /// <summary>The inactive parent of every template, kept across scene loads (the game keeps its prefabs too).</summary>
        private static Transform Root()
        {
            if (root == null)
            {
                root = new GameObject("OpenKeep Blueprint Shapes");
                root.SetActive(false);
                Object.DontDestroyOnLoad(root);
            }
            return root.transform;
        }
    }
}
