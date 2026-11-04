using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A dock lantern's post in a preview (<see cref="KwDockLanternPiece"/>): the hammer's ghost shows the post the aim's site
    /// would give, at its length, seed and material (<see cref="Ghost"/>: one mesh, made again only when one of those
    /// changes, the last one dropped); the Auto-decorate tool's see-through previews show each planned lantern's post at its
    /// material and length to the quarter metre (<see cref="Sketch"/>: meshes shared by those, made once a session). Only
    /// where there is a screen; the preview's own material is kept but for the hammer's ghost, which wears the dock's.
    /// </summary>
    public static class KwDockLanternLook
    {
        private const float Quarter = 0.25f;
        private static readonly Dictionary<string, Mesh> sketches = new Dictionary<string, Mesh>();
        private static (string Key, Mesh? Mesh) ghost;

        /// <summary>The hammer's ghost: its post at this material, length and seed, in the dock's material.</summary>
        public static void Ghost(Transform root, KwPierKind kind, float length, int seed)
        {
            Transform? post = Post(root);
            if (post == null || !KwIcons.CanRender)
                return;
            string key = $"{kind.Id}|{length:F2}|{seed}";
            if (ghost.Key != key)
            {
                if (ghost.Mesh != null)
                    Object.Destroy(ghost.Mesh);
                ghost = (key, KwDockLanternShape.Mesh(kind, length, seed, KwDockLanternPiece.Prefab + "_ghost"));
            }
            post.GetComponent<MeshFilter>().sharedMesh = ghost.Mesh;
            Material? look = KwPierLooks.Of(kind);
            if (look != null)
                post.GetComponent<MeshRenderer>().sharedMaterial = look;
        }

        /// <summary>An Auto-decorate preview: its post at the planned material and length (to the quarter metre), see-through.</summary>
        public static void Sketch(Transform root, KwDockLanternSpec spec)
        {
            Transform? post = Post(root);
            if (post == null || !KwIcons.CanRender)
                return;
            float length = Mathf.Max(Quarter, Mathf.Round(spec.Length / Quarter) * Quarter);
            string key = $"{spec.Kind.Id}|{length:F2}";
            if (!sketches.TryGetValue(key, out Mesh mesh) || mesh == null)
                sketches[key] = mesh = KwDockLanternShape.Mesh(spec.Kind, length, 1, KwDockLanternPiece.Prefab + "_sketch");
            post.GetComponent<MeshFilter>().sharedMesh = mesh;
            Material? glass = KwPreview.Glass(KwPierLooks.Of(spec.Kind));
            if (glass != null)
                post.GetComponent<MeshRenderer>().sharedMaterial = glass;
        }

        // The post's drawn part under a piece, its ghost or a preview copy (named as the piece's).
        private static Transform? Post(Transform root)
        {
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.name == KwDockLanternPiece.PostName)
                    return filter.transform;
            }
            return null;
        }
    }
}
