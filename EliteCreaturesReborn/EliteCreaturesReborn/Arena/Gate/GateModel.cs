using System;
using System.Collections.Generic;
using BundlePrefabs;
using EliteCreaturesReborn.Arena.Hall;
using EliteCreaturesReborn.Util;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EliteCreaturesReborn.Arena.Gate
{
    /// <summary>
    /// Helgrind's own model and build-menu icon, from the embedded bundle <c>ecr_helgate</c> (ValheimAssets
    /// <c>Assets/Props/HelGate/ecr_helgate</c>, its BRIEF.md the contract: prefab <c>ecr_helgate</c> with <c>model</c>, the
    /// glow children <c>glow_frostcore_0</c>, <c>_1</c>, <c>glow_coal</c>, <c>glow_runes</c>, the points
    /// <c>portal_center</c>, <c>light</c> and <c>exit</c>, eight <c>col_box_*</c> colliders; sprite <c>ecr_helgate_icon</c>).
    /// The stone portal's model, its mesh and floor colliders go; the gate takes their place on the piece's layer, dressed
    /// on clients (<see cref="GateMaterials"/>), one object for every wear state. Every peer, identically. A bundle that
    /// cannot load leaves the stone portal's look, logged; it still takes players into the hall.
    /// </summary>
    internal static class GateModel
    {
        public const string Bundle = "ecr_helgate";

        /// <summary>The effect's bundle, <c>ecr_helgatefx</c> (<see cref="GateSwirl"/>).</summary>
        public const string EffectBundle = "ecr_helgatefx";

        /// <summary>The swirl's centre, as built: (0, 1.73, 0), facing +Z (the gate's front).</summary>
        public const string Center = "portal_center";

        /// <summary>The swirl's light, as built: (0, 1.73, 0.2475).</summary>
        public const string LightPoint = "light";

        /// <summary>Where a player comes back out, as built: (0, 0, 2.254), facing away from the gate.</summary>
        public const string Exit = "exit";

        /// <summary>The points as built, for a gate without its model.</summary>
        public static readonly Vector3 CenterAsBuilt = new Vector3(0f, 1.73f, 0f);
        public static readonly Vector3 LightAsBuilt = new Vector3(0f, 1.73f, 0.2475f);
        public static readonly Vector3 ExitAsBuilt = new Vector3(0f, 0f, 2.254f);

        /// <summary>The walk-in trigger's box, centred on <c>portal_center</c>: the arched opening (2.44 x 3.22 m) less a
        /// margin, 0.4 m deep.</summary>
        public static readonly Vector3 Opening = new Vector3(2.24f, 3.12f, 0.40f);

        private const string Asset = "ecr_helgate";
        private const string Placed = "helgrind";

        // What the stone portal drew or stood on: its model (with its level-of-detail group), its mesh and floor colliders.
        private static readonly string[] GameLook = { "New", "Mesh collider", "floor collider" };

        private static readonly HashSet<string> Failed = new HashSet<string>();

        /// <summary>The build menu icon; null without the bundle (the stone portal's stays).</summary>
        public static Sprite? Icon { get; private set; }

        /// <summary>An embedded bundle of the gate's; null (logged once) when it will not load.</summary>
        public static AssetBundle? Load(string bundle)
        {
            if (Failed.Contains(bundle))
            {
                return null;
            }
            try
            {
                return EmbeddedBundle.Load(typeof(GateModel).Assembly, bundle);
            }
            catch (Exception e)
            {
                Failed.Add(bundle);
                Log.Error($"Helgrind: the bundle {bundle} did not load: {e.Message}");
                return null;
            }
        }

        /// <summary>Puts the gate on the bench copy of the stone portal; false when the bundle is missing.</summary>
        public static bool Wear(GameObject piece, List<GameObject> prefabs)
        {
            AssetBundle? bundle = Load(Bundle);
            GameObject? model = bundle != null ? bundle.LoadAsset<GameObject>(Asset) : null;
            if (bundle == null || model == null)
            {
                Log.Error("Helgrind's model did not load: it keeps the stone portal's look.");
                return false;
            }
            Strip(piece);
            GameObject placed = Object.Instantiate(model, piece.transform, false);
            placed.name = Placed;
            HallBuilder.SetLayer(placed.transform, piece.layer);
            GateMaterials.Dress(placed, prefabs);
            Point(piece.GetComponent<WearNTear>(), placed);
            Icon = bundle.LoadAsset<Sprite>(Asset + "_icon");
            return true;
        }

        private static void Strip(GameObject piece)
        {
            foreach (string name in GameLook)
            {
                Transform? part = piece.transform.Find(name);
                if (part != null)
                {
                    Object.DestroyImmediate(part.gameObject);
                }
            }
        }

        // One model for every wear state, no fragment roots (they were the stripped model's).
        private static void Point(WearNTear? wear, GameObject placed)
        {
            if (wear == null)
            {
                return;
            }
            (wear.m_new, wear.m_worn, wear.m_broken, wear.m_wet) = (placed, placed, placed, null);
            wear.m_fragmentRoots = null;
        }

        /// <summary>A point of the gate's model (<c>portal_center</c>, <c>light</c>, <c>exit</c>) in the piece's own space,
        /// read from the piece; the given fallback (the bundle's value as built) when the model is missing.</summary>
        public static Vector3 PointIn(GameObject? piece, string name, Vector3 fallback)
        {
            Transform? point = piece != null ? GameMaterials.Find(piece.transform, name) : null;
            return point != null ? piece!.transform.InverseTransformPoint(point.position) : fallback;
        }
    }
}
