using System;
using System.Collections.Generic;
using BundlePrefabs;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Broadside
{
    /// <summary>
    /// An ammunition pile's workshop model, from its embedded bundle (<c>bs_pile_cannonballs</c>, <c>bs_pile_powder</c>;
    /// <c>.windows</c> and <c>.linux</c> in <c>Broadside/assets/bundles</c>, embedded as
    /// <c>Broadside.assets.&lt;bundle&gt;.&lt;platform&gt;</c>): the prefab of the bundle's name, with <c>model</c> (the frame or
    /// cradle), <c>fill_0</c> to <c>fill_9</c> (the ball groups or kegs, shown in order as it fills, <see cref="KwPileFill"/>;
    /// all hidden in the prefab, so a placement ghost and a new pile stand empty) and the box <c>col_box_body</c>, and the
    /// hammer icon <c>&lt;bundle&gt;_icon</c>. The chest copy is stripped (<see cref="KwPrefabs.Strip"/>) and the prefab goes
    /// whole under it as <see cref="LookName"/>, on the piece's layer, so its box is the piece's collider on every peer (a
    /// dedicated server loads the Linux bundle and draws nothing). On a screen every placeholder wears the wood chest's own
    /// material (<c>piece_chest_wood</c>'s body renderer <c>woodchest</c>, the game's piece shader) with the bundle's albedo
    /// and normal map, and the shine of iron where the placeholder's metal mask says. Aboard a launched ship the rider swaps
    /// those for the ship's copies (<see cref="KwShipMaterials"/>). Without the bundle the pile keeps the wood chest's look.
    /// </summary>
    public static class KwPileModel
    {
        public const string LookName = "pile", FillName = "fill_";
        private const string ChestBody = "woodchest";
        private const float Gloss = 0.1f, MetalGloss = 0.6f;
        private static readonly Dictionary<Material, Material> dressed = new Dictionary<Material, Material>();

        /// <summary>The pile wearing its bundle's model and icon; false (the chest's look kept) without the bundle.</summary>
        public static bool Wear(GameObject pile, GameObject chest, string bundle)
        {
            AssetBundle? source = Load(bundle);
            GameObject? model = source != null ? source.LoadAsset<GameObject>(bundle) : null;
            if (source == null || model == null)
            {
                if (source != null)
                    Log.Warn($"Broadside: the {bundle} bundle has no {bundle} prefab; {pile.name} wears the wood chest's look.");
                return false;
            }
            KwPrefabs.Strip(pile);
            GameObject look = Object.Instantiate(model, pile.transform, false);
            look.name = LookName;
            foreach (Transform part in look.GetComponentsInChildren<Transform>(true))
                part.gameObject.layer = pile.layer;
            KwPrefabs.Anchor(look);
            Worn(pile, look);
            Icon(pile, source, bundle);
            Empty(look);
            if (KwIcons.CanRender)
                Dress(look, chest);
            return true;
        }

        /// <summary>The model's fill steps, <c>fill_0</c> first; a step the model lacks is null.</summary>
        public static GameObject?[] Fills(Transform look)
        {
            GameObject?[] fills = new GameObject?[KwPileFill.Steps];
            for (int i = 0; i < fills.Length; i++)
                fills[i] = GameMaterials.Find(look, FillName + i)?.gameObject;
            return fills;
        }

        // The bundle, loaded once (EmbeddedBundle keeps it); null while the mod embeds none or it does not load, logged.
        private static AssetBundle? Load(string bundle)
        {
            if (!Array.Exists(typeof(KwPileModel).Assembly.GetManifestResourceNames(), r => r.StartsWith("Broadside.assets." + bundle + ".", StringComparison.Ordinal)))
            {
                Log.Warn($"Broadside: no {bundle} bundle embedded; that ammunition pile wears the wood chest's look.");
                return null;
            }
            try
            {
                return EmbeddedBundle.Load(typeof(KwPileModel).Assembly, bundle);
            }
            catch (Exception e)
            {
                Log.Warn($"Broadside: the {bundle} bundle did not load; that ammunition pile wears the wood chest's look: {e.Message}");
                return null;
            }
        }

        // The wear states all the model; no wet look, no fragments (the chest's went with its children: WearNTear.Destroy
        // would throw on them after refunding and before removing the piece).
        private static void Worn(GameObject pile, GameObject look)
        {
            WearNTear? wear = pile.GetComponent<WearNTear>();
            if (wear == null)
                return;
            wear.m_new = wear.m_worn = wear.m_broken = look;
            (wear.m_wet, wear.m_fragmentRoots) = (null, null);
        }

        private static void Icon(GameObject pile, AssetBundle source, string bundle)
        {
            Sprite? icon = source.LoadAsset<Sprite>(bundle + "_icon");
            if (icon != null)
                pile.GetComponent<Piece>().m_icon = icon;
            else
                Log.Warn($"Broadside: the {bundle} bundle has no {bundle}_icon; {pile.name} keeps the wood chest's icon.");
        }

        // Every fill step hidden in the prefab: a ghost and a new pile stand empty, each pile shows its own (KwPileFill).
        private static void Empty(GameObject look)
        {
            GameObject?[] fills = Fills(look.transform);
            int found = 0;
            foreach (GameObject? fill in fills)
            {
                if (fill == null)
                    continue;
                fill.SetActive(false);
                found++;
            }
            if (found < fills.Length)
                Log.Warn($"Broadside: the {look.name} model has {found} of {fills.Length} fill steps ({FillName}0 to {FillName}{fills.Length - 1}).");
        }

        // Every placeholder becomes the wood chest's material wearing its maps and metal mask, each made once; without it the placeholder stays.
        private static void Dress(GameObject look, GameObject chest)
        {
            Material? wood = GameMaterials.Borrow(chest, ChestBody);
            if (wood == null)
            {
                Log.Warn($"Broadside: no {ChestBody} material on {chest.name}; the ammunition pile wears its bundle's placeholder.");
                return;
            }
            foreach (Renderer renderer in look.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterials = Array.ConvertAll(renderer.sharedMaterials, m => m == null ? wood : Dressed(wood, m));
        }

        private static Material Dressed(Material wood, Material placeholder)
        {
            if (dressed.TryGetValue(placeholder, out Material made) && made != null)
                return made;
            Material plain = GameMaterials.Plain(GameMaterials.Dress(wood, placeholder), Gloss);
            return dressed[placeholder] = GameMaterials.Metal(plain, placeholder, MetalGloss);
        }
    }
}
