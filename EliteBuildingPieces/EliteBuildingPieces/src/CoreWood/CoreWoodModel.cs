using System;
using System.Collections.Generic;
using BundlePrefabs;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EliteBuildingPieces.CoreWood
{
    /// <summary>
    /// A core wood piece's own model and build-menu icon, from its embedded bundle (prefab and bundle share the name;
    /// children <c>model</c>, box colliders <c>col_*</c>, anchors <c>snap_*</c>, and on the doors the leaves
    /// <c>door_left</c>/<c>door_right</c> hinged inside their posts). The game piece's look, colliders and snap points go
    /// and the model's take their place, in the game's own core wood material: the models are mapped onto the game's
    /// core wood atlas, so they wear <c>logwall</c> from <c>wood_wall_log</c> whole (Custom/Piece, its own tint), like
    /// the game's core wood logs. The bundle's animator goes too: the game's Door keeps its own (<see cref="GateSwing"/>).
    /// Every peer, identically. A bundle that cannot load leaves the game piece's look.
    /// </summary>
    public static class CoreWoodModel
    {
        private const string MaterialSource = "wood_wall_log";
        private const string MaterialName = "logwall";
        private const string Shader = "Custom/Piece";

        private static readonly Dictionary<string, AssetBundle> bundles = new Dictionary<string, AssetBundle>();

        public static bool Wear(GameObject piece, CoreWoodPiece def, Material wood)
        {
            AssetBundle bundle = Load(def.Bundle);
            GameObject model = bundle != null ? Model(bundle, def.Bundle) : null;
            if (model == null)
                return false;
            Strip(piece);
            GameObject placed = Object.Instantiate(model, piece.transform, false);
            placed.name = "corewood";
            foreach (Animator animator in placed.GetComponentsInChildren<Animator>(true))
                Object.DestroyImmediate(animator);
            Dress(placed, wood);
            Unmirror(placed);
            SetLayer(placed.transform, piece.layer);
            CoreWoodSnaps.Lift(piece.transform, placed.transform);
            Point(piece.GetComponent<WearNTear>(), placed);
            if (def.IsDoor)
                piece.AddComponent<GateSwing>();
            SetIcon(piece.GetComponent<Piece>(), bundle);
            return true;
        }

        /// <summary>The game's core wood wall material (shared, never changed), or null.</summary>
        public static Material LogWall(List<GameObject> prefabs)
        {
            GameObject source = prefabs.Find(p => p != null && p.name == MaterialSource);
            if (source == null)
                return null;
            foreach (MeshRenderer renderer in source.GetComponentsInChildren<MeshRenderer>(true))
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material != null && material.name == MaterialName && material.shader.name == Shader)
                        return material;
                }
            }
            Plugin.Log.LogWarning($"EliteBuildingPieces: {MaterialSource} has no {MaterialName}: the core wood pieces keep the bundle's materials");
            return null;
        }

        private static AssetBundle Load(string name)
        {
            if (bundles.TryGetValue(name, out AssetBundle bundle))
                return bundle;
            try
            {
                bundle = EmbeddedBundle.Load(typeof(CoreWoodModel).Assembly, name);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"EliteBuildingPieces: the model {name} did not load: {e.Message}");
            }
            bundles[name] = bundle;
            return bundle;
        }

        private static GameObject Model(AssetBundle bundle, string name)
        {
            GameObject model = bundle.LoadAsset<GameObject>(name);
            if (model != null)
                return model;
            GameObject[] all = bundle.LoadAllAssets<GameObject>();
            return all.Length > 0 ? all[0] : null;
        }

        // Everything the game piece drew or collided with: its children (models, wear states, snow, snap points, the
        // gate's leaves), the root's own drawing parts and colliders, its level-of-detail group. The root's animator stays.
        private static void Strip(GameObject piece)
        {
            for (int i = piece.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(piece.transform.GetChild(i).gameObject);
            foreach (Type kind in new[] { typeof(LODGroup), typeof(Renderer), typeof(MeshFilter), typeof(Collider) })
            {
                foreach (Component part in piece.GetComponents(kind))
                    Object.DestroyImmediate(part);
            }
        }

        private static void Dress(GameObject placed, Material wood)
        {
            if (wood == null)
                return;
            foreach (Renderer renderer in placed.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                    materials[i] = wood;
                renderer.sharedMaterials = materials;
            }
        }

        // The walls' collider boxes come from Blender mirrored into Unity: negative scale (-2, -0.4, -3.467). Unity's
        // BoxCollider does not take negative scale, so the walls had no collider, found no ground under them and broke
        // a second after placing. A box mirrored about its own transform is the same box with the scale made positive
        // and its centre mirrored with it.
        private static void Unmirror(GameObject placed)
        {
            foreach (BoxCollider box in placed.GetComponentsInChildren<BoxCollider>(true))
            {
                Vector3 scale = box.transform.localScale;
                Vector3 sign = new Vector3(Mathf.Sign(scale.x), Mathf.Sign(scale.y), Mathf.Sign(scale.z));
                if (sign == Vector3.one)
                    continue;
                box.center = Vector3.Scale(box.center, sign);
                box.transform.localScale = Vector3.Scale(scale, sign);
            }
        }

        private static void SetLayer(Transform part, int layer)
        {
            part.gameObject.layer = layer;
            foreach (Transform child in part)
                SetLayer(child, layer);
        }

        // One model for every wear state. The stakewall's fragment roots were its stripped wear models: without them the
        // game breaks the whole piece into fragments, as it does for pieces that name none.
        private static void Point(WearNTear wear, GameObject placed)
        {
            if (wear == null)
                return;
            wear.m_new = placed;
            wear.m_worn = placed;
            wear.m_broken = placed;
            wear.m_wet = null;
            wear.m_fragmentRoots = null;
        }

        private static void SetIcon(Piece piece, AssetBundle bundle)
        {
            Sprite[] icons = bundle.LoadAllAssets<Sprite>();
            if (piece != null && icons.Length > 0)
                piece.m_icon = icons[0];
        }
    }
}
