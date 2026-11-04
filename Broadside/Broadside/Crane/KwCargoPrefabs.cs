using System.Collections.Generic;
using BundlePrefabs;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The cargo crane (<see cref="KwCargo.CranePrefab"/>, a copy of the game's workbench: its placement, health, wear
    /// and sounds, without the crafting station) and the cargo crate (<see cref="KwCargo.CratePrefab"/>, a copy of the
    /// game's wood chest: its Container, 6 x 4, its items kept in its ZDO), each wearing its workshop model, made once
    /// when the network scene first wakes and registered every time it wakes, on every peer, the dedicated server
    /// included (an Asset Lab bench until 2026-11-02, when the user had it put into Broadside). Without its bundle a piece
    /// is not built (logged once).
    /// </summary>
    public static class KwCargoPrefabs
    {
        private const string Workbench = "piece_workbench", WoodChest = "piece_chest_wood";
        // The game pieces' body renderers (their first renderer is often a snow overlay whose shader moves vertices).
        private const string WorkbenchBody = "high", ChestBody = "woodchest";
        private static GameObject? crane, crate;
        private static bool tried;

        public static GameObject? Crane => crane;
        public static GameObject? Crate => crate;

        public static void Install(Harmony harmony) =>
            NetPrefabs.OnSceneAwake(harmony, scene => Guard.Run("Broadside cargo pieces", () => Build(scene)));

        private static void Build(ZNetScene scene)
        {
            if (!tried)
            {
                tried = true;
                crane = Make(KwCargo.CranePrefab, MakeCrane, scene);
                crate = Make(KwCargo.CratePrefab, MakeCrate, scene);
            }
            foreach (GameObject? prefab in new[] { crane, crate })
            {
                if (prefab != null)
                    NetPrefabs.Register(scene, prefab);
            }
            KwCargoHammer.Refresh();
        }

        // One piece that throws is logged and left out; the other is still built.
        private static GameObject? Make(string name, System.Func<ZNetScene, GameObject?> make, ZNetScene scene)
        {
            try
            {
                return make(scene);
            }
            catch (System.Exception e)
            {
                Log.Error($"Broadside: {name} not built: {e}");
                return null;
            }
        }

        private static GameObject? MakeCrane(ZNetScene scene)
        {
            GameObject? bench = Base(scene, Workbench), source = KwCargoModel.Source(KwCargo.CraneBundle);
            if (bench == null || source == null)
                return null;
            GameObject copy = Strip(PrefabBench.Copy(bench, KwCargo.CranePrefab));
            GameObject model = KwCargoModel.Wear(copy, source);
            KwCraneRig.Arrange(model.transform);
            Transform? turn = GameMaterials.Find(model.transform, "turn"), head = GameMaterials.Find(model.transform, "head");
            KwCargoModel.Dress(model, bench, WorkbenchBody, r => turn != null && r.transform.IsChildOf(turn) || head != null && r.transform.IsChildOf(head));
            KwCargoModel.Line(copy.transform, KwCraneRig.RopeName, scene);
            Setup(copy, KwCrane.Label, KwCraneText.CraneDescription, KwCargo.CraneBundle);
            copy.AddComponent<KwCraneRig>();
            copy.AddComponent<KwCrane>();
            Log.Info($"Broadside: {KwCargo.CranePrefab} built.");
            return copy;
        }

        private static GameObject? MakeCrate(ZNetScene scene)
        {
            GameObject? chest = Base(scene, WoodChest), source = KwCargoModel.Source(KwCargo.CrateBundle);
            if (chest == null || source == null || chest.GetComponent<Container>() == null)
                return null;
            GameObject copy = Strip(PrefabBench.Copy(chest, KwCargo.CratePrefab));
            KwCargoModel.Dress(KwCargoModel.Wear(copy, source), chest, ChestBody, null);
            Setup(copy, KwCrate.Label, KwCraneText.CrateDescription, KwCargo.CrateBundle);
            Container box = copy.GetComponent<Container>();
            (box.m_name, box.m_width, box.m_height, box.m_open, box.m_closed) = (KwCrate.Label, 6, 4, null, null);
            WearNTear wear = copy.GetComponent<WearNTear>();
            // it hangs in the air and moves: no wear for want of support, and nothing is built on it
            (wear.m_noSupportWear, wear.m_supports) = (false, false);
            copy.AddComponent<KwCrate>();
            copy.AddComponent<KwCrateLook>();
            copy.AddComponent<KwCrateLid>();
            Log.Info($"Broadside: {KwCargo.CratePrefab} built.");
            return copy;
        }

        private static GameObject? Base(ZNetScene scene, string name)
        {
            GameObject? found = scene.GetPrefab(name);
            if (found == null || found.GetComponent<Piece>() == null || found.GetComponent<WearNTear>() == null)
                Log.Error($"Broadside: the game has no {name} piece to copy; that cargo piece is not built.");
            return found;
        }

        // Everything the base drew or did besides being a networked, wearing piece (and a chest's Container) goes.
        private static GameObject Strip(GameObject copy)
        {
            for (int i = copy.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(copy.transform.GetChild(i).gameObject);
            foreach (Component part in copy.GetComponents<Component>())
            {
                if (!(part is Transform || part is ZNetView || part is Piece || part is WearNTear || part is Container))
                    Object.DestroyImmediate(part);
            }
            return copy;
        }

        private static void Setup(GameObject copy, string label, string description, string bundle)
        {
            Piece piece = copy.GetComponent<Piece>();
            piece.m_name = label;
            piece.m_description = description;
            piece.m_category = Piece.PieceCategory.Misc;
            Sprite? icon = KwCargoModel.Icon(bundle);
            if (icon != null)
                piece.m_icon = icon;
        }
    }
}
