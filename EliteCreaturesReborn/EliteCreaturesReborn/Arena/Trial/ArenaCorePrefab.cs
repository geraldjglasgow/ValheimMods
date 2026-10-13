using System;
using BundlePrefabs;
using HarmonyLib;
using PatchGuard;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// The arena core's prefab, <c>ECR_ArenaCore</c>: an invisible networked object with nothing but a persistent ZNetView
    /// and an <see cref="ArenaCore"/>, as the raids' test marker is built (<see cref="Raids.RaidMarker"/>). Built once
    /// under BundlePrefabs' inactive bench and added to ZNetScene's prefab list on every peer before the scene wakes, so
    /// the core's ZDO loads into the same prefab on the server and every client, whatever the Arena setting says (a world
    /// saved with a core must always load it). Nothing may leave the prefix: a ZNetScene whose Awake throws makes no world
    /// object at all, and the game waits on its loading screen for ever.
    /// </summary>
    [HarmonyPatch(typeof(ZNetScene), "Awake")]
    internal static class ArenaCorePrefab
    {
        public const string PrefabName = "ECR_ArenaCore";

        public static readonly int PrefabHash = PrefabName.GetStableHashCode();

        private static GameObject? _prefab;

        private static void Prefix(ZNetScene __instance)
        {
            try
            {
                Add(__instance);
            }
            catch (Exception e)
            {
                Guard.Report(e, "arena core prefab (Hel's Trial is unavailable this session)");
            }
            Patches.SafeCall.Run("Hel's Trial words", static () => ArenaWords.Add(Localization.instance));
        }

        /// <summary>Puts a new core at <paramref name="at"/>; this machine owns it. Null before the scene is up.</summary>
        public static ZNetView? Place(Vector3 at)
        {
            GameObject? prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(PrefabHash) : null;
            return prefab != null ? Object.Instantiate(prefab, at, Quaternion.identity).GetComponent<ZNetView>() : null;
        }

        private static void Add(ZNetScene scene)
        {
            foreach (GameObject known in scene.m_prefabs)
            {
                if (known != null && known.name == PrefabName)
                {
                    return; // a second scene of the session, or another copy already added
                }
            }
            scene.m_prefabs.Add(Prefab());
        }

        private static GameObject Prefab()
        {
            if (_prefab != null)
            {
                return _prefab;
            }
            GameObject core = new GameObject(PrefabName);
            core.transform.SetParent(PrefabBench.Root, false); // inactive parent: no Awake, no ZDO, until placed
            core.AddComponent<ZNetView>().m_persistent = true;
            core.AddComponent<ArenaCore>();
            _prefab = core;
            return core;
        }
    }
}
