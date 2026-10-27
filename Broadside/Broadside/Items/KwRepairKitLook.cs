using System;
using System.Collections.Generic;
using BundlePrefabs;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Broadside
{
    /// <summary>
    /// The repair kit's workshop look, from the embedded bundle <c>kw_repairkit</c> (ValheimAssets; <c>.windows</c> and
    /// <c>.linux</c> in <c>Broadside/assets/bundles</c>, which the csproj embeds as <c>Broadside.assets.kw_repairkit.&lt;platform&gt;</c>
    /// once the files are there): the item's model <c>kw_repairkit_wood</c> (0.51 by 0.26 by 0.23 m; no collider, so the kit
    /// is given a box round it), its inventory icon, the sprite <c>kw_repairkit_wood_icon</c>, and the patch nailed over a
    /// hole, <c>kw_hullpatch_wood</c> (0.69 by 0.64 by 0.058 m, thin along z, its front +z; <see cref="KwHullPatchBoard"/>
    /// lays its front out of the hull and scales it to the hole by its bounds). Each is a child <c>model</c> with one mesh
    /// on one Standard placeholder material (albedo, normal map, metal mask). Without the bundle the kit keeps its base
    /// item's look and icon and the patch is generated; this is logged once. The models are dressed in a game material
    /// (the kit in its base item's, a patch in the plank wood) where there is a screen; a dedicated server loads the Linux
    /// bundle and draws nothing.
    /// </summary>
    public static class KwRepairKitLook
    {
        public const string Bundle = "kw_repairkit", Item = "kw_repairkit_wood", Patch = "kw_hullpatch_wood", Icon = "kw_repairkit_wood_icon";
        private const float Gloss = 0.1f, MetalGloss = 0.62f;
        private static readonly Dictionary<(Material Game, Material Placeholder), Material> dressed = new Dictionary<(Material, Material), Material>();
        private static AssetBundle? bundle;
        private static bool tried;

        /// <summary>The bundle, loaded once; null while the mod embeds none (or it does not load).</summary>
        public static AssetBundle? Source()
        {
            if (tried)
                return bundle;
            tried = true;
            if (!Embedded())
            {
                Log.Info($"Broadside: no {Bundle} bundle embedded; the {KwRepairKit.Label} wears fine wood's look and its patches are generated.");
                return null;
            }
            try
            {
                bundle = EmbeddedBundle.Load(typeof(KwRepairKitLook).Assembly, Bundle);
            }
            catch (Exception e)
            {
                Log.Warn($"Broadside: the {Bundle} bundle did not load, the kit keeps fine wood's look: {e.Message}");
            }
            return bundle;
        }

        private static bool Embedded() =>
            Array.Exists(typeof(KwRepairKitLook).Assembly.GetManifestResourceNames(), r => r.StartsWith("Broadside.assets." + Bundle + ".", StringComparison.Ordinal));

        /// <summary>The kit wearing the bundle's icon and model, when the mod embeds the bundle.</summary>
        public static void Wear(GameObject kit, GameObject game)
        {
            AssetBundle? source = Source();
            if (source == null)
                return;
            Sprite? icon = source.LoadAsset<Sprite>(Icon);
            if (icon != null)
                kit.GetComponent<ItemDrop>().m_itemData.m_shared.m_icons = new[] { icon };
            else
                Log.Warn($"Broadside: the {Bundle} bundle has no {Icon}; the kit keeps fine wood's icon.");
            GameObject? model = source.LoadAsset<GameObject>(Item);
            if (model != null)
                Model(kit, model, GameMaterials.Borrow(game));
            else
                Log.Warn($"Broadside: the {Bundle} bundle has no {Item}; the kit keeps fine wood's look.");
        }

        /// <summary>The patch's model from the bundle, or null (the generated boards then).</summary>
        public static GameObject? PatchModel() => Source()?.LoadAsset<GameObject>(Patch);

        // The base item's meshes and colliders taken off (its body and sparkle kept, so it falls, floats and glints as before),
        // the bundle's model on in its place (it has no collider) with a box round it, every placeholder material dressed as
        // the base item's material where there is a screen.
        private static void Model(GameObject kit, GameObject model, Material? look)
        {
            foreach (System.Type kind in new[] { typeof(LODGroup), typeof(MeshRenderer), typeof(SkinnedMeshRenderer), typeof(MeshFilter), typeof(Collider) })
            {
                foreach (Component part in kit.GetComponentsInChildren(kind, true))
                    Object.DestroyImmediate(part);
            }
            GameObject worn = Object.Instantiate(model, kit.transform, false);
            worn.name = "model";
            foreach (Transform t in worn.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = kit.layer;
            Bounds bounds = KwHullPatchBoard.Measure(worn) ?? new Bounds(Vector3.zero, Vector3.one * 0.3f);
            BoxCollider box = kit.AddComponent<BoxCollider>();
            (box.center, box.size) = (bounds.center, Vector3.Max(bounds.size, Vector3.one * 0.05f));
            if (look != null && KwShipBody.Drawn)
                Dress(worn, look);
        }

        /// <summary>
        /// Every placeholder material under a model dressed as a game material: wearing its own albedo and normal map, matte,
        /// its nails shining from its metal mask; each made once and kept, so the ship's copies of a patch's are made once too.
        /// </summary>
        public static void Dress(GameObject model, Material game)
        {
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterials = Array.ConvertAll(renderer.sharedMaterials, m => m == null ? game : Dressed(game, m));
        }

        private static Material Dressed(Material game, Material placeholder)
        {
            if (dressed.TryGetValue((game, placeholder), out Material made) && made != null)
                return made;
            made = GameMaterials.Metal(GameMaterials.Plain(GameMaterials.Dress(game, placeholder), Gloss), placeholder, MetalGloss);
            return dressed[(game, placeholder)] = made;
        }
    }
}
