using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Broadside
{
    /// <summary>
    /// The hammock's look and colliders, made in code from its shape (<see cref="KwHammockSpec"/>): on the prefab once, from
    /// the standard shape, after the copy of the game's bed is stripped of the bed's own (its player base area kept, switched
    /// off as the game's bed has it); on a placed hammock again from the shape in its ZDO (<see cref="KwHammock"/>), the
    /// prefab's parts let go. Under <see cref="ModelName"/>: the posts in the plank wood (its own mesh), the hooks in the
    /// game's iron (<see cref="HooksName"/>), the solid boxes of posts, feet and hooks (<see cref="KwHammockParts.Boxes"/>) and a
    /// non-solid box over the cloth (the bed is hovered there, players walk through it); and <see cref="SlingName"/> on the
    /// rope line (<see cref="KwHammockSling"/>): the cloth (<see cref="KwHammockCloth"/>), the spreader bars, the four ropes in
    /// the game's rope (<see cref="KwSailCloth.Rope"/>, line renderers in the sling's own space) and the bed's spawn point
    /// (<see cref="SpawnName"/>, level, along the span). The sling has no collider, so swinging it never moves a ship's body.
    /// A dedicated server makes the colliders, the sling and the spawn point, and no mesh.
    /// </summary>
    public static class KwHammockModel
    {
        public const string ModelName = "model", SlingName = "sling", SpawnName = "spawnpoint", HooksName = "hooks";
        public const string SlingPath = ModelName + "/" + SlingName;
        private const string Kept = "PlayerBase";
        private const float Rope = 0.018f;

        /// <summary>The bed copy stripped and wearing the standard hammock: the prefab's look, ghost and icon.</summary>
        public static void Wear(GameObject hammock)
        {
            Strip(hammock);
            Dress(hammock, KwHammockSpec.Standard, null);
        }

        // The bed's children but its player base area, and its root's drawing parts and solid colliders.
        private static void Strip(GameObject hammock)
        {
            for (int i = hammock.transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = hammock.transform.GetChild(i).gameObject;
                if (child.name != Kept)
                    Object.DestroyImmediate(child);
            }
            foreach (Type kind in new[] { typeof(LODGroup), typeof(Renderer), typeof(MeshFilter) })
            {
                foreach (Component part in hammock.GetComponents(kind))
                    Object.DestroyImmediate(part);
            }
            foreach (Collider collider in hammock.GetComponents<Collider>())
            {
                if (!collider.isTrigger)
                    Object.DestroyImmediate(collider);
            }
        }

        /// <summary>
        /// The hammock made from a shape: the model's parts (any it wore let go), the sling and the bed's spawn point, the
        /// wear states on the model and the game's support told to read the colliders again. The meshes made go into
        /// <paramref name="made"/> (null for the prefab's, kept for good). The sling and its turn at rest.
        /// </summary>
        public static (Transform Sling, Quaternion Turn) Dress(GameObject hammock, KwHammockSpec spec, List<Object>? made)
        {
            Transform model = Model(hammock);
            Clear(model);
            KwHammockSling sling = new KwHammockSling(spec);
            if (KwIcons.CanRender)
                Draw(model, spec, made);
            KwHammockParts.Boxes(model, spec);
            int open = LayerMask.NameToLayer("piece_nonsolid");
            KwHammockParts.Box(model, "col_cloth", new Vector3(0f, 0f, spec.Bars - 0.025f), Quaternion.identity,
                new Vector3(KwHammockShape.Width + 0.1f, 0.3f, sling.Length), open >= 0 ? open : -1);
            Transform node = Sling(model, sling, made);
            hammock.GetComponent<Bed>().m_spawnPoint = Spawn(node, sling);
            Worn(hammock.GetComponent<WearNTear>(), model.gameObject);
            return (node, sling.Rest);
        }

        // The model child (made once, on the piece's layer), drawing the posts on its own mesh.
        private static Transform Model(GameObject hammock)
        {
            Transform? model = hammock.transform.Find(ModelName);
            if (model != null)
                return model;
            GameObject made = new GameObject(ModelName) { layer = hammock.layer };
            made.transform.SetParent(hammock.transform, false);
            made.AddComponent<MeshFilter>();
            made.AddComponent<MeshRenderer>();
            return made.transform;
        }

        // The parts it wore, switched off at once (no collider or renderer counts this frame) and let go.
        private static void Clear(Transform model)
        {
            model.GetComponent<MeshFilter>().sharedMesh = null;
            for (int i = model.childCount - 1; i >= 0; i--)
            {
                Transform child = model.GetChild(i);
                child.gameObject.SetActive(false);
                child.SetParent(null, false);
                Object.Destroy(child.gameObject);
            }
        }

        // The posts on the model's own mesh in the plank wood, the hooks on a child in the game's iron.
        private static void Draw(Transform model, KwHammockSpec spec, List<Object>? made)
        {
            MeshBuilder wood = new MeshBuilder(Vector3.zero), iron = new MeshBuilder(Vector3.zero);
            KwHammockParts.Posts(wood, spec);
            KwHammockParts.Hooks(iron, spec);
            if (spec.A.Post || spec.B.Post)
                model.GetComponent<MeshFilter>().sharedMesh = Keep(wood.ToMesh("BS_hammock_frame"), made);
            if (KwPrefabs.Planks != null)
                model.GetComponent<MeshRenderer>().sharedMaterial = KwPrefabs.Planks;
            if (spec.Hooks)
                Part(model, HooksName, Keep(iron.ToMesh("BS_hammock_hooks"), made), KwOpeningLooks.Iron);
        }

        private static Mesh Keep(Mesh mesh, List<Object>? made)
        {
            made?.Add(mesh);
            return mesh;
        }

        private static GameObject Part(Transform parent, string name, Mesh mesh, Material? material)
        {
            GameObject part = new GameObject(name) { layer = parent.gameObject.layer };
            part.transform.SetParent(parent, false);
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = part.AddComponent<MeshRenderer>();
            if (material != null)
                renderer.sharedMaterial = material;
            return part;
        }

        // The sling on the rope line, turned along it: cloth, bars and ropes where there is a screen.
        private static Transform Sling(Transform model, KwHammockSling sling, List<Object>? made)
        {
            Transform node = new GameObject(SlingName) { layer = model.gameObject.layer }.transform;
            node.SetParent(model, false);
            node.SetLocalPositionAndRotation(KwMesh.ToUnity(sling.Middle), sling.Rest);
            if (!KwIcons.CanRender)
                return node;
            Part(node, "cloth", Keep(KwHammockShape.ClothMesh(sling, "BS_hammock_cloth"), made), KwHammockCloth.Material);
            Part(node, "bars", Keep(KwHammockShape.BarsMesh(sling, "BS_hammock_bars"), made), KwPrefabs.Planks);
            int i = 0;
            foreach ((Vector3 from, Vector3 to) in sling.Ropes())
                KwSailRigging.Line(node, "rope_" + i++, KwSailCloth.Rope, Rope, new List<Vector3> { sling.In(from), sling.In(to) });
            return node;
        }

        // Where a sleeper lies and a player who slept here wakes: over the cloth's middle, level, along the span.
        private static Transform Spawn(Transform sling, KwHammockSling shape)
        {
            Transform spawn = new GameObject(SpawnName) { layer = sling.gameObject.layer }.transform;
            spawn.SetParent(sling, false);
            spawn.SetLocalPositionAndRotation(KwMesh.ToUnity(shape.In(shape.Lying)), Quaternion.Inverse(shape.Rest));
            return spawn;
        }

        // The wear states all the model; no wet look, no snow (the bed's went with its children), no fragments
        // (WearNTear.Destroy would throw on the bed's after refunding and before removing the piece); the support's
        // colliders read again.
        private static void Worn(WearNTear wear, GameObject model)
        {
            wear.m_new = wear.m_worn = wear.m_broken = model;
            (wear.m_wet, wear.m_fragmentRoots) = (null, null);
            (wear.m_snow, wear.m_snowWorn, wear.m_snowBroken) = (null, null, null);
            wear.m_colliders = null;
        }
    }
}
