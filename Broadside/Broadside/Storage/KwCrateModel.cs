using System;
using System.Collections.Generic;
using BundlePrefabs;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Broadside
{
    /// <summary>
    /// A storage crate's workshop model (ValheimAssets <c>Assets/Ships/Keelwright/StorageCrates</c>), from its embedded
    /// bundle (<c>bs_crate_small</c>, <c>bs_crate_large</c>; <c>.windows</c> and <c>.linux</c> in
    /// <c>Broadside/assets/bundles</c>, embedded as <c>Broadside.assets.&lt;bundle&gt;.&lt;platform&gt;</c>): the prefab of the
    /// bundle's name, with <c>model</c> (the body), <c>lid</c> (on its hinge, <see cref="KwCrateLid"/>) and the box
    /// <c>col_box_body</c>, and the hammer icon <c>&lt;bundle&gt;_icon</c>. The chest copy is stripped
    /// (<see cref="KwPrefabs.Strip"/>) and the prefab goes whole under it as <see cref="LookName"/>, on the piece's layer,
    /// so its box is the piece's collider on every peer (a dedicated server loads the Linux bundle and draws nothing). On
    /// a screen it wears the wood chest's own material (<c>piece_chest_wood</c>'s body renderer <c>woodchest</c>, the
    /// game's piece shader; its first renderer may be a snow overlay) with the bundle's albedo and normal map. Aboard a
    /// launched ship the rider swaps those for the ship's copies (<see cref="KwShipMaterials"/>).
    /// </summary>
    public static class KwCrateModel
    {
        public const string LookName = "crate";
        private const string ChestBody = "woodchest";
        private const float Gloss = 0.1f;
        private static readonly Dictionary<Material, Material> dressed = new Dictionary<Material, Material>();

        /// <summary>
        /// The crate wearing its bundle's model and icon; false (the chest's look kept) without the bundle. A crate with
        /// marks of its own (<paramref name="moving"/>) wears the materials' ship copies everywhere (no world-laid noise),
        /// so its own copies of them are never copied again when a ship carries it.
        /// </summary>
        public static bool Wear(GameObject crate, GameObject chest, string bundle, bool moving)
        {
            AssetBundle? source = Load(bundle);
            GameObject? model = source != null ? source.LoadAsset<GameObject>(bundle) : null;
            if (source == null || model == null)
            {
                if (source != null)
                    Log.Warn($"Broadside: the {bundle} bundle has no {bundle} prefab; {crate.name} wears the wood chest's look.");
                return false;
            }
            KwPrefabs.Strip(crate);
            GameObject look = Object.Instantiate(model, crate.transform, false);
            look.name = LookName;
            foreach (Transform part in look.GetComponentsInChildren<Transform>(true))
                part.gameObject.layer = crate.layer;
            KwPrefabs.Anchor(look);
            Worn(crate, look);
            Icon(crate, source, bundle);
            if (KwIcons.CanRender)
                Dress(look, chest, moving);
            return true;
        }

        // The bundle, loaded once (EmbeddedBundle keeps it); null while the mod embeds none or it does not load, logged.
        private static AssetBundle? Load(string bundle)
        {
            if (!Array.Exists(typeof(KwCrateModel).Assembly.GetManifestResourceNames(), r => r.StartsWith("Broadside.assets." + bundle + ".", StringComparison.Ordinal)))
            {
                Log.Warn($"Broadside: no {bundle} bundle embedded; that storage crate wears the wood chest's look.");
                return null;
            }
            try
            {
                return EmbeddedBundle.Load(typeof(KwCrateModel).Assembly, bundle);
            }
            catch (Exception e)
            {
                Log.Warn($"Broadside: the {bundle} bundle did not load; that storage crate wears the wood chest's look: {e.Message}");
                return null;
            }
        }

        // The wear states all the model; no wet look, no fragments (the chest's went with its children: WearNTear.Destroy
        // would throw on them after refunding and before removing the piece).
        private static void Worn(GameObject crate, GameObject look)
        {
            WearNTear? wear = crate.GetComponent<WearNTear>();
            if (wear == null)
                return;
            wear.m_new = wear.m_worn = wear.m_broken = look;
            (wear.m_wet, wear.m_fragmentRoots) = (null, null);
        }

        private static void Icon(GameObject crate, AssetBundle source, string bundle)
        {
            Sprite? icon = source.LoadAsset<Sprite>(bundle + "_icon");
            if (icon != null)
                crate.GetComponent<Piece>().m_icon = icon;
            else
                Log.Warn($"Broadside: the {bundle} bundle has no {bundle}_icon; {crate.name} keeps the wood chest's icon.");
        }

        // Every placeholder becomes the wood chest's material wearing its maps, each made once; without it the placeholder stays.
        private static void Dress(GameObject look, GameObject chest, bool moving)
        {
            Material? wood = GameMaterials.Borrow(chest, ChestBody);
            if (wood == null)
            {
                Log.Warn($"Broadside: no {ChestBody} material on {chest.name}; the storage crate wears its bundle's placeholder.");
                return;
            }
            foreach (Renderer renderer in look.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterials = Array.ConvertAll(renderer.sharedMaterials, m => Moving(m == null ? wood : Dressed(wood, m), moving));
        }

        private static Material Moving(Material material, bool moving) => moving ? KwShipMaterials.Ship(material) : material;

        private static Material Dressed(Material wood, Material placeholder)
        {
            if (dressed.TryGetValue(placeholder, out Material made) && made != null)
                return made;
            return dressed[placeholder] = GameMaterials.Plain(GameMaterials.Dress(wood, placeholder), Gloss);
        }
    }
}
