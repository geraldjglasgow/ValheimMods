using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Broadside
{
    /// <summary>
    /// An item copy's own look: everything the game item drew or collided with taken off (its children, the root's
    /// drawing parts and colliders), then an "attach" child (the name the game's item stands look for) holding the model,
    /// a mesh a look in that look's game material (<see cref="KwTierMaterials"/>), and one box round it all that the drop
    /// rests and is picked up by.
    /// </summary>
    public static class KwItemModel
    {
        public const string AttachName = "attach";

        public static void Dress(GameObject item, KwItemDef def)
        {
            Strip(item);
            Transform attach = new GameObject(AttachName) { layer = item.layer }.transform;
            attach.SetParent(item.transform, false);
            Bounds? all = null;
            foreach (IGrouping<KwLook, (KwLook Look, System.Action<MeshBuilder> Shape)> group in def.Parts.GroupBy(p => p.Look))
            {
                GameObject part = KwArmMeshes.Child(attach, "part_" + group.Key.ToString().ToLowerInvariant(), b => Shapes(b, group), group.Key);
                Bounds bounds = part.GetComponent<MeshFilter>().sharedMesh.bounds;
                if (all.HasValue)
                    bounds.Encapsulate(all.Value);
                all = bounds;
            }
            BoxCollider box = attach.gameObject.AddComponent<BoxCollider>();
            box.center = all?.center ?? Vector3.zero;
            box.size = Vector3.Max(all?.size ?? Vector3.one * 0.1f, Vector3.one * 0.04f);
        }

        private static void Shapes(MeshBuilder b, IGrouping<KwLook, (KwLook Look, System.Action<MeshBuilder> Shape)> group)
        {
            foreach ((KwLook _, System.Action<MeshBuilder> shape) in group)
                shape(b);
        }

        private static void Strip(GameObject item)
        {
            for (int i = item.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(item.transform.GetChild(i).gameObject);
            foreach (System.Type kind in new[] { typeof(LODGroup), typeof(Renderer), typeof(MeshFilter), typeof(Collider) })
            {
                foreach (Component part in item.GetComponents(kind))
                    Object.DestroyImmediate(part);
            }
        }
    }
}
