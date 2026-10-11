using System;
using System.Collections.Generic;
using BundlePrefabs;
using EliteCreaturesReborn.Util;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EliteCreaturesReborn.Raids
{
    /// <summary>
    /// The Raiders Chest's own model and build-menu icon, from the embedded bundle <c>ecr_raiders_chest</c> (ValheimAssets
    /// <c>Assets/Props/RaidersChest</c>, its BRIEF.md the contract: prefab <c>ecr_raiders_chest</c> with <c>model</c>, a
    /// <c>lid</c> pivoted on its hinge carrying the horn, the point <c>horn_mouth</c> at the horn's bell and three box
    /// colliders on its root; sprite <c>ecr_raiders_chest_icon</c>). The reinforced chest copy's look and colliders go and
    /// the model's take their place, dressed in the reinforced chest's own body material (<c>ironchest</c>, Custom/Piece,
    /// so the game lights, wets and snows it like its pieces) wearing the model's baked maps. The game's two lid copies are
    /// dropped: <see cref="ChestLid"/> turns the one lid. Every peer, identically. A bundle that cannot load leaves the
    /// reinforced chest's look and no raid controls (an ordinary coin chest), logged.
    /// </summary>
    internal static class ChestModel
    {
        public const string Bundle = "ecr_raiders_chest";

        /// <summary>The lid, its origin on the hinge, closed at rotation zero.</summary>
        public const string LidPart = "lid";

        /// <summary>The middle of the horn's bell, where the horn sounds from.</summary>
        public const string HornMouth = "horn_mouth";

        private const string Asset = "ecr_raiders_chest";
        private const string Placed = "raiderschest";
        private const float Gloss = 0.1f;

        // The reinforced chest's body renderer (Custom/Piece). Its first renderer is a snow overlay whose shader moves
        // vertices: borrowed, it throws the model's parts about (Mímir's Chest and the Rune Table, 2026-10-07).
        private const string ChestRenderer = "ironchest";

        private static AssetBundle? _bundle;
        private static bool _tried;

        /// <summary>The build menu icon; null without the bundle (the reinforced chest's stays).</summary>
        public static Sprite? Icon { get; private set; }

        /// <summary>Puts the model on the bench copy of the reinforced chest; false when the bundle is missing.</summary>
        public static bool Wear(GameObject piece, Container container)
        {
            AssetBundle? bundle = Load();
            GameObject? model = bundle != null ? bundle.LoadAsset<GameObject>(Asset) : null;
            if (bundle == null || model == null)
            {
                Log.Error("the Raiders Chest's model did not load: it keeps the reinforced chest's look and cannot sound a raid.");
                return false;
            }
            Material? body = GameMaterials.Borrow(piece, ChestRenderer);
            Strip(piece);
            GameObject placed = Object.Instantiate(model, piece.transform, false);
            placed.name = Placed;
            Dress(placed, body);
            SetLayer(placed.transform, piece.layer);
            Point(piece.GetComponent<WearNTear>(), container, placed);
            placed.AddComponent<ChestFace>();
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
                _bundle = EmbeddedBundle.Load(typeof(ChestModel).Assembly, Bundle);
            }
            catch (Exception e)
            {
                Log.Error($"the Raiders Chest's bundle did not load: {e.Message}");
            }
            return _bundle;
        }

        // Everything the game chest drew or collided with: its children (open and closed lids, the snow overlay), the
        // root's own drawing parts and colliders, its level-of-detail group.
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

        // Body and lid share one placeholder (one atlas), so they share one dressed material too.
        private static void Dress(GameObject placed, Material? body)
        {
            if (body == null)
            {
                Log.Warn("the reinforced chest's body material was not found: the Raiders Chest keeps the bundle's own.");
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
                material = GameMaterials.Plain(GameMaterials.Dress(body, placeholder), Gloss);
                dressed.Add(placeholder, material);
            }
            return material;
        }

        private static void SetLayer(Transform part, int layer)
        {
            part.gameObject.layer = layer;
            foreach (Transform child in part)
            {
                SetLayer(child, layer);
            }
        }

        // One model for every wear state, no fragment roots (they were the stripped models), no open/closed lid swap.
        private static void Point(WearNTear? wear, Container container, GameObject placed)
        {
            container.m_open = null;
            container.m_closed = null;
            if (wear == null)
            {
                return;
            }
            (wear.m_new, wear.m_worn, wear.m_broken, wear.m_wet) = (placed, placed, placed, null);
            wear.m_fragmentRoots = null;
        }
    }
}
