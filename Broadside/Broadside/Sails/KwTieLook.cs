using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Broadside
{
    /// <summary>
    /// The fittings ropes are made fast to (<see cref="KwTieShapes"/>) drawn under a piece's transform, in their ties' tiers'
    /// wood (the deck's) and metal (iron in the Meadows and the Swamp, bronze, silver, black metal, flametal; the deadeyes'
    /// holes black metal, their lanyards and the turns on the cleats the game's rope), or handed out as meshes for a ghost or
    /// a preview. No colliders, casting no shadow; only where something is drawn (<see cref="KwShipBody.Drawn"/>).
    /// </summary>
    public static class KwTieLook
    {
        public const string Name = "kw_ties";

        private static readonly Dictionary<string, KwLook> Metals = new Dictionary<string, KwLook>
        {
            ["meadows"] = KwLook.Iron, ["blackforest"] = KwLook.Bronze, ["swamp"] = KwLook.Iron, ["mountains"] = KwLook.Silver,
            ["plains"] = KwLook.BlackMetal, ["mistlands"] = KwLook.BlackMetal, ["ashlands"] = KwLook.Flametal,
        };

        /// <summary>Every tie's fitting under one child of a parent (any old one of that name gone first).</summary>
        public static void Draw(Transform parent, IEnumerable<KwRigTie> ties, string name = Name)
        {
            Transform? old = parent.Find(name);
            if (old != null)
            {
                old.name = name + "_gone";
                Object.Destroy(old.gameObject);
            }
            if (!KwShipBody.Drawn)
                return;
            GameObject holder = new GameObject(name) { layer = parent.gameObject.layer };
            holder.transform.SetParent(parent, false);
            foreach ((Mesh mesh, Material? look) in Meshes(ties))
                Part(holder.transform, mesh, look);
        }

        /// <summary>The fittings' meshes (the piece's kit placed in Unity axes), one per tier and look.</summary>
        public static List<(Mesh Mesh, Material? Look)> Meshes(IEnumerable<KwRigTie> ties)
        {
            List<(Mesh, Material?)> meshes = new List<(Mesh, Material?)>();
            Dictionary<string, MeshBuilder[]> tiers = new Dictionary<string, MeshBuilder[]>();
            foreach (KwRigTie tie in ties)
            {
                if (!tiers.TryGetValue(tie.Tier.Id, out MeshBuilder[] b))
                    tiers[tie.Tier.Id] = b = new[] { new MeshBuilder(Vector3.zero), new MeshBuilder(Vector3.zero), new MeshBuilder(Vector3.zero), new MeshBuilder(Vector3.zero) };
                KwTieShapes.Draw(tie, b[0], b[1], b[2], b[3]);
            }
            foreach (KeyValuePair<string, MeshBuilder[]> tier in tiers)
            {
                Material?[] looks = Looks(KwBiomeTiers.Of(tier.Key));
                for (int i = 0; i < looks.Length; i++)
                {
                    Mesh mesh = tier.Value[i].ToMesh("kw_tie_" + i);
                    if (mesh.vertexCount > 0)
                        meshes.Add((mesh, looks[i]));
                    else
                        Object.Destroy(mesh);
                }
            }
            return meshes;
        }

        // A tier's wood, metal, the holes' black and the rope.
        private static Material?[] Looks(KwBiomeTier tier) => new[]
        {
            KwBiomeLooks.For(KwPart.Deck, tier) ?? KwPrefabs.Planks,
            KwTierMaterials.Of(Metals.TryGetValue(tier.Id, out KwLook look) ? look : KwLook.Iron),
            KwTierMaterials.Of(KwLook.BlackMetal),
            KwSailCloth.Rope,
        };

        private static void Part(Transform holder, Mesh mesh, Material? look)
        {
            GameObject part = new GameObject("tie") { layer = holder.gameObject.layer };
            part.transform.SetParent(holder, false);
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = part.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            if (look != null)
                renderer.sharedMaterial = look;
            part.AddComponent<KwMeshOwner>();
        }
    }
}
