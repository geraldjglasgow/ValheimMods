using System;
using System.Collections.Generic;
using BundlePrefabs;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Broadside
{
    /// <summary>
    /// A dockside decoration's workshop model, from its embedded bundle (<c>bs_dock_&lt;id&gt;</c>; <c>.windows</c> and
    /// <c>.linux</c> in <c>Broadside/assets/bundles</c>, embedded as <c>Broadside.assets.&lt;bundle&gt;.&lt;platform&gt;</c>): the
    /// prefab of the bundle's name, with <c>model</c> (the body), its <c>col_*</c> boxes, the beacon's <c>glow_embers</c> and
    /// <c>fire</c> point and the vane's turning <c>vane</c> part, and the hammer icon <c>&lt;bundle&gt;_icon</c>. The prefab goes
    /// whole under the copied game piece as <see cref="LookName"/>, on the piece's layer, so its boxes are the piece's
    /// colliders on every peer (a dedicated server loads the Linux bundle and draws nothing); the base's own children,
    /// drawing parts and solid colliders go (<see cref="Strip"/>), after the def's <see cref="KwDockDef.Fit"/> took what it
    /// keeps. On a screen every placeholder wears the wood chest's material (<c>piece_chest_wood</c>'s body renderer
    /// <c>woodchest</c>, the game's piece shader) with the bundle's albedo and normal map and the shine of metal where the
    /// placeholder's metal mask says (<see cref="KwDockDef.MetalGloss"/>); a <c>glow_</c> part keeps its own emissive
    /// material, and a lantern's <c>glass</c> its own see-through one. Aboard a launched ship the rider swaps those for the ship's copies (<see cref="KwShipMaterials"/>). A piece
    /// that hangs (its <see cref="KwDecorFit.Hangs"/>: the tool wall, the hanging net, the fenders) is placed by a box in
    /// front of its origin, so its back lies on the wall it is aimed at; one with nothing solid (the shavings) by a box on
    /// the non-solid layer, so nobody trips on it.
    /// </summary>
    public static class KwDockModel
    {
        public const string LookName = "dock";
        private const string GlowPrefix = "glow_", GlassPrefix = "glass", NonSolid = "piece_nonsolid";
        private const float Gloss = 0.1f;
        private static readonly Dictionary<(Material, string), Material> dressed = new Dictionary<(Material, string), Material>();

        /// <summary>The bundle's prefab and icon; null (logged) when the mod embeds no such bundle, it does not load or lacks the prefab.</summary>
        public static (GameObject Model, Sprite? Icon)? Load(string bundle)
        {
            AssetBundle? source = Open(bundle);
            GameObject? model = source != null ? source.LoadAsset<GameObject>(bundle) : null;
            if (source == null)
                return null;
            if (model == null)
            {
                Log.Warn($"Broadside: the {bundle} bundle has no {bundle} prefab; that decoration is left out.");
                return null;
            }
            Sprite? icon = source.LoadAsset<Sprite>(bundle + "_icon");
            if (icon == null)
                Log.Warn($"Broadside: the {bundle} bundle has no {bundle}_icon; that decoration keeps its base's icon.");
            return (model, icon);
        }

        /// <summary>The piece wearing the model (made here, returned), stripped of its base's look, placed by its origin.</summary>
        public static GameObject Wear(GameObject piece, GameObject model, Sprite? icon, KwDockDef def, Material? wood)
        {
            GameObject look = Object.Instantiate(model, piece.transform, false);
            look.name = LookName;
            foreach (Transform part in look.GetComponentsInChildren<Transform>(true))
                part.gameObject.layer = piece.layer;
            // dressed before Fit moves anything of the base's in (the beacon's flames keep their own materials and layers)
            if (wood != null)
                Dress(look.transform, wood, def.MetalGloss, KwDockTiers.Albedo(def));
            def.Fit?.Invoke(piece, look);
            Strip(piece, look);
            KwPrefabs.Anchor(look);
            Underfoot(look);
            if (def.Decor != null && def.Decor.Hangs)
                Flush(look);
            Worn(piece, look);
            if (icon != null)
                piece.GetComponent<Piece>().m_icon = icon;
            return look;
        }

        /// <summary>
        /// The shavings' flat box (<see cref="KwDockDef.Fit"/>): their model has no collider, so a box 2 cm tall over its
        /// footprint (1.9 by 1.86 m, as the mesh lies) is what the hammer aims at to remove them; nobody trips on it (<see cref="HitBox"/>).
        /// </summary>
        public static void FlatBox(GameObject piece, GameObject look) =>
            HitBox(look.transform, "hit_floor", new Vector3(0f, 0.01f, -0.03f), new Vector3(1.9f, 0.02f, 1.86f));

        /// <summary>
        /// A box for aiming at a part with no collider of its own, on the game's <c>piece_nonsolid</c> layer: characters and
        /// dropped items pass through it, while the hammer's, the placing and the hover rays hit it.
        /// </summary>
        public static void HitBox(Transform parent, string name, Vector3 centre, Vector3 size)
        {
            GameObject box = new GameObject(name) { layer = LayerMask.NameToLayer(NonSolid) };
            box.transform.SetParent(parent, false);
            BoxCollider collider = box.AddComponent<BoxCollider>();
            (collider.center, collider.size) = (centre, size);
        }

        // A piece with nothing solid of its own (the shavings, lying in paths) has its 10 cm placing box on the non-solid layer
        // as well, so nobody trips on it; the ghost, the hammer's rays and the wear's ground check still find it.
        private static void Underfoot(GameObject look)
        {
            Transform? place = look.transform.Find(KwPrefabs.PlaceName);
            int open = LayerMask.NameToLayer(NonSolid);
            foreach (Collider collider in look.GetComponentsInChildren<Collider>(true))
            {
                if (collider.transform != place && !collider.isTrigger && collider.gameObject.layer != open)
                    return;
            }
            if (place != null)
                place.gameObject.layer = open;
        }

        // A hung piece's placing box in front of its origin, its back face on it: aimed at a wall, the piece's back lies on the
        // wall's face at the aimed height (a box round the origin would hold it off by half the box, 5 cm).
        private static void Flush(GameObject look)
        {
            BoxCollider? place = look.transform.Find(KwPrefabs.PlaceName)?.GetComponent<BoxCollider>();
            if (place != null)
                place.center += new Vector3(0f, 0f, place.size.z / 2f);
        }

        // The bundle, loaded once (EmbeddedBundle keeps it); null while the mod embeds none or it does not load, logged.
        private static AssetBundle? Open(string bundle)
        {
            if (!Array.Exists(typeof(KwDockModel).Assembly.GetManifestResourceNames(), r => r.StartsWith("Broadside.assets." + bundle + ".", StringComparison.Ordinal)))
            {
                Log.Warn($"Broadside: no {bundle} bundle embedded; that decoration is left out.");
                return null;
            }
            try
            {
                return EmbeddedBundle.Load(typeof(KwDockModel).Assembly, bundle);
            }
            catch (Exception e)
            {
                Log.Warn($"Broadside: the {bundle} bundle did not load; that decoration is left out: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Everything of the base but the model, as <see cref="KwPrefabs.Strip"/> takes it: its other children, the root's
        /// drawing parts, colliders and level-of-detail group; but a trigger on the root stays (an effect area works by it).
        /// </summary>
        private static void Strip(GameObject piece, GameObject look)
        {
            for (int i = piece.transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = piece.transform.GetChild(i).gameObject;
                if (child != look)
                    Object.DestroyImmediate(child);
            }
            foreach (Type kind in new[] { typeof(LODGroup), typeof(Renderer), typeof(MeshFilter) })
            {
                foreach (Component part in piece.GetComponents(kind))
                    Object.DestroyImmediate(part);
            }
            foreach (Collider collider in piece.GetComponents<Collider>())
            {
                if (!collider.isTrigger)
                    Object.DestroyImmediate(collider);
            }
        }

        // The wear states all the model; no wet look, no fragments (the base's went with its children: WearNTear.Destroy
        // would throw on them after refunding and before removing the piece; null fragments the piece itself).
        private static void Worn(GameObject piece, GameObject look)
        {
            WearNTear wear = piece.GetComponent<WearNTear>();
            wear.m_new = wear.m_worn = wear.m_broken = look;
            (wear.m_wet, wear.m_fragmentRoots) = (null, null);
        }

        // Every placeholder becomes the wood chest's material wearing its maps and metal mask (a wood tier's copy its tier's
        // albedo, KwDockTiers), each made once; a glow part keeps its own emissive material, and a lantern's glass its own
        // see-through one as the bundle built it (Standard's transparent mode, its shader in the bundle), casting no shadow.
        private static void Dress(Transform look, Material wood, float metalGloss, Texture2D? albedo)
        {
            foreach (Renderer renderer in look.GetComponentsInChildren<Renderer>(true))
            {
                bool glow = Under(renderer.transform, look, GlowPrefix), glass = Under(renderer.transform, look, GlassPrefix);
                renderer.sharedMaterials = Array.ConvertAll(renderer.sharedMaterials, m => m == null ? wood : glass ? m : glow ? Glow(m) : Dressed(wood, m, metalGloss, albedo));
                if (glass)
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        private static bool Under(Transform part, Transform look, string prefix)
        {
            for (Transform? at = part; at != null && at != look; at = at.parent)
            {
                if (at.name.StartsWith(prefix, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static Material Dressed(Material wood, Material placeholder, float metalGloss, Texture2D? albedo)
        {
            (Material, string) key = (placeholder, albedo != null ? albedo.name : "");
            if (dressed.TryGetValue(key, out Material made) && made != null)
                return made;
            Material plain = GameMaterials.Plain(GameMaterials.Dress(wood, placeholder), Gloss);
            if (albedo != null)
                (plain.mainTexture, plain.name) = (albedo, albedo.name);
            return dressed[key] = metalGloss > 0f ? GameMaterials.Metal(plain, placeholder, metalGloss) : plain;
        }

        // The bundle's own emissive material (its Standard shader, built into the bundle, draws in game), glow on.
        private static Material Glow(Material placeholder)
        {
            if (dressed.TryGetValue((placeholder, GlowPrefix), out Material made) && made != null)
                return made;
            Material glow = new Material(placeholder) { name = placeholder.name };
            glow.EnableKeyword("_EMISSION");
            return dressed[(placeholder, GlowPrefix)] = glow;
        }
    }
}
