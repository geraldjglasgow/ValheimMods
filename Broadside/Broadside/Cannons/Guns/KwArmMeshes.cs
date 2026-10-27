using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A cannon's or a cannon item's meshes, on the bench after the piece or item is made: a child mesh in one look, in its
    /// own game material (<see cref="KwTierMaterials"/>; an item atlas's look shows its clean part,
    /// <see cref="KwRudderWindows"/>), and the piece's "model" worn in another look.
    /// </summary>
    public static class KwArmMeshes
    {
        /// <summary>The piece's "model" (made in the plank wood) in another look, its UVs fitted to that look's texture.</summary>
        public static void Wear(Transform? model, KwLook look)
        {
            MeshRenderer? body = model != null ? model.GetComponent<MeshRenderer>() : null;
            Material? material = KwTierMaterials.Of(look);
            if (body == null || material == null || look == KwLook.Planks)
                return;
            body.sharedMaterial = material;
            Mesh? mesh = body.GetComponent<MeshFilter>().sharedMesh;
            if (mesh != null)
                KwRudderWindows.Fit(mesh, look);
        }

        /// <summary>A child mesh of the piece in one look, drawn in the piece's own kit axes, its UVs fitted to the look's texture.</summary>
        public static GameObject Child(Transform parent, string name, System.Action<MeshBuilder> shape, KwLook look)
        {
            GameObject child = new GameObject(name) { layer = parent.gameObject.layer };
            child.transform.SetParent(parent, false);
            MeshBuilder builder = new MeshBuilder(Vector3.zero);
            shape(builder);
            Mesh mesh = builder.ToMesh(parent.name + "_" + name);
            KwRudderWindows.Fit(mesh, look);
            child.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = child.AddComponent<MeshRenderer>();
            Material? material = KwTierMaterials.Of(look);
            if (material != null)
                renderer.sharedMaterial = material;
            return child;
        }
    }
}
