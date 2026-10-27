using System.Collections.Generic;
using BundlePrefabs;
using HarmonyLib;
using MannedWeapons;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The ship cannons in game (CLAUDE.md, "Cannons"): the four cannons (<see cref="KwShipCannons"/>), each a copy of the
    /// game's wood floor (its Piece, WearNTear and network view, stripped as Broadside's own pieces are,
    /// <see cref="KwPrefabs.Strip"/>) wearing meshes made in code from the game's own materials; then the cannonballs and
    /// the black powder (<see cref="KwItems"/>) and the cannons' shots (<see cref="KwShots"/>). Built once when the network
    /// scene first wakes, after Broadside's own pieces, on every peer (the dedicated server too: icons only where there is a
    /// graphics device), and registered every time it wakes with <see cref="NetPrefabs.Register"/>; then the hammer is
    /// refreshed (<see cref="KwCannonHammer"/>). Every name starts with <c>BS_</c>, so none is taken for a Broadside ship
    /// part (those are <c>KW_</c>: blueprints, snap rules, undo, <c>kwship clear</c>). Unlike Broadside's design pieces
    /// they cost their recipe and give it back when removed. The gunners' controls, feet, facing and view are the
    /// MannedWeapons library's patches, and a shot passing through the cannon and the ship it was fired from its projectile
    /// patch, all installed here, with the captain's broadside key (<see cref="KwBroadside"/>).
    /// </summary>
    public static class KwCannonPrefabs
    {
        private const string Source = "wood_floor";
        private static readonly List<GameObject> built = new List<GameObject>();

        /// <summary>The cannon pieces, once made.</summary>
        public static IReadOnlyList<GameObject> All => built;

        /// <summary>Once, from the plugin, after Broadside's own pieces (<see cref="KwPrefabs.Install"/>): the library's patches, the broadside's ticker, the prefabs' builder.</summary>
        public static void Install(Harmony harmony)
        {
            IReadOnlyList<string> missing = MannedWeapon.Install(harmony);
            if (missing.Count > 0)
                Log.Warn($"Broadside cannons: the game has no {string.Join(", ", missing)}; cannons cannot be manned.");
            WeaponShot.InstallPassThrough(harmony);
            KwBroadside.Install();
            NetPrefabs.OnSceneAwake(harmony, scene => Guard.Run("Broadside cannons", () => Build(scene)));
        }

        private static void Build(ZNetScene scene)
        {
            if (built.Count == 0)
            {
                Create(scene);
                Log.Info($"Broadside cannons: {KwIcons.RenderAll(built)} hammer icons rendered.");
            }
            foreach (GameObject prefab in built)
                NetPrefabs.Register(scene, prefab);
            KwItems.Install(scene);
            KwShots.Install(scene, KwArmory.Shots());
            KwHammer.Refresh();
            Log.Info($"Broadside cannons ready: {built.Count} pieces, {KwArmory.Items.Count} items.");
        }

        private static void Create(ZNetScene scene)
        {
            GameObject? source = scene.GetPrefab(Source);
            if (source == null || source.GetComponent<Piece>() == null)
            {
                Log.Warn($"Broadside cannons: the game's {Source} was not found; no cannons.");
                return;
            }
            foreach (KwDef def in KwArmory.Pieces())
                built.Add(Make(source, def));
        }

        private static GameObject Make(GameObject source, KwDef def)
        {
            GameObject copy = PrefabBench.Copy(source, def.Name);
            KwPrefabs.Strip(copy);
            GameObject model = Model(copy, def);
            Configure(copy, def, model);
            def.Setup?.Invoke(copy);
            return copy;
        }

        // The piece's model (a cannon's ringbolts), without a collider: its colliders are its body's (KwCannonBody).
        private static GameObject Model(GameObject piece, KwDef def)
        {
            GameObject model = new GameObject("model") { layer = piece.layer };
            model.transform.SetParent(piece.transform, false);
            model.AddComponent<MeshFilter>().sharedMesh = KwMesh.Build(def);
            MeshRenderer renderer = model.AddComponent<MeshRenderer>();
            if (KwPrefabs.Planks != null)
                renderer.sharedMaterial = KwPrefabs.Planks;
            return model;
        }

        private static void Configure(GameObject copy, KwDef def, GameObject model)
        {
            Piece piece = copy.GetComponent<Piece>();
            (piece.m_name, piece.m_description) = (def.Label, def.Description ?? def.Label);
            (piece.m_category, piece.m_enabled) = (Piece.PieceCategory.Misc, true);
            // The build menu's Transportation filter, as Broadside's pieces and the game's own ships carry.
            piece.m_usage = Piece.UsageTagFlags.Transport;
            WearNTear wear = copy.GetComponent<WearNTear>();
            if (wear == null)
                return;
            // never breaks for lack of support (the ship holds it up), never rots in the rain
            (wear.m_noSupportWear, wear.m_noRoofWear) = (false, false);
            wear.m_new = wear.m_worn = wear.m_broken = model;
            (wear.m_wet, wear.m_fragmentRoots) = (null, null);
        }
    }
}
