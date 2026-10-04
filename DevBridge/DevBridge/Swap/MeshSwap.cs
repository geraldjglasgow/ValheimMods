using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DevBridge.Swap
{
    /// <summary>
    /// A bundle renderer's mesh on a live renderer. A skinned mesh is rebound to the live skeleton by bone names, in the
    /// mesh's own bone order (its bind poses come with the mesh), as CreatureBody.Rebind puts a workshop body on a game
    /// creature; its root bone and bounds follow. A mesh filter just takes the mesh.
    /// </summary>
    internal static class MeshSwap
    {
        /// <summary>Puts the mesh on; null when done, else why the renderer was left as it was.</summary>
        internal static string Put(Renderer live, Renderer bundle, Target target)
        {
            if (live is SkinnedMeshRenderer skin && bundle is SkinnedMeshRenderer ours) return Skin(skin, ours, target);
            if (live is SkinnedMeshRenderer || bundle is SkinnedMeshRenderer)
                return $"the bundle's is a {bundle.GetType().Name} and this one a {live.GetType().Name}: a skinned mesh only goes on a skinned renderer";
            live.GetComponent<MeshFilter>().sharedMesh = bundle.GetComponent<MeshFilter>().sharedMesh;
            return null;
        }

        private static string Skin(SkinnedMeshRenderer live, SkinnedMeshRenderer ours, Target target)
        {
            Dictionary<string, Transform> bones = Bones(live, target);
            List<string> missing = ours.bones.Where(b => !b || !bones.ContainsKey(b.name)).Select(b => b ? b.name : "(none)").ToList();
            if (missing.Count > 0 && !OnSkin(live, target)) return "bones missing here: " + Names(missing);
            live.sharedMesh = ours.sharedMesh;
            if (missing.Count == 0) live.bones = ours.bones.Select(b => bones[b.name]).ToArray();
            if (ours.rootBone && bones.TryGetValue(ours.rootBone.name, out Transform root)) live.rootBone = root;
            live.localBounds = ours.localBounds;
            return null;
        }

        /// <summary>The bones the renderer rides now by name, then every transform of the object (or the character wearing it).</summary>
        private static Dictionary<string, Transform> Bones(SkinnedMeshRenderer live, Target target)
        {
            var bones = new Dictionary<string, Transform>();
            foreach (Transform bone in live.bones.Where(b => b).Concat(target.BoneRoot.GetComponentsInChildren<Transform>(true)))
            {
                if (!bones.ContainsKey(bone.name)) bones.Add(bone.name, bone);
            }
            return bones;
        }

        /// <summary>
        /// An item's skin part (attach_skin) on the item prefab has no skeleton of its own: VisEquipment gives it the
        /// wearer's bones when it is put on, so there it takes the mesh alone.
        /// </summary>
        private static bool OnSkin(Renderer live, Target target)
        {
            if (target.Prefix == "attach_skin") return false;
            for (Transform at = live.transform; at; at = at == target.Root.transform ? null : at.parent)
                if (at.name == "attach_skin") return true;
            return false;
        }

        private static string Names(List<string> names) =>
            string.Join(", ", names.Take(8)) + (names.Count > 8 ? $" and {names.Count - 8} more" : "");
    }
}
