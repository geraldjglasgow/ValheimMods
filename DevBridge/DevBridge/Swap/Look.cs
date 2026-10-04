using System.Collections.Generic;
using System.Linq;
using DevBridge.Stage;
using UnityEngine;

namespace DevBridge.Swap
{
    /// <summary>What a renderer drew before a swap: its mesh, and for a skinned one its bones, root bone and bounds; its materials.</summary>
    internal sealed class Look
    {
        internal Mesh Mesh;
        internal Transform[] Bones;
        internal Transform RootBone;
        internal Bounds Bounds;
        internal Material[] Materials;

        internal static Look Of(Renderer renderer)
        {
            var look = new Look { Mesh = MeshOf(renderer), Materials = renderer.sharedMaterials };
            if (!(renderer is SkinnedMeshRenderer skin)) return look;
            look.Bones = skin.bones;
            look.RootBone = skin.rootBone;
            look.Bounds = skin.localBounds;
            return look;
        }

        /// <summary>
        /// The mesh, bones and bounds back (materials are put back on their own): as recorded on the renderer it was
        /// recorded from (no target), or on a copy made from the swapped prefab, with the bones found in that copy.
        /// </summary>
        internal void PutBack(Renderer renderer, Target copy)
        {
            if (!(renderer is SkinnedMeshRenderer skin))
            {
                renderer.GetComponent<MeshFilter>().sharedMesh = Mesh;
                return;
            }
            skin.sharedMesh = Mesh;
            skin.bones = copy == null ? Bones : copy.Bones(Bones);
            skin.rootBone = copy == null ? RootBone : copy.RootBone(RootBone);
            skin.localBounds = Bounds;
        }

        internal static Mesh MeshOf(Renderer renderer) =>
            renderer is SkinnedMeshRenderer skin ? skin.sharedMesh : renderer.GetComponent<MeshFilter>()?.sharedMesh;

        internal static int Triangles(Renderer renderer)
        {
            Mesh mesh = MeshOf(renderer);
            return mesh ? AssetInfo.Triangles(mesh) : 0;
        }
    }

    /// <summary>Transform paths relative to a root ("Visual/Armature/Hips"; "" for the root itself), the same in every copy of a prefab.</summary>
    internal static class Paths
    {
        internal static string Of(Transform node, Transform root)
        {
            var names = new List<string>();
            for (Transform at = node; at && at != root; at = at.parent) names.Add(at.name);
            names.Reverse();
            return string.Join("/", names);
        }

        internal static Transform Find(Transform root, string path) => path.Length == 0 ? root : root.Find(path);

        /// <summary>The first transform of that name under the root, the root included.</summary>
        internal static Transform Named(Transform root, string name) =>
            root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
    }
}
