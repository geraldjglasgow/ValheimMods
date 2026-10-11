using BundlePrefabs;
using HarmonyLib;
using UnityEngine;

namespace EliteCreaturesReborn.Raids
{
    /// <summary>
    /// The host of a test raid (`elite raid start`, no chest): an invisible networked object with nothing but a ZNetView
    /// and a <see cref="RaidRunner"/>, so a test raid runs on exactly the code a chest raid does. Built once under
    /// BundlePrefabs' inactive bench and registered with ZNetScene on every peer as the scene wakes, so its ZDO loads into
    /// the same prefab everywhere. Persistent, so its raid survives the zone unloading and is handed to the next machine
    /// like any piece; it removes itself a few seconds after its raid is over (<see cref="RaidRunner.RemoveWhenDone"/>).
    /// </summary>
    internal static class RaidMarker
    {
        public const string PrefabName = "ECR_RaidMarker";

        private static GameObject? _prefab;

        /// <summary>Plugin start: build and register the marker each time a ZNetScene wakes.</summary>
        public static void Setup(Harmony harmony)
        {
            RaidHosts.AddHostPrefab(PrefabName);
            NetPrefabs.OnSceneAwake(harmony, scene => NetPrefabs.Register(scene, Prefab()));
        }

        /// <summary>Puts a new marker at <paramref name="at"/>; this machine owns it. Null before the scene is up.</summary>
        public static ZNetView? Place(Vector3 at)
        {
            GameObject? prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(PrefabName) : null;
            return prefab != null ? Object.Instantiate(prefab, at, Quaternion.identity).GetComponent<ZNetView>() : null;
        }

        private static GameObject Prefab()
        {
            if (_prefab != null)
            {
                return _prefab;
            }
            GameObject marker = new GameObject(PrefabName);
            marker.transform.SetParent(PrefabBench.Root, false); // inactive parent: no Awake, no ZDO, until placed
            marker.AddComponent<ZNetView>().m_persistent = true;
            marker.AddComponent<RaidRunner>().RemoveWhenDone = true;
            _prefab = marker;
            return marker;
        }
    }
}
