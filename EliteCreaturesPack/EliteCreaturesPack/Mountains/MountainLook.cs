using System.Linq;
using BundlePrefabs;
using EliteCreaturesPack.Core;
using UnityEngine;

namespace EliteCreaturesPack.Mountains
{
    public static class MountainLook
    {
        public static void Dress(GameObject creature, MountainKind kind, GameObject asset)
        {
            Material? skin = creature.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Select(r => r.sharedMaterial).FirstOrDefault(m => m != null);
            Tint(creature, kind);
            GameObject kit = Object.Instantiate(asset, creature.transform, false);
            kit.name = kind.Asset;
            kit.transform.localPosition = Vector3.zero;
            kit.transform.localRotation = Quaternion.identity;
            kit.transform.localScale = Vector3.one;
            if (skin != null) Materials(kit, skin);
            string bone = kind.Id == "IceCrawler" ? "Spine1" : kind.Flying || kind.Night ? "Head" : "Spine2";
            Transform? anchor = GameMaterials.Find(creature.transform, bone);
            if (anchor == null) Log.Warn(kind.Name + ": missing attachment bone " + bone + "; kit follows creature root.");
            else kit.transform.SetParent(anchor, true); // Art is measured in the vanilla prefab's root rest frame.
        }
        private static void Materials(GameObject kit, Material skin)
        {
                foreach (Renderer renderer in kit.GetComponentsInChildren<Renderer>(true))
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(m =>
                    {
                        if (m.mainTexture != null) m.mainTexture.filterMode = FilterMode.Point;
                        return GameMaterials.Plain(GameMaterials.Dress(skin, m), .08f);
                    }).ToArray();
        }
        public static void Tint(GameObject creature, MountainKind kind)
        {
            Color tint = kind.Boss ? new Color(.82f, .85f, .83f) : kind.Id == "Rimeback" ? new Color(.8f, .82f, .78f)
                : kind.Night ? new Color(.73f, .76f, .73f) : kind.Flying ? new Color(.8f, .84f, .86f) : new Color(.7f, .81f, .79f);
            foreach (SkinnedMeshRenderer renderer in creature.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                renderer.sharedMaterials = renderer.sharedMaterials.Select(m =>
                {
                    if (m == null) return m;
                    var copy = new Material(m);
                    if (copy.HasProperty("_Color")) copy.color *= tint;
                    return copy;
                }).ToArray();
        }
    }
}
