using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OpenKeep.Blueprints.Bench
{
    /// <summary>
    /// The Blueprint Bench piece <c>OpenKeep_BlueprintBench</c>: a copy of the game's workbench (its placement, wear,
    /// sounds and wood) without the crafting station, wearing the bench's own model, a drafting desk about half the
    /// workbench's size (<see cref="BenchModel"/>), and carrying <see cref="BlueprintBench"/>. Without the bundle it keeps
    /// the workbench's look at half size. Made once per process on every peer, dedicated server included, under
    /// BundlePrefabs' inactive bench (no Awake, no ZDO), and added to the scene's prefab list before ZNetScene.Awake
    /// registers it, whether blueprints are on or not, so benches already built always load. The hammer and the cost are
    /// <see cref="BenchHammer"/>'s.
    /// </summary>
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    public static class BenchPrefab
    {
        public const string PrefabName = "OpenKeep_BlueprintBench";
        public const string Source = "piece_workbench";

        /// <summary>The workbench's look is shrunk to half only when the bench's own model is missing.</summary>
        private const float FallbackScale = 0.5f;

        /// <summary>The bench prefab; null until the scene first woke (or when the game's workbench is missing).</summary>
        public static GameObject Prefab { get; private set; }

        public static Piece Piece => Prefab != null ? Prefab.GetComponent<Piece>() : null;

        [HarmonyPrefix]
        private static void Prefix(ZNetScene __instance)
        {
            try
            {
                AddTo(__instance.m_prefabs);
            }
            catch (Exception e)
            {
                // Never out of ZNetScene.Awake: a throw there leaves the world loading for ever.
                Plugin.Log.LogError($"OpenKeep: the Blueprint Bench could not be made: {e}");
            }
        }

        private static void AddTo(List<GameObject> prefabs)
        {
            if (Prefab == null)
                Prefab = Build(prefabs.Find(p => p != null && p.name == Source));
            if (Prefab != null && !prefabs.Contains(Prefab))
                prefabs.Add(Prefab);
        }

        private static GameObject Build(GameObject workbench)
        {
            if (workbench == null || workbench.GetComponent<Piece>() == null)
            {
                Plugin.Log.LogWarning($"OpenKeep: the game's {Source} was not found: there is no Blueprint Bench");
                return null;
            }
            GameObject copy = BundlePrefabs.PrefabBench.Copy(workbench, PrefabName);
            Object.DestroyImmediate(copy.GetComponent<CraftingStation>());
            if (!BenchModel.Wear(copy))
                Shrink(copy, workbench);
            Configure(copy.GetComponent<Piece>(), workbench);
            copy.AddComponent<BlueprintBench>();
            return copy;
        }

        /// <summary>The fallback look: the workbench at half size, without its range circle.</summary>
        private static void Shrink(GameObject copy, GameObject workbench)
        {
            copy.transform.localScale = workbench.transform.localScale * FallbackScale;
            foreach (CircleProjector marker in copy.GetComponentsInChildren<CircleProjector>(true))
                Object.DestroyImmediate(marker.gameObject);
        }

        private static void Configure(Piece piece, GameObject workbench)
        {
            piece.m_name = BenchWords.Name;
            piece.m_description = BenchWords.Description;
            piece.m_category = Piece.PieceCategory.Crafting;
            piece.m_enabled = true;
            piece.m_craftingStation = workbench.GetComponent<CraftingStation>();
            if (BenchModel.Icon != null)
                piece.m_icon = BenchModel.Icon;
            else if (!Application.isBatchMode)
                piece.m_icon = BlueprintIcons.Get(BlueprintIcons.Blueprint) ?? piece.m_icon;
        }
    }
}
