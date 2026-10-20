using System;
using System.Collections.Generic;
using BundlePrefabs;
using EliteCreaturesReborn.Util;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EliteCreaturesReborn.Rendering
{
    /// <summary>
    /// The Rendering Table piece <c>ECR_RenderingTable</c> (the user, 2026-10-20: "a new workbench that is as tall as the
    /// vanilla workbench, but 1/2 the width"): a copy of the game's workbench (<c>piece_workbench</c>: its model, wear
    /// states, fragments, snow, collider, health and sounds) with every model part squeezed to half its width along the
    /// bench's long side (3.05 m to 1.53 m; its 1.85 m height and 1.33 m depth kept). It is not a crafting station: the
    /// station, its build circle, its base area, its roof check, its extension point and Hugin's guide point go, and
    /// <see cref="RenderingTable"/> takes the hover and E. The station's craft-done sound is kept for a finished render.
    /// Built once per process on every peer, dedicated server included, under BundlePrefabs' inactive bench (no Awake,
    /// no ZDO), and put in the scene's prefab list before <c>ZNetScene.Awake</c> registers it, whether the setting is on
    /// or not, so tables already built always load. The prefab's name is hashed into saves: it never changes.
    /// </summary>
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    internal static class RenderPrefab
    {
        public const string PrefabName = "ECR_RenderingTable";

        /// <summary>The piece's name token.</summary>
        public const string Name = "$" + RenderWords.Piece;

        private const string GameBench = "piece_workbench";
        private const float Width = 0.5f;

        // The workbench's children that belong to its station, not its model.
        private static readonly string[] StationParts =
        {
            "roof_check_pint", "connectionEffectPoint", "PlayerBase", "GuidePoint", "AreaMarker",
        };

        private static EffectList? _done;

        /// <summary>The table prefab; null until the scene first woke (or when the game's workbench is missing).</summary>
        public static GameObject? Prefab { get; private set; }

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
                Log.Error($"the Rendering Table could not be made: {e}");
            }
        }

        /// <summary>The workbench's craft-done sound at the table, on this machine only (the renderer's own ears).</summary>
        public static void PlayDone(Vector3 at)
        {
            if (_done != null)
            {
                _done.Create(at, Quaternion.identity);
            }
        }

        private static void AddTo(List<GameObject> prefabs)
        {
            if (Prefab == null)
            {
                Prefab = Build(prefabs.Find(p => p != null && p.name == GameBench));
            }
            if (Prefab != null && !prefabs.Contains(Prefab))
            {
                prefabs.Add(Prefab);
            }
        }

        private static GameObject? Build(GameObject? bench)
        {
            if (bench == null || bench.GetComponent<CraftingStation>() == null || bench.GetComponent<Piece>() == null)
            {
                Log.Warn($"the game's {GameBench} was not found: there is no Rendering Table.");
                return null;
            }
            GameObject copy = PrefabBench.Copy(bench, PrefabName);
            CraftingStation station = copy.GetComponent<CraftingStation>();
            _done = station.m_craftItemDoneEffects;
            Object.DestroyImmediate(station);
            StripStation(copy.transform);
            Squeeze(copy.transform);
            Describe(copy.GetComponent<Piece>());
            copy.AddComponent<RenderingTable>();
            return copy;
        }

        private static void StripStation(Transform root)
        {
            foreach (string name in StationParts)
            {
                Transform part = root.Find(name);
                if (part != null)
                {
                    Object.DestroyImmediate(part.gameObject);
                }
            }
        }

        // Every remaining child is model, collider, snow or fragments, each unrotated on the bench's root: half the
        // width along X, and half its distance from the middle.
        private static void Squeeze(Transform root)
        {
            foreach (Transform part in root)
            {
                Vector3 place = part.localPosition;
                Vector3 size = part.localScale;
                part.localPosition = new Vector3(place.x * Width, place.y, place.z);
                part.localScale = new Vector3(size.x * Width, size.y, size.z);
            }
        }

        // The hammer's Crafting tab beside the workbench; the cost and the station are set with the hammer (RenderHammer).
        private static void Describe(Piece piece)
        {
            piece.m_name = Name;
            piece.m_description = Name + "_description";
            piece.m_category = Piece.PieceCategory.Crafting;
            piece.m_enabled = true;
        }
    }
}
