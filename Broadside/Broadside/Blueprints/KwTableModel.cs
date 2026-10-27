using System;
using BundlePrefabs;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Broadside
{
    /// <summary>
    /// The blueprint table's own model, as its painted hammer icon shows it: a drafting table with a longship's plan on
    /// its sloping top, from the embedded bundle <c>kw_bptable</c> (ValheimAssets <c>Assets/Ships/Keelwright/BlueprintTable</c>:
    /// prefab <c>kw_bptable</c> with <c>model</c>, the table's one mesh, and the box colliders <c>col_box_top</c>, tilted
    /// with the top, <c>col_box_legs_l</c>, <c>col_box_legs_r</c> and <c>col_box_shelf</c>). The bundle's prefab goes
    /// whole under the stripped piece as its <c>model</c> child, on the piece's layer, so its boxes are the piece's
    /// colliders on every peer, the dedicated server included (it loads the Linux bundle). On a screen it wears the
    /// workbench's own material (<c>piece_workbench</c>'s body renderer <c>high</c>, <c>Workbench_mat</c>: the game lights,
    /// wets and snows it as its pieces) with the bundle's baked albedo and normal map, the iron and brass shining from the
    /// bundle's metal mask; a dedicated server draws nothing and keeps the bundle's placeholder. Without the bundle the
    /// table keeps its generated mesh (<see cref="KwBlueprintStation"/>) and one warning is logged.
    /// </summary>
    public static class KwTableModel
    {
        private const string Bundle = "kw_bptable", Asset = "kw_bptable";
        private const string Workbench = "piece_workbench";
        // The workbench's body (New/high, Custom/Piece); its first renderer is a snow overlay whose shader moves vertices.
        private const string BodyRenderer = "high";
        private const float Gloss = 0.1f, MetalGloss = 0.62f;
        private static GameObject? source;
        private static bool tried;

        /// <summary>The table's model under the stripped piece copy; null without the bundle (the generated table then).</summary>
        public static GameObject? Wear(ZNetScene scene, GameObject piece)
        {
            GameObject? table = Source();
            if (table == null)
                return null;
            GameObject model = Object.Instantiate(table, piece.transform, false);
            model.name = "model";
            SetLayer(model.transform, piece.layer);
            if (KwIcons.CanRender)
                Dress(model, scene.GetPrefab(Workbench));
            KwPrefabs.Anchor(model);
            return model;
        }

        // The bundle's prefab, loaded once; a missing or unloadable bundle is logged once.
        private static GameObject? Source()
        {
            if (tried)
                return source;
            tried = true;
            try
            {
                source = EmbeddedBundle.Prefab(EmbeddedBundle.Load(typeof(KwTableModel).Assembly, Bundle), Asset);
            }
            catch (Exception e)
            {
                Log.Warn($"Broadside: the blueprint table's model did not load, it keeps its generated table: {e.Message}");
            }
            return source;
        }

        // Every placeholder material becomes the workbench's wearing its maps; without the workbench the placeholder stays.
        private static void Dress(GameObject model, GameObject? workbench)
        {
            Material? wood = GameMaterials.Borrow(workbench, BodyRenderer);
            if (wood == null)
            {
                Log.Warn($"Broadside: no {Workbench} body material; the blueprint table wears its bundle's placeholder.");
                return;
            }
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterials = Array.ConvertAll(renderer.sharedMaterials, m => m == null ? wood : Dressed(wood, m));
        }

        private static Material Dressed(Material wood, Material placeholder) =>
            GameMaterials.Metal(GameMaterials.Plain(GameMaterials.Dress(wood, placeholder), Gloss), placeholder, MetalGloss);

        private static void SetLayer(Transform part, int layer)
        {
            part.gameObject.layer = layer;
            foreach (Transform child in part)
                SetLayer(child, layer);
        }
    }
}
