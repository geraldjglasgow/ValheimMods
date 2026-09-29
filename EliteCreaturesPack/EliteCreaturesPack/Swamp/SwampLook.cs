using System.Collections.Generic;
using System.Linq;
using BundlePrefabs;
using EliteCreaturesPack.Core;
using UnityEngine;

namespace EliteCreaturesPack.Swamp
{
    /// <summary>Kits use the base creature's root-space rest pose; preserve that pose when parenting to a moving bone.</summary>
    public static class SwampLook
    {
        public static void Wear(GameObject creature, SwampKind kind, AssetBundle bundle, GameObject draugr)
        {
            Renderer? body = draugr.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .OrderByDescending(renderer => renderer.sharedMesh != null ? renderer.sharedMesh.vertexCount : 0).FirstOrDefault();
            if (body?.sharedMaterial == null) return;
            string[] bones = kind.Base == "Blob" ? new[] { "Bone", "Bone.002" }
                : kind.Base == "Neck" ? new[] { "Spine", "Spine1" } : new[] { "Spine1", "Spine", "spine1", "spine" };
            Attach(creature.transform, bundle, kind.Asset, body.sharedMaterial, bones);
            if (kind.Jarl) Attach(creature.transform, bundle, "ecp_mire_crown", body.sharedMaterial, new[] { "Head", "head" });
        }

        private static void Attach(Transform root, AssetBundle bundle, string asset, Material skin, string[] bones)
        {
            GameObject? prefab = bundle.LoadAsset<GameObject>(asset);
            if (prefab == null) { Log.Warn("Swamp kit missing: " + asset); return; }
            Transform? bone = bones.Select(name => GameMaterials.Find(root, name)).FirstOrDefault(found => found != null);
            if (bone == null) { Log.Warn("Swamp attachment bone missing: " + asset); return; }
            GameObject kit = Object.Instantiate(prefab, root, false);
            kit.name = asset;
            Dress(kit, skin);
            kit.transform.SetParent(bone, true);
        }

        private static void Dress(GameObject kit, Material skin)
        {
            var dressed = new Dictionary<Material, Material>();
            foreach (Renderer renderer in kit.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; ++i)
                {
                    Material original = materials[i];
                    if (original.mainTexture != null) original.mainTexture.filterMode = FilterMode.Point;
                    if (!dressed.TryGetValue(original, out Material material))
                        dressed[original] = material = GameMaterials.Plain(GameMaterials.Dress(skin, original), 0.08f);
                    materials[i] = material;
                }
                renderer.sharedMaterials = materials;
            }
        }
    }
}
