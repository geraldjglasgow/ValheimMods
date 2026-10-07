using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OpenKeep.Mimir
{
    /// <summary>
    /// The Mímir's Chest piece <c>OpenKeep_MimirChest</c>: a copy of the game's reinforced chest (<c>piece_chest</c>: its
    /// container, placement, wear, sounds and hammer tab) eight slots wide, wearing its own model with an animated lid
    /// (<see cref="MimirModel"/>, <see cref="MimirLid"/>). It is an ordinary game container, so Reach, Store, Shared,
    /// Signs and the game itself use it as any chest; only its height follows its contents (<see cref="MimirRows"/>).
    /// Made once per process on every peer, dedicated server included, under BundlePrefabs' inactive bench (no Awake, no
    /// ZDO), and added to the scene's prefab list before ZNetScene.Awake registers it, whether the switch is on or not,
    /// so chests already built always load.
    /// </summary>
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    public static class MimirPrefab
    {
        public const string PrefabName = "OpenKeep_MimirChest";

        /// <summary>The container's name, also how its inventory is known (<see cref="MimirRows"/>).</summary>
        public const string ContainerName = "$ok_mimir_name";

        public const int Width = 8;
        private const string Source = "piece_chest";

        /// <summary>The chest prefab; null until the scene first woke (or when the game's chest is missing).</summary>
        public static GameObject Prefab { get; private set; }

        public static Piece Piece => Prefab != null ? Prefab.GetComponent<Piece>() : null;

        public static bool Is(string prefabName) => prefabName == PrefabName;

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
                Plugin.Log.LogError($"OpenKeep: Mímir's Chest could not be made: {e}");
            }
        }

        private static void AddTo(List<GameObject> prefabs)
        {
            if (Prefab == null)
                Prefab = Build(prefabs.Find(p => p != null && p.name == Source));
            if (Prefab != null && !prefabs.Contains(Prefab))
                prefabs.Add(Prefab);
        }

        private static GameObject Build(GameObject chest)
        {
            Container container = chest != null ? chest.GetComponent<Container>() : null;
            if (container == null || chest.GetComponent<Piece>() == null)
            {
                Plugin.Log.LogWarning($"OpenKeep: the game's {Source} was not found: there is no Mímir's Chest");
                return null;
            }
            GameObject copy = BundlePrefabs.PrefabBench.Copy(chest, PrefabName);
            Container own = copy.GetComponent<Container>();
            own.m_name = ContainerName;
            own.m_width = Width;
            own.m_height = MimirRows.MinRows;
            MimirModel.Wear(copy, own);
            Configure(copy.GetComponent<Piece>());
            return copy;
        }

        private static void Configure(Piece piece)
        {
            piece.m_name = ContainerName;
            piece.m_description = "$ok_mimir_desc";
            piece.m_enabled = true;
            if (MimirModel.Icon != null)
                piece.m_icon = MimirModel.Icon;
        }
    }
}
