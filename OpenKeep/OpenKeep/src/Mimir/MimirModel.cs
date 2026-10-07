using System;
using BundlePrefabs;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OpenKeep.Mimir
{
    /// <summary>
    /// Mímir's Chest's own model and build-menu icon, from the embedded bundle <c>ok_mimirchest</c> (ValheimAssets
    /// <c>Assets/Props/MimirsChest</c>: prefab <c>ok_mimirchest</c> with <c>model</c>, a <c>lid</c> pivoted on its hinge
    /// and box colliders; sprite <c>ok_mimirchest_icon</c>). The reinforced chest copy's look and colliders go and the
    /// chest's take their place, in the game chest's own material (its shader, so the game lights, wets and snows it like
    /// its pieces) wearing the chest's baked maps. The game's open and closed models are dropped (the lid moves instead).
    /// Every peer, identically. A bundle that cannot load leaves the reinforced chest's look.
    /// </summary>
    public static class MimirModel
    {
        public const string Bundle = "ok_mimirchest";
        private const string Asset = "ok_mimirchest";
        private const float Gloss = 0.1f;
        // The reinforced chest's own body renderer (Custom/Piece). Its first renderer is a snow overlay
        // (floor_2x2_snow, Valheim/Snow Mesh) whose shader moves vertices: borrowed, it threw the lid out as a huge
        // slab and scrambled the texture.
        private const string ChestRenderer = "ironchest";

        private static AssetBundle bundle;
        private static bool tried;

        /// <summary>The build menu icon; null without the bundle (the reinforced chest's stays).</summary>
        public static Sprite Icon { get; private set; }

        public static bool Wear(GameObject piece, Container container)
        {
            GameObject model = Load()?.LoadAsset<GameObject>(Asset);
            if (model == null)
                return false;
            Material wood = GameMaterials.Borrow(piece, ChestRenderer);
            Strip(piece);
            GameObject placed = Object.Instantiate(model, piece.transform, false);
            placed.name = "mimirchest";
            Dress(placed, wood);
            SetLayer(placed.transform, piece.layer);
            Point(piece.GetComponent<WearNTear>(), container, placed);
            piece.AddComponent<MimirLid>();
            Icon = bundle.LoadAsset<Sprite>(Asset + "_icon");
            return true;
        }

        private static AssetBundle Load()
        {
            if (tried)
                return bundle;
            tried = true;
            try
            {
                bundle = EmbeddedBundle.Load(typeof(MimirModel).Assembly, Bundle);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"OpenKeep: Mímir's Chest's model did not load, it keeps the reinforced chest's look: {e.Message}");
            }
            return bundle;
        }

        // Everything the game chest drew or collided with: its children (open and closed models, snap points), the root's
        // own drawing parts and colliders, its level-of-detail group.
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
                    materials[i] = materials[i] == null ? wood : GameMaterials.Plain(GameMaterials.Dress(wood, materials[i]), Gloss);
                renderer.sharedMaterials = materials;
            }
        }

        private static void SetLayer(Transform part, int layer)
        {
            part.gameObject.layer = layer;
            foreach (Transform child in part)
                SetLayer(child, layer);
        }

        // One model for every wear state and no open/closed swap: the lid is moved by MimirLid instead.
        private static void Point(WearNTear wear, Container container, GameObject placed)
        {
            container.m_open = null;
            container.m_closed = null;
            if (wear == null)
                return;
            wear.m_new = placed;
            wear.m_worn = placed;
            wear.m_broken = placed;
            wear.m_wet = null;
        }
    }
}
