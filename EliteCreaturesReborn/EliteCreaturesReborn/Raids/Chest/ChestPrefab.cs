using System;
using System.Collections.Generic;
using BundlePrefabs;
using EliteCreaturesReborn.Patches;
using EliteCreaturesReborn.Util;
using HarmonyLib;
using UnityEngine;

namespace EliteCreaturesReborn.Raids
{
    /// <summary>
    /// The Raiders Chest piece <c>ECR_RaidersChest</c> (features/raids.md section 2): a copy of the game's reinforced chest
    /// (<c>piece_chest</c>: its container, placement, health, wear, sounds and workbench) wearing its own model with the war
    /// horn on its lid (<see cref="ChestModel"/>, <see cref="ChestLid"/>), a raid host (<see cref="RaidRunner"/>) and the
    /// chest's own hover and use (<see cref="RaidChest"/>). Built once per process on every peer, dedicated server
    /// included, under BundlePrefabs' inactive bench (no Awake, no ZDO), and put in the scene's prefab list before
    /// <c>ZNetScene.Awake</c> registers it, whether the setting is on or not, so chests already built always load. The
    /// prefab's name is hashed into saves: it never changes.
    /// </summary>
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    internal static class ChestPrefab
    {
        public const string PrefabName = "ECR_RaidersChest";

        /// <summary>The piece's word in the game's translation table (<see cref="ChestWords"/>).</summary>
        public const string Word = "piece_ecr_raiderschest";

        /// <summary>The piece's and the container's name; also how the chest's inventory is known (<see cref="ChestCoins"/>).</summary>
        public const string ContainerName = "$" + Word;

        private const string GameChest = "piece_chest";

        /// <summary>The prefab's hash, as every ZDO of a Raiders Chest carries it.</summary>
        public static readonly int PrefabHash = PrefabName.GetStableHashCode();

        /// <summary>The chest prefab; null until the scene first woke (or when the game's chest is missing).</summary>
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
                Log.Error($"the Raiders Chest could not be made: {e}");
            }
        }

        /// <summary>A prefab of the scene's list by name; null when the game has none. Only while the scene wakes.</summary>
        public static GameObject? Find(List<GameObject> prefabs, string name) =>
            prefabs.Find(p => p != null && p.name == name);

        // The chest first: its sound and words are extras a failure must not cost it.
        private static void AddTo(List<GameObject> prefabs)
        {
            RaidHosts.AddHostPrefab(PrefabName);
            if (Prefab == null)
            {
                Prefab = Build(Find(prefabs, GameChest));
            }
            if (Prefab != null && !prefabs.Contains(Prefab))
            {
                prefabs.Add(Prefab);
            }
            SafeCall.Run("Raiders Chest horn sound", static list => RaidHorn.Prepare(list), prefabs);
            SafeCall.Run("Raiders Chest words", static () => ChestWords.Add(Localization.instance));
        }

        private static GameObject? Build(GameObject? chest)
        {
            if (chest == null || chest.GetComponent<Container>() == null || chest.GetComponent<Piece>() == null)
            {
                Log.Warn($"the game's {GameChest} was not found: there is no Raiders Chest.");
                return null;
            }
            GameObject copy = PrefabBench.Copy(chest, PrefabName);
            Container container = copy.GetComponent<Container>();
            container.m_name = ContainerName;
            bool own = ChestModel.Wear(copy, container);
            Describe(copy.GetComponent<Piece>());
            copy.AddComponent<RaidRunner>();
            copy.AddComponent<RaidChest>();
            if (own)
            {
                copy.AddComponent<ChestLid>();
            }
            return copy;
        }

        // The hammer's Misc tab; the cost and the workbench are set with the hammer (ChestHammer), once items exist.
        private static void Describe(Piece piece)
        {
            piece.m_name = ContainerName;
            piece.m_description = "$" + Word + "_description";
            piece.m_category = Piece.PieceCategory.Misc;
            piece.m_enabled = true;
            if (ChestModel.Icon != null)
            {
                piece.m_icon = ChestModel.Icon;
            }
        }
    }
}
