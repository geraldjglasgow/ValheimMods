using System;
using System.Collections.Generic;
using BundlePrefabs;
using EliteCreaturesReborn.Arena.Hall;
using EliteCreaturesReborn.Util;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EliteCreaturesReborn.Rendering
{
    /// <summary>
    /// The Rendering Table's own model and build-menu icon (a player, 2026-10-22: the squeezed workbench looked like the
    /// basic table; the user: a completely new, decorated table, half the workbench's width and as tall), from the
    /// embedded bundle <c>ecr_renderingtable</c> (ValheimAssets <c>Assets/Props/RenderingTable</c>, its BRIEF.md the
    /// contract: prefab <c>ecr_renderingtable</c> with <c>model</c> and <c>col_*</c> box colliders; sprite
    /// <c>ecr_renderingtable_icon</c>). The workbench copy's look and colliders go and the model's take their place,
    /// dressed in the workbench's body material (<c>Workbench_mat</c>, Custom/Piece, so the game lights, wets and snows it
    /// like its pieces) wearing the model's baked maps, its iron shining through the model's metal mask. One object for
    /// every wear state. Every peer, identically. A bundle that cannot load leaves the squeezed workbench, logged.
    /// </summary>
    internal static class RenderModel
    {
        public const string Bundle = "ecr_renderingtable";

        private const string Asset = "ecr_renderingtable";
        private const string Placed = "renderingtable";
        private const float Gloss = 0.1f;
        private const float MetalGloss = 0.6f;

        // The workbench's body renderer (Custom/Piece), never its first: that is a snow overlay whose shader moves
        // vertices and throws a model's parts about (Mímir's Chest and the Rune Table, 2026-10-07).
        private const string BenchRenderer = "high";

        private static AssetBundle? _bundle;
        private static bool _tried;

        /// <summary>The build menu icon; null without the bundle (the workbench's stays).</summary>
        public static Sprite? Icon { get; private set; }

        /// <summary>Puts the model on the bench copy of the workbench; false when the bundle is missing.</summary>
        public static bool Wear(GameObject piece)
        {
            AssetBundle? bundle = Load();
            GameObject? model = bundle != null ? bundle.LoadAsset<GameObject>(Asset) : null;
            if (bundle == null || model == null)
            {
                Log.Error("the Rendering Table's model did not load: it keeps the squeezed workbench's look.");
                return false;
            }
            Material? body = GameMaterials.Borrow(piece, BenchRenderer);
            Strip(piece);
            GameObject placed = Object.Instantiate(model, piece.transform, false);
            placed.name = Placed;
            Dress(placed, body);
            HallBuilder.SetLayer(placed.transform, piece.layer);
            Point(piece.GetComponent<WearNTear>(), placed);
            Icon = bundle.LoadAsset<Sprite>(Asset + "_icon");
            return true;
        }

        private static AssetBundle? Load()
        {
            if (_tried)
            {
                return _bundle;
            }
            _tried = true;
            try
            {
                _bundle = EmbeddedBundle.Load(typeof(RenderModel).Assembly, Bundle);
            }
            catch (Exception e)
            {
                Log.Error($"the Rendering Table's bundle did not load: {e.Message}");
            }
            return _bundle;
        }

        // Everything the workbench drew or collided with: its children (model, wear states, snow, colliders), the root's
        // own drawing parts and colliders, its level-of-detail group.
        private static void Strip(GameObject piece)
        {
            for (int i = piece.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(piece.transform.GetChild(i).gameObject);
            }
            foreach (Type kind in new[] { typeof(LODGroup), typeof(Renderer), typeof(MeshFilter), typeof(Collider) })
            {
                foreach (Component part in piece.GetComponents(kind))
                {
                    Object.DestroyImmediate(part);
                }
            }
        }

        // Slots sharing a placeholder (one atlas) share one dressed material too.
        private static void Dress(GameObject placed, Material? body)
        {
            if (body == null)
            {
                Log.Warn("the workbench's body material was not found: the Rendering Table keeps the bundle's own.");
                return;
            }
            Dictionary<Material, Material> dressed = new Dictionary<Material, Material>();
            foreach (Renderer renderer in placed.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = materials[i] == null ? body : DressedFor(materials[i], body, dressed);
                }
                renderer.sharedMaterials = materials;
            }
        }

        private static Material DressedFor(Material placeholder, Material body, Dictionary<Material, Material> dressed)
        {
            if (!dressed.TryGetValue(placeholder, out Material material))
            {
                Material plain = GameMaterials.Plain(GameMaterials.Dress(body, placeholder), Gloss);
                material = GameMaterials.Metal(plain, placeholder, MetalGloss);
                dressed.Add(placeholder, material);
            }
            return material;
        }

        // One model for every wear state, no fragment roots: they were the stripped workbench's, and left pointing at
        // them WearNTear.Destroy throws after the refund, so the table could not be removed (Rune Table, 2026-10-22).
        private static void Point(WearNTear? wear, GameObject placed)
        {
            if (wear == null)
            {
                return;
            }
            (wear.m_new, wear.m_worn, wear.m_broken, wear.m_wet) = (placed, placed, placed, null);
            wear.m_fragmentRoots = null;
        }
    }
}
