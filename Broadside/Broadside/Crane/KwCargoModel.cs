using System;
using System.Collections.Generic;
using BundlePrefabs;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Broadside
{
    /// <summary>
    /// The crane's and the crate's workshop models (ValheimAssets <c>Assets/Ships/Keelwright/CargoCrane</c> and
    /// <c>CargoCrate</c>), from their embedded bundles: the bundle's prefab goes whole under the stripped piece as its
    /// <c>model</c> child, on the piece's layer, so its <c>col_*</c> boxes are the piece's colliders on every peer (a
    /// dedicated server loads the Linux bundle and draws nothing). On a screen each placeholder material becomes the game
    /// piece's body material wearing the bundle's maps; whatever moves (the crane's turret and head, the whole crate) wears
    /// copies set as the game's ships are, so the building shader's world-space noise does not slide across it. Also the
    /// game's rope (the longship's) for the line renderers.
    /// </summary>
    public static class KwCargoModel
    {
        private const float Gloss = 0.1f, RopeWidth = 0.035f;
        private static readonly Dictionary<string, GameObject?> sources = new Dictionary<string, GameObject?>();
        private static readonly Dictionary<Material, Material> moving = new Dictionary<Material, Material>();

        /// <summary>Whether this game draws (a dedicated server does not).</summary>
        public static bool Screens => SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;

        /// <summary>The game's rope material, found once; null if the game has none.</summary>
        public static Material? Rope { get; private set; }

        /// <summary>A bundle's prefab of the same name, loaded once; a bundle that does not load is logged once.</summary>
        public static GameObject? Source(string bundle)
        {
            if (sources.TryGetValue(bundle, out GameObject? source))
                return source;
            try
            {
                source = EmbeddedBundle.Prefab(EmbeddedBundle.Load(typeof(KwCargoModel).Assembly, bundle), bundle);
            }
            catch (Exception e)
            {
                Log.Error($"Broadside: the {bundle} model did not load, so its piece is not built: {e.Message}");
            }
            return sources[bundle] = source;
        }

        /// <summary>The bundle's hammer icon (the sprite <c>&lt;bundle&gt;_icon</c>), or null: the base piece's icon stays.</summary>
        public static Sprite? Icon(string bundle)
        {
            try
            {
                return EmbeddedBundle.Load(typeof(KwCargoModel).Assembly, bundle).LoadAsset<Sprite>(bundle + "_icon");
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>The bundle's prefab under the piece as its model, the piece's wear states pointed at it.</summary>
        public static GameObject Wear(GameObject piece, GameObject source)
        {
            GameObject model = Object.Instantiate(source, piece.transform, false);
            model.name = "model";
            foreach (Transform part in model.GetComponentsInChildren<Transform>(true))
                part.gameObject.layer = piece.layer;
            WearNTear? wear = piece.GetComponent<WearNTear>();
            if (wear != null)
            {
                wear.m_new = wear.m_worn = wear.m_broken = model;
                wear.m_wet = null;
                wear.m_fragmentRoots = null;
            }
            return model;
        }

        /// <summary>On a screen: every placeholder becomes the game piece's body material with its maps; under <paramref name="moves"/> (or all, when null) the moving copy.</summary>
        public static void Dress(GameObject model, GameObject? gamePiece, string bodyRenderer, Func<Renderer, bool>? moves)
        {
            Material? body = Screens ? GameMaterials.Borrow(gamePiece, bodyRenderer) : null;
            if (body == null)
            {
                if (Screens)
                    Log.Warn($"Broadside: no {bodyRenderer} material on {gamePiece?.name}; {model.transform.parent?.name} wears its bundle's placeholder.");
                return;
            }
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                bool moved = moves == null || moves(renderer);
                renderer.sharedMaterials = Array.ConvertAll(renderer.sharedMaterials, m => Dressed(body, m, moved));
            }
        }

        private static Material Dressed(Material body, Material? placeholder, bool moved)
        {
            Material dressed = placeholder == null ? body : GameMaterials.Plain(GameMaterials.Dress(body, placeholder), Gloss);
            return moved ? Moving(dressed) : dressed;
        }

        // A copy set as the game's ships are: no world-space value noise or ripple, the moveable-object switch on.
        private static Material Moving(Material source)
        {
            if (!source.HasProperty("_ValueNoise") || !source.HasProperty("_MoveableObject"))
                return source;
            if (moving.TryGetValue(source, out Material made) && made != null)
                return made;
            Material copy = new Material(source) { name = source.name + " (moving)" };
            copy.SetFloat("_ValueNoise", 0f);
            copy.SetFloat("_ValueNoiseVertex", 0f);
            copy.DisableKeyword("_VALUENOISEVERTEX_ON");
            copy.SetFloat("_RippleDistance", 0f);
            copy.SetFloat("_MoveableObject", 1f);
            copy.EnableKeyword("_MOVEABLEOBJECT_ON");
            return moving[source] = copy;
        }

        /// <summary>On a screen: a line renderer child in the game's rope, in world space, its points set as it moves; null on a server.</summary>
        public static LineRenderer? Line(Transform parent, string name, ZNetScene scene)
        {
            if (!Screens)
                return null;
            Rope ??= FindRope(scene);
            GameObject go = new GameObject(name) { layer = parent.gameObject.layer };
            go.transform.SetParent(parent, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.widthMultiplier = RopeWidth;
            line.textureMode = LineTextureMode.Tile;
            line.numCapVertices = 2;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.positionCount = 0;
            if (Rope != null)
                line.sharedMaterial = Rope;
            return line;
        }

        // The rope the longship's ropes are drawn in: a line renderer's material.
        private static Material? FindRope(ZNetScene scene)
        {
            GameObject? ship = scene.GetPrefab("VikingShip");
            foreach (LineRenderer line in ship != null ? ship.GetComponentsInChildren<LineRenderer>(true) : new LineRenderer[0])
            {
                if (line.sharedMaterial != null && line.sharedMaterial.name.ToLowerInvariant().Contains("rope"))
                    return line.sharedMaterial;
            }
            return null;
        }
    }
}
