using System;
using BundlePrefabs;
using EliteCrafting.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EliteCrafting.Tables
{
    /// <summary>
    /// The Rune Table's own model and build-menu icon, from the embedded bundle <c>ecf_runetable</c> (ValheimAssets
    /// <c>Assets/Props/RuneTable</c>: prefab <c>ecf_runetable</c> with its box colliders and an optional <c>glow</c> child,
    /// sprite <c>ecf_runetable_icon</c>). The workbench copy's own look and colliders go and the table's take their place,
    /// in the workbench's own material (its shader, so the game lights, wets and snows it like its pieces) wearing the
    /// table's baked maps; the glow keeps its own emissive material. Every peer, identically (the colliders are physics).
    /// A bundle that cannot load leaves the workbench's look.
    /// </summary>
    internal static class TableModel
    {
        public const string Bundle = "ecf_runetable";
        private const string Asset = "ecf_runetable";
        private const string GlowPart = "glow";

        // The workbench's body renderer (New/high, Workbench_mat on Custom/Piece). Its first renderer is the snow overlay
        // (floor_2x2_snow, Valheim/Snow Mesh), whose shader moves vertices: borrowed, it threw the table's planks about.
        private const string BodyRenderer = "high";
        private const float Gloss = 0.1f;

        private static AssetBundle? _bundle;
        private static bool _tried;

        /// <summary>The build menu icon; null without the bundle (the workbench's stays).</summary>
        public static Sprite? Icon { get; private set; }

        /// <summary>Dresses the fresh workbench copy as the table. False when the bundle or its model is missing.</summary>
        public static bool Wear(GameObject piece)
        {
            GameObject? model = Load()?.LoadAsset<GameObject>(Asset);
            if (model == null)
            {
                return false;
            }
            Material? wood = GameMaterials.Borrow(piece, BodyRenderer);
            Strip(piece);
            GameObject placed = Object.Instantiate(model, piece.transform, false);
            placed.name = "runetable";
            Dress(placed, wood);
            SetLayer(placed.transform, piece.layer);
            Point(piece.GetComponent<WearNTear>(), placed);
            Icon = _bundle!.LoadAsset<Sprite>(Asset + "_icon");
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
                _bundle = EmbeddedBundle.Load(typeof(TableModel).Assembly, Bundle);
            }
            catch (Exception e)
            {
                Log.Warn($"the Rune Table's model did not load, it keeps the workbench's look: {e.Message}");
            }
            return _bundle;
        }

        // Everything the workbench drew or collided with: its children (models, worn and broken states, snap points),
        // the root's own drawing parts and colliders, its level-of-detail group.
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

        private static void Dress(GameObject placed, Material? wood)
        {
            foreach (Renderer renderer in placed.GetComponentsInChildren<Renderer>(true))
            {
                if (IsGlow(renderer.transform, placed.transform))
                {
                    renderer.sharedMaterials = Array.ConvertAll(renderer.sharedMaterials, Glow);
                    continue;
                }
                if (wood == null)
                {
                    continue;
                }
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = materials[i] == null ? wood : GameMaterials.Plain(GameMaterials.Dress(wood, materials[i]), Gloss);
                }
                renderer.sharedMaterials = materials;
            }
        }

        // The glyphs' emissive material on the game's own Standard shader, so it never depends on a copy in the bundle.
        private static Material Glow(Material placeholder)
        {
            var material = new Material(placeholder);
            Shader? standard = Shader.Find("Standard");
            if (standard != null)
            {
                material.shader = standard;
                material.EnableKeyword("_EMISSION");
            }
            return material;
        }

        private static bool IsGlow(Transform part, Transform root)
        {
            for (Transform? t = part; t != null && t != root; t = t.parent)
            {
                if (t.name == GlowPart)
                {
                    return true;
                }
            }
            return false;
        }

        private static void SetLayer(Transform part, int layer)
        {
            part.gameObject.layer = layer;
            foreach (Transform child in part)
            {
                SetLayer(child, layer);
            }
        }

        // One model for every state: the table shows no worn or broken look, as several of the game's small pieces.
        private static void Point(WearNTear? wear, GameObject placed)
        {
            if (wear == null)
            {
                return;
            }
            wear.m_new = placed;
            wear.m_worn = placed;
            wear.m_broken = placed;
            wear.m_wet = null;
        }
    }
}
