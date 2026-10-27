using System;
using System.Collections.Generic;
using BundlePrefabs;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The carved heads' models from the embedded bundle <c>kw_postheads</c> (ValheimAssets
    /// <c>Assets/Ships/Keelwright/PostHeads</c>, embedded as <c>Broadside.assets.kw_postheads.&lt;platform&gt;</c>): prefabs
    /// <c>kw_head_dragon</c>, <c>_serpent</c>, <c>_wolf</c>, <c>_raven</c> and <c>_horse</c>, each with its mesh under
    /// <c>model</c> on the one shared material <c>kw_postheads</c> (a 256 px atlas and its normal map) and an empty
    /// <c>head_start</c> at the neck's top; the root at the foot's middle, +y up, looking along +z, the foot 0.40 by 0.30 m.
    /// Read once, the first time a head is drawn, into plain data (<see cref="KwCapHead"/>); the bundle's material is what
    /// the heads' looks are dressed from (<see cref="KwHeadLooks"/>). Only where there is a screen: a dedicated server
    /// loads nothing, and a head there (or with the bundle missing, logged once) is drawn by the code as a scroll.
    /// </summary>
    public static class KwCapHeads
    {
        public const string Prefix = "kw_head_";
        private const string Bundle = "kw_postheads", Start = "head_start";
        private static readonly Dictionary<string, KwCapHead> heads = new Dictionary<string, KwCapHead>();
        private static bool tried;

        /// <summary>The bundle's shared material, the heads' looks' source; null until a head is loaded, or with none.</summary>
        public static Material? Material { get; private set; }

        /// <summary>A style's carved head; null for a style the code draws, on a dedicated server, or without the bundle.</summary>
        public static KwCapHead? Of(string style)
        {
            string? prefab = KwCaps.Find(style)?.Head;
            if (prefab == null || !KwShipBody.Drawn)
                return null;
            Load();
            return heads.TryGetValue(prefab, out KwCapHead head) ? head : null;
        }

        /// <summary>
        /// A piece's model wearing a carved head at the standard post (an old hammer dragon, the Post cap tool's placeholder):
        /// the "model" child with its mesh, collider and Meadows look; null where the head is not loaded (the code's shape then).
        /// </summary>
        public static GameObject? Model(GameObject piece, KwCapSpec spec)
        {
            Mesh? mesh = KwCapMesh.Head(spec);
            if (mesh == null)
                return null;
            GameObject model = new GameObject("model") { layer = piece.layer };
            model.transform.SetParent(piece.transform, false);
            model.AddComponent<MeshFilter>().sharedMesh = mesh;
            model.AddComponent<MeshRenderer>().sharedMaterial = KwHeadLooks.For(KwBiomeTiers.Meadows);
            model.AddComponent<MeshCollider>().sharedMesh = mesh;
            KwPrefabs.Anchor(model);
            return model;
        }

        // Every head in the bundle read, once; a bundle that does not load is logged once and the heads are drawn by the code.
        private static void Load()
        {
            if (tried)
                return;
            tried = true;
            try
            {
                AssetBundle bundle = EmbeddedBundle.Load(typeof(KwCapHeads).Assembly, Bundle);
                foreach (KwCapStyle style in KwCaps.Styles)
                {
                    if (style.Head != null)
                        heads[style.Head] = Read(EmbeddedBundle.Prefab(bundle, style.Head));
                }
            }
            catch (Exception e)
            {
                Log.Warn($"Broadside: the carved post heads did not load, they are drawn as scrolls: {e.Message}");
            }
        }

        // A head prefab's mesh in the prefab's own frame, and its head_start's height.
        private static KwCapHead Read(GameObject prefab)
        {
            MeshFilter filter = prefab.GetComponentInChildren<MeshFilter>(true) ?? throw new InvalidOperationException($"{prefab.name} has no mesh");
            Transform start = GameMaterials.Find(prefab.transform, Start) ?? throw new InvalidOperationException($"{prefab.name} has no {Start}");
            Material ??= filter.GetComponent<Renderer>()?.sharedMaterial;
            Mesh mesh = filter.sharedMesh;
            Matrix4x4 into = prefab.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            Vector3[] vertices = Array.ConvertAll(mesh.vertices, v => into.MultiplyPoint3x4(v));
            Vector3[] normals = Array.ConvertAll(mesh.normals, n => into.MultiplyVector(n).normalized);
            Vector4[] tangents = Array.ConvertAll(mesh.tangents, t => Turned(into, t));
            float height = prefab.transform.InverseTransformPoint(start.position).y;
            return new KwCapHead(vertices, normals, tangents, mesh.uv, mesh.triangles, height);
        }

        private static Vector4 Turned(Matrix4x4 into, Vector4 t)
        {
            Vector3 way = into.MultiplyVector(new Vector3(t.x, t.y, t.z)).normalized;
            return new Vector4(way.x, way.y, way.z, t.w);
        }
    }
}
