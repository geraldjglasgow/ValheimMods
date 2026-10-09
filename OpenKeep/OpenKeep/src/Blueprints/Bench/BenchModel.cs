using System;
using BundlePrefabs;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OpenKeep.Blueprints.Bench
{
    /// <summary>
    /// The Blueprint Bench's own model and build-menu icon, from the embedded bundle <c>ok_blueprintbench</c>
    /// (ValheimAssets <c>Assets/Props/BlueprintBench</c>: prefab <c>ok_blueprintbench</c> with <c>model</c>, the glowing
    /// plan lines <c>glow_plan</c> and box colliders; sprite <c>ok_blueprintbench_icon</c>): a Norse drafting desk about
    /// half the workbench's size. The workbench copy's look and colliders go and the bench's take their place, in the
    /// workbench's own material (its shader, so the game lights, wets and snows it like its pieces) wearing the bench's
    /// baked maps; the glow keeps the bundle's emissive material. Every peer, identically (the colliders are physics).
    /// </summary>
    public static class BenchModel
    {
        public const string Bundle = "ok_blueprintbench";
        private const string Asset = "ok_blueprintbench";
        private const string GlowPart = "glow";
        private const float Gloss = 0.1f;
        // The workbench's own body renderer (New/high, Custom/Piece); its first renderer is a snow overlay whose shader
        // moves vertices and scrambles a model dressed in it.
        private const string BodyRenderer = "high";

        private static AssetBundle bundle;
        private static bool tried;

        /// <summary>The build menu icon; null without the bundle.</summary>
        public static Sprite Icon { get; private set; }

        /// <summary>Dresses the fresh workbench copy as the bench. False when the bundle or its model is missing.</summary>
        public static bool Wear(GameObject piece)
        {
            GameObject model = Load()?.LoadAsset<GameObject>(Asset);
            if (model == null)
                return false;
            Material wood = GameMaterials.Borrow(piece, BodyRenderer);
            Strip(piece);
            GameObject placed = Object.Instantiate(model, piece.transform, false);
            placed.name = "blueprintbench";
            Dress(placed, wood);
            SetLayer(placed.transform, piece.layer);
            Point(piece.GetComponent<WearNTear>(), placed);
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
                bundle = EmbeddedBundle.Load(typeof(BenchModel).Assembly, Bundle);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"OpenKeep: the Blueprint Bench's model did not load, it keeps the workbench's look at half size: {e.Message}");
            }
            return bundle;
        }

        // Everything the workbench drew or collided with: its children (models, worn and broken states, range circle,
        // player-base area), the root's own drawing parts and colliders, its level-of-detail group.
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
            foreach (Renderer renderer in placed.GetComponentsInChildren<Renderer>(true))
            {
                if (IsGlow(renderer.transform, placed.transform))
                {
                    renderer.sharedMaterials = Array.ConvertAll(renderer.sharedMaterials, m => m != null ? new Material(m) : null);
                    continue;
                }
                if (wood == null)
                    continue;
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                    materials[i] = materials[i] == null ? wood : GameMaterials.Plain(GameMaterials.Dress(wood, materials[i]), Gloss);
                renderer.sharedMaterials = materials;
            }
        }

        /// <summary>A part under a child named glow... keeps the bundle's own emissive material (its Standard shader draws in game).</summary>
        private static bool IsGlow(Transform part, Transform root)
        {
            for (Transform t = part; t != null && t != root; t = t.parent)
            {
                if (t.name.StartsWith(GlowPart, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static void SetLayer(Transform part, int layer)
        {
            part.gameObject.layer = layer;
            foreach (Transform child in part)
                SetLayer(child, layer);
        }

        // One model for every wear state. The workbench's destruction fragments were children stripped above: without
        // roots the game breaks the whole bench into fragments instead of throwing on the missing ones.
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
    }
}
