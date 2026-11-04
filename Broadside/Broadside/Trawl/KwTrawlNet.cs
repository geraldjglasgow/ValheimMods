using System;
using System.Collections.Generic;
using BundlePrefabs;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Broadside
{
    /// <summary>
    /// The towed trawl net's model: the workshop's boat-towed trawl net (<c>ValheimAssets/Assets/Props/TrawlNet</c>, v001;
    /// its bundle copied in as <c>bs_trawlnet.windows</c> and <c>.linux</c>, embedded as <c>Broadside.assets.bs_trawlnet.&lt;platform&gt;</c>;
    /// the prefab keeps its contract name <see cref="Prefab"/>): a static deployed net, 5.7 by 2.7 by 11.4 m, the mouth's
    /// middle at its origin, +Z into the bag, -Z toward the boat, its tow eye the child <see cref="Attach"/> at
    /// (0, 0.45, -4.5); meshes <c>frame</c> (boards and floats), <c>netting</c>, <c>ropes</c>, <c>tow_ropes</c> and
    /// <c>weights</c>; no colliders. Dressed as its README says: the frame in the game's own log wall (<c>wood_wall_log</c>'s
    /// <c>logwall</c>, <c>Custom/Piece</c>), every other part in a copy of it wearing its own hemp or stone maps, white, its
    /// metal cleared, gloss 0.08; then the ship's copies of those materials (<see cref="KwShipMaterials"/>), since it moves
    /// with a ship. Made once where there is a screen, as an inactive template every net is a copy of; a dedicated server
    /// loads nothing. Without the bundle no net is drawn (the winch still works) and the drum's roll wears plank wood.
    /// </summary>
    public static class KwTrawlNet
    {
        public const string Bundle = "bs_trawlnet", Prefab = "va_trawlnet_v001", Attach = "tow_attach";
        private const string WallPiece = "wood_wall_log", WallMaterial = "logwall", Frame = "frame", Netting = "netting";
        private const float Gloss = 0.08f;
        private static readonly Dictionary<Material, Material> dressed = new Dictionary<Material, Material>();
        private static GameObject? template;

        /// <summary>The tow eye in the net's own frame (the contract's, read from the model when it loads).</summary>
        public static Vector3 TowLocal { get; private set; } = new Vector3(0f, 0.45f, -4.5f);

        /// <summary>The netting's dressed material (the drum's roll of net wears it), once loaded.</summary>
        public static Material? Hemp { get; private set; }

        /// <summary>Once, before the winch is made, where there is a screen.</summary>
        public static void Load(ZNetScene scene)
        {
            if (template != null || !KwShipBody.Drawn)
                return;
            try
            {
                GameObject? model = Model();
                if (model != null)
                    template = Template(scene, model);
            }
            catch (Exception e)
            {
                Log.Warn($"Broadside: the trawl net's bundle did not load; no net is drawn: {e.Message}");
            }
        }

        /// <summary>A net in the world (no parent), drawn, never networked; null without the model.</summary>
        public static GameObject? Make()
        {
            if (template == null)
                return null;
            GameObject net = Object.Instantiate(template);
            net.name = Bundle;
            return net;
        }

        private static GameObject? Model()
        {
            if (!Array.Exists(typeof(KwTrawlNet).Assembly.GetManifestResourceNames(), r => r.StartsWith("Broadside.assets." + Bundle + ".", StringComparison.Ordinal)))
            {
                Log.Warn($"Broadside: no {Bundle} bundle embedded; no trawl net is drawn.");
                return null;
            }
            GameObject? model = EmbeddedBundle.Load(typeof(KwTrawlNet).Assembly, Bundle).LoadAsset<GameObject>(Prefab);
            if (model == null)
                Log.Warn($"Broadside: the {Bundle} bundle has no {Prefab} prefab; no trawl net is drawn.");
            return model;
        }

        // The model copied onto the bench, its tow eye read, dressed in the game's log wall, then in the ship's copies.
        private static GameObject Template(ZNetScene scene, GameObject model)
        {
            GameObject copy = PrefabBench.Copy(model, Bundle);
            Transform? eye = GameMaterials.Find(copy.transform, Attach);
            if (eye != null)
                TowLocal = copy.transform.InverseTransformPoint(eye.position);
            int layer = LayerMask.NameToLayer("piece");
            foreach (Transform part in copy.GetComponentsInChildren<Transform>(true))
                part.gameObject.layer = layer;
            Material? wall = Wall(scene);
            if (wall != null)
                Dress(copy, wall);
            else
                Log.Warn($"Broadside: no {WallMaterial} material on {WallPiece}; the trawl net wears its bundle's placeholders.");
            KwShipMaterials.Apply(copy.transform);
            Log.Info($"Broadside: the trawl net loaded, its tow eye at {TowLocal}.");
            return copy;
        }

        // The game's log wall material (Custom/Piece), found by name among the wall's renderers (never the first one: a snow overlay).
        private static Material? Wall(ZNetScene scene)
        {
            GameObject? wall = scene.GetPrefab(WallPiece);
            foreach (Renderer renderer in wall != null ? wall.GetComponentsInChildren<Renderer>(true) : new Renderer[0])
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material != null && material.name == WallMaterial && material.shader.name == "Custom/Piece")
                        return material;
                }
            }
            return null;
        }

        private static void Dress(GameObject net, Material wall)
        {
            foreach (MeshRenderer renderer in net.GetComponentsInChildren<MeshRenderer>(true))
            {
                Material? placeholder = renderer.sharedMaterial;
                Material worn = renderer.name == Frame || placeholder == null ? wall : Dressed(wall, placeholder);
                renderer.sharedMaterial = worn;
                if (renderer.name == Netting)
                    Hemp = worn;
            }
        }

        private static Material Dressed(Material wall, Material placeholder)
        {
            if (dressed.TryGetValue(placeholder, out Material made) && made != null)
                return made;
            return dressed[placeholder] = GameMaterials.Plain(GameMaterials.Dress(wall, placeholder), Gloss);
        }
    }
}
