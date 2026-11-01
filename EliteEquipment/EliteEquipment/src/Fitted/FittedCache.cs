using System;
using System.Collections.Generic;
using EliteEquipment.Boots;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

using Object = UnityEngine.Object;

namespace EliteEquipment.Fitted
{
    /// <summary>
    /// How long the fitted looks' shared builds live. Each kind is made once and shared by every player wearing it
    /// (<see cref="FittedBuild"/>: the leggings' mesh and the trimmed body; <see cref="FittedMaterial"/>: a look's
    /// material; <see cref="ChestFit"/>: a drawn-in chest), and kept while the scene runs, so a crowd walking in and out
    /// of view never makes them again. When the main menu or a world starts (postfixes on <c>FejdStartup.Start</c> and
    /// <c>Game.Start</c>), every build that no character in the scene draws or keeps is destroyed, so nothing made for one
    /// world's players stays in memory in the menu or in the next world; a build still worn (an object of the old scene
    /// not yet gone) stays until the next change. When a world starts, behind its loading screen, what would hold up the
    /// frame in which the first player of a kind comes into view is made at once (<see cref="Prepare"/>): the Player
    /// prefab's two bodies read (vertices, weights, the texture's wrap, the trim for bare legs) and every look's material,
    /// whose atlas reads the game's chest textures back from the GPU (each read waits for the GPU). Then the first player
    /// of a kind costs only its mesh build. Prepared only while Separate Boots is on; nothing on a dedicated server (no
    /// graphics). A failure only logs: the builds are then made on first use, as before.
    /// </summary>
    internal static class FittedCache
    {
        private static readonly bool headless = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
        private static readonly int PlayerPrefab = "Player".GetStableHashCode();

        /// <summary>Destroys every shared build no character in the scene wears; the next one worn is made again.</summary>
        internal static void Release()
        {
            if (headless)
                return;
            HashSet<Object> used = InUse();
            int freed = FittedBuild.Release(used) + FittedMaterial.Release(used) + ChestFit.Release(used);
            BootTop.Clear();
            if (freed > 0)
                Plugin.Log.LogInfo($"EliteEquipment: released {freed} fitted leggings meshes and materials no one wears");
        }

        /// <summary>Every mesh and material a character in the scene (shown, hidden or inactive) draws or keeps for its body.</summary>
        private static HashSet<Object> InUse()
        {
            var used = new HashSet<Object>();
            foreach (VisEquipment vis in Object.FindObjectsByType<VisEquipment>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (vis.m_models != null)
                {
                    foreach (VisEquipment.PlayerModel model in vis.m_models)
                    {
                        if (model != null && model.m_mesh != null)
                            used.Add(model.m_mesh);
                    }
                }
                foreach (SkinnedMeshRenderer renderer in vis.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    Drawn(renderer, used);
            }
            return used;
        }

        private static void Drawn(SkinnedMeshRenderer renderer, HashSet<Object> used)
        {
            if (renderer.sharedMesh != null)
                used.Add(renderer.sharedMesh);
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material != null)
                    used.Add(material);
            }
        }

        /// <summary>The parts that hold up a frame, made behind the world's loading screen: both bodies and every look's material.</summary>
        private static void Prepare()
        {
            if (headless || !BootsSettings.On || ZNetScene.instance == null || ObjectDB.instance == null)
                return;
            GameObject prefab = ZNetScene.instance.GetPrefab(PlayerPrefab);
            VisEquipment vis = prefab != null ? prefab.GetComponent<VisEquipment>() : null;
            if (vis != null && vis.m_bodyModel != null && vis.m_models != null)
            {
                foreach (VisEquipment.PlayerModel model in vis.m_models)
                {
                    if (model != null && model.m_mesh != null)
                        FittedBuild.Prepare(model.m_mesh, vis.m_bodyModel);
                }
            }
            foreach (LegStyle style in LegStyles.All)
            {
                // Only for a chest the database has: a material not found is remembered as none for the scene.
                if (ObjectDB.instance.GetItemPrefab(style.Chest) != null)
                    FittedMaterial.Get(style);
            }
        }

        private static void Safely(Action step, string what)
        {
            try
            {
                step();
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"EliteEquipment: the fitted leggings could not {what}: {e.Message}");
            }
        }

        /// <summary>The main menu starts: the last world's builds go (the menu's character makes its own).</summary>
        [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Start))]
        private static class MenuPatch
        {
            [HarmonyPostfix]
            private static void Postfix() => Safely(Release, "release the last world's builds");
        }

        /// <summary>A world starts, behind its loading screen: the menu's builds go, the parts that stall are made.</summary>
        [HarmonyPatch(typeof(Game), nameof(Game.Start))]
        private static class WorldPatch
        {
            [HarmonyPostfix]
            private static void Postfix()
            {
                Safely(Release, "release the menu's builds");
                Safely(Prepare, "be made ahead of need");
            }
        }
    }
}
