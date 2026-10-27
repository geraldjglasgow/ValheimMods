using System.Collections.Generic;
using BundlePrefabs;
using HarmonyLib;
using PatchGuard;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Broadside
{
    /// <summary>
    /// Broadside's pieces in game: the dry dock set, the tools and the generated parts they build, the fittings and the
    /// blueprint table, each a copy of the game's wood floor (its Piece, WearNTear and network view) wearing a mesh made
    /// from the joint table, in the floor's own wood material (the keels, ribs, post caps and generated hull parts in
    /// their Meadows biome look, <see cref="KwBiomeLooks"/>), with its joints as snap points (<see cref="KwSnaps"/>). They
    /// never break for lack of support or rain, cost nothing and give nothing back: a ship is designed with them, not
    /// built. With them the ship a saved design is built as from the hammer (<see cref="KwShipPrefab"/>, never in the
    /// hammer itself). Made once, registered whenever the network scene wakes, on every peer.
    /// </summary>
    public static class KwPrefabs
    {
        private const string Source = "wood_floor";
        private const string ShipSource = "VikingShip";
        private const string PieceShader = "Custom/Piece";
        private static readonly List<GameObject> built = new List<GameObject>();
        private static GameObject? ship;

        public static IReadOnlyList<GameObject> All => built;

        /// <summary>The longship's hull material and the floor's plank wood the pieces wear (the preview borrows their textures).</summary>
        public static Material? Hull { get; private set; }
        public static Material? Planks { get; private set; }

        public static void Install(Harmony harmony) =>
            NetPrefabs.OnSceneAwake(harmony, scene => Guard.Run("Broadside pieces", () => Build(scene)));

        private static void Build(ZNetScene scene)
        {
            if (built.Count == 0)
            {
                Create(scene);
                Log.Info($"Broadside: {KwIcons.RenderAll(built)} hammer icons rendered.");
            }
            foreach (GameObject prefab in built)
                NetPrefabs.Register(scene, prefab);
            ship ??= KwShipPrefab.Make(scene);
            if (ship != null)
                NetPrefabs.Register(scene, ship);
            KwHammer.Refresh();
            Log.Info($"Broadside ready: {built.Count} pieces" + (ship != null ? ", and the ship built from a saved design." : "; no ship: the game's Karve was not found."));
        }

        private static void Create(ZNetScene scene)
        {
            GameObject? source = scene.GetPrefab(Source);
            if (source == null || source.GetComponent<Piece>() == null)
            {
                Log.Warn($"Broadside: the game's {Source} was not found; no pieces.");
                return;
            }
            Planks = Wood(source);
            Hull = ShipHull(scene) ?? Planks;
            KwSailCloth.Find(scene);
            KwBiomeLooks.Make(Planks);
            KwCatalogue.Build();
            foreach (KwDef def in KwCatalogue.Defs.Values)
                built.Add(Make(scene, source, def, KwBiomeLooks.Prefab(def) ?? (def.Wood ? Planks : Hull)));
        }

        private static GameObject Make(ZNetScene scene, GameObject source, KwDef def, Material? wood)
        {
            GameObject copy = PrefabBench.Copy(source, def.Name);
            Strip(copy);
            // a workshop model when the piece has one (the blueprint table), else the generated mesh
            GameObject model = def.Model?.Invoke(scene, copy) ?? Model(copy, def, wood);
            KwSnaps.Add(copy.transform, def);
            Configure(copy, def, model);
            def.Setup?.Invoke(copy);
            if (def.Kind == "gen")
                copy.AddComponent<KwGenerated>().Tool = def.Name.Substring(KwToolPieces.PartPrefix.Length);
            return copy;
        }

        /// <summary>
        /// Everything the floor drew or collided with: its children (model, wear states, snap points), the root's own
        /// drawing parts and colliders, its level-of-detail group (the ship cannons' copies too, <see cref="KwCannonPrefabs"/>).
        /// </summary>
        public static void Strip(GameObject piece)
        {
            for (int i = piece.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(piece.transform.GetChild(i).gameObject);
            foreach (System.Type kind in new[] { typeof(LODGroup), typeof(Renderer), typeof(MeshFilter), typeof(Collider) })
            {
                foreach (Component part in piece.GetComponents(kind))
                    Object.DestroyImmediate(part);
            }
        }

        private static GameObject Model(GameObject piece, KwDef def, Material? wood)
        {
            GameObject model = new GameObject("model") { layer = piece.layer };
            model.transform.SetParent(piece.transform, false);
            Mesh mesh = KwMesh.Build(def);
            model.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = model.AddComponent<MeshRenderer>();
            if (wood != null)
                renderer.sharedMaterial = wood;
            // a generated part's mesh (and collider) is built from its saved span when it appears in the world
            model.AddComponent<MeshCollider>().sharedMesh = def.Kind == "gen" ? null : mesh;
            Anchor(model);
            if (def.Support)
                model.AddComponent<KwSupport>();
            foreach ((string name, Vector3 kit, System.Action<MeshBuilder> shape) in def.Parts)
                Part(model, def, name, kit, shape, wood);
            return model;
        }

        /// <summary>
        /// The box a piece's ghost is placed by. The game places a ghost by the point of its colliders nearest the aim, and
        /// skips concave mesh colliders: with only the mesh collider it found none and threw the ghost to twice the aim's
        /// world position, and with boxes only (the blueprint table's) it takes the nearest box's corner, a leg's foot, and
        /// shifts the piece sideways. A 10 cm box at the piece's origin (its lowest point) is what it places by; the mesh
        /// collider or the boxes stay for walking.
        /// </summary>
        public static void Anchor(GameObject model)
        {
            BoxCollider anchor = model.AddComponent<BoxCollider>();
            // standing on the origin (its bottom face there), so a piece aimed at a floor sits on it, not 5 cm above
            anchor.center = new Vector3(0f, 0.05f, 0f);
            anchor.size = Vector3.one * 0.1f;
        }

        // A part with a mesh of its own under the model (a keel support), at its kit position; it fits the keel above it.
        private static void Part(GameObject model, KwDef def, string name, Vector3 kit, System.Action<MeshBuilder> shape, Material? wood)
        {
            GameObject part = new GameObject(name) { layer = model.layer };
            part.transform.SetParent(model.transform, false);
            part.transform.localPosition = KwMesh.ToUnity(kit - new Vector3(def.Pivot.x, 0f, def.Pivot.y));
            MeshBuilder builder = new MeshBuilder(Vector3.zero);
            shape(builder);
            Mesh mesh = builder.ToMesh(def.Name + "_" + name);
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = part.AddComponent<MeshRenderer>();
            if (wood != null)
                renderer.sharedMaterial = wood;
            part.AddComponent<MeshCollider>().sharedMesh = mesh;
            part.AddComponent<KwSupport>();
        }

        private static void Configure(GameObject copy, KwDef def, GameObject model)
        {
            Piece piece = copy.GetComponent<Piece>();
            piece.m_name = def.Label;
            piece.m_description = def.Description ?? (def.Kind == "tool" ? def.Label : "A Broadside ship design piece: free to build, and removing it gives nothing back.");
            piece.m_category = Piece.PieceCategory.Misc;
            // The build menu's Transportation filter reads Transport, as the game's own ships carry.
            piece.m_usage = Piece.UsageTagFlags.Transport;
            piece.m_enabled = true;
            // Design mode: nothing to pay, so nothing to give back or drop when the piece is removed or broken.
            piece.m_resources = new Piece.Requirement[0];
            WearNTear wear = copy.GetComponent<WearNTear>();
            if (wear == null)
                return;
            wear.m_noSupportWear = false;
            wear.m_noRoofWear = false;
            wear.m_health = 2000f;
            wear.m_new = wear.m_worn = wear.m_broken = model;
            wear.m_wet = null;
            wear.m_fragmentRoots = null;
        }

        /// <summary>
        /// The longship's hull material (<c>VikingShip</c>, renderer <c>hull</c>, material <c>ship</c>: the atlas
        /// <see cref="KwUv"/> maps onto), shared and never changed; null when the game has none.
        /// </summary>
        private static Material? ShipHull(ZNetScene scene)
        {
            GameObject? ship = scene.GetPrefab(ShipSource);
            if (ship == null)
                return null;
            foreach (MeshRenderer renderer in ship.GetComponentsInChildren<MeshRenderer>(true))
            {
                Material material = renderer.sharedMaterial;
                if (renderer.name == "hull" && material != null && material.name.StartsWith("ship") && material.shader.name == PieceShader)
                {
                    Log.Info($"Broadside: pieces wear the longship's {material.name}.");
                    return material;
                }
            }
            Log.Warn($"Broadside: {ShipSource} has no hull material; pieces wear the wood floor's.");
            return null;
        }

        /// <summary>The floor's own wood material (shared, never changed): Custom/Piece, not a snow overlay.</summary>
        private static Material? Wood(GameObject source)
        {
            foreach (Renderer renderer in source.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material != null && material.shader.name == PieceShader && !material.name.ToLowerInvariant().Contains("snow"))
                    {
                        Log.Info($"Broadside: pieces wear {source.name}'s {material.name}.");
                        return material;
                    }
                }
            }
            Log.Warn($"Broadside: {source.name} has no {PieceShader} material; pieces use Unity's default.");
            return null;
        }
    }
}
