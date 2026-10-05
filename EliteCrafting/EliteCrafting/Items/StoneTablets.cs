using System;
using System.Collections.Generic;
using BundlePrefabs;
using EliteCrafting.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EliteCrafting.Items
{
    /// <summary>
    /// The rune tablets (prefabs.md section 4): each rune's own model and icon from the embedded bundle
    /// <c>ecf_runes</c> (ValheimAssets <c>Assets/Items/RuneTablets</c>, asset <c>ecf_runetablet_&lt;id&gt;</c>, icon
    /// <c>ecf_runetablet_&lt;id&gt;_icon</c>): a small chipped stone tablet with the rune's glyph cut into its top and
    /// coloured. The cloned base keeps its item parts (net view, rigidbody, sync, sounds, item data); its own look, lights
    /// and colliders go, and the tablet goes where its model was (under <c>attach</c> when it has one), with the tablet's
    /// box collider. It wears a game item's opaque material (<see cref="MaterialItem"/>'s, as the Kraken beak does; the
    /// Ruby's own is see-through) with the tablet's baked maps, so the game lights it like its own items. Every peer,
    /// identically (the collider is physics); a bundle that cannot load leaves the base's own look, tinted as before.
    /// </summary>
    internal static class StoneTablets
    {
        public const string Bundle = "ecf_runes";

        /// <summary>The base every rune is copied from while the tablets load: the Ruby's group (a plain small item).</summary>
        public const StoneGroup BaseGroup = StoneGroup.Ascension;

        /// <summary>The game item whose material the tablets wear: opaque Standard with a normal map.</summary>
        public const string MaterialItem = "SerpentScale";

        private const string Holder = "attach";
        private const string ModelName = "model";
        private const float Gloss = 0.15f;

        private static AssetBundle? _bundle;
        private static Material? _game;
        private static bool _tried;

        /// <summary>Loads the bundle once per process; false (with a warning) when it cannot load.</summary>
        public static bool Load(GameObject? materialItem)
        {
            if (!_tried)
            {
                _tried = true;
                try
                {
                    _bundle = EmbeddedBundle.Load(typeof(StoneTablets).Assembly, Bundle);
                    _game = GameMaterials.Borrow(materialItem);
                }
                catch (Exception e)
                {
                    Log.Warn($"the rune tablets did not load, runes keep the base items' look: {e.Message}");
                }
            }
            return _bundle != null;
        }

        /// <summary>Dresses one freshly cloned rune prefab as its tablet. False (with a warning) when the bundle lacks it.</summary>
        public static bool Wear(StoneEntry entry)
        {
            string asset = "ecf_runetablet_" + entry.BuiltInId;
            GameObject? model = _bundle?.LoadAsset<GameObject>(asset);
            if (model == null)
            {
                Log.Warn($"the rune bundle has no {asset}; {entry.PrefabName} keeps its base item's look");
                return false;
            }
            Material? look = Look(model);
            Strip(entry.Prefab);
            Place(entry.Prefab, model, look);
            Sprite? icon = _bundle!.LoadAsset<Sprite>(asset + "_icon");
            if (icon != null)
            {
                entry.Shared.m_icons = new[] { icon };
            }
            return true;
        }

        /// <summary>The game material wearing the tablet's baked albedo and normal map, or null (keep the bundle's own).</summary>
        private static Material? Look(GameObject model)
        {
            Renderer? renderer = model.GetComponentInChildren<Renderer>(true);
            if (_game == null || renderer == null || renderer.sharedMaterial == null)
            {
                return null;
            }
            return GameMaterials.Plain(GameMaterials.Dress(_game, renderer.sharedMaterial), Gloss);
        }

        /// <summary>Removes the base's colliders and every part that draws or lights it (models, glows, sparkles).</summary>
        private static void Strip(GameObject item)
        {
            foreach (Collider collider in item.GetComponentsInChildren<Collider>(true))
            {
                Object.DestroyImmediate(collider);
            }
            Transform holder = HolderOf(item);
            foreach (GameObject part in LookParts(item))
            {
                if (part == null)
                {
                    continue;
                }
                if (part == item || part.transform == holder)
                {
                    DropDrawing(part);
                    continue;
                }
                Object.DestroyImmediate(part);
            }
        }

        // A particle system before its renderer: Unity refuses to remove a renderer a particle system still needs.
        private static readonly Type[] Drawing =
            { typeof(ParticleSystem), typeof(LODGroup), typeof(Light), typeof(Renderer), typeof(MeshFilter) };

        private static List<GameObject> LookParts(GameObject item)
        {
            var parts = new List<GameObject>();
            foreach (Type kind in Drawing)
            {
                foreach (Component c in item.GetComponentsInChildren(kind, true))
                {
                    parts.Add(c.gameObject);
                }
            }
            return parts;
        }

        // The root and the holder stay (they carry the item and the attach point); only their own drawing goes.
        private static void DropDrawing(GameObject part)
        {
            foreach (Type kind in Drawing)
            {
                foreach (Component c in part.GetComponents(kind))
                {
                    Object.DestroyImmediate(c);
                }
            }
        }

        /// <summary>The tablet as the item's model: centred on the holder, on the item's layer, in the dressed material.</summary>
        private static void Place(GameObject item, GameObject model, Material? look)
        {
            Transform holder = HolderOf(item);
            GameObject copy = Object.Instantiate(model, holder, false);
            copy.name = ModelName;
            if (look != null)
            {
                GameMaterials.Apply(copy, placeholder => look);
            }
            foreach (Transform part in copy.GetComponentsInChildren<Transform>(true))
            {
                part.gameObject.layer = item.layer;
            }
            copy.transform.localPosition = -ModelBounds.In(copy, holder).center;
        }

        private static Transform HolderOf(GameObject item) => item.transform.Find(Holder) ?? item.transform;
    }
}
