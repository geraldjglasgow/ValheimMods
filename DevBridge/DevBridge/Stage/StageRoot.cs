using DevBridge.Server;
using UnityEngine;

namespace DevBridge.Stage
{
    /// <summary>
    /// Where everything placed for looking at lives: one root object in the world scene (so logging out removes it),
    /// with an inactive bench under it where copies are made and stripped before anything in them wakes.
    /// </summary>
    internal static class StageRoot
    {
        private static GameObject root;
        private static GameObject bench;

        internal static Transform Root => Ensure().transform;

        internal static Transform Bench
        {
            get
            {
                Ensure();
                return bench.transform;
            }
        }

        internal static bool Exists => root;

        private static GameObject Ensure()
        {
            if (root) return root;
            if (!ZNetScene.instance || !Player.m_localPlayer) throw new BridgeException("no world loaded: the stage needs the local player in a world");
            root = new GameObject("DevBridge_Stage");
            bench = new GameObject("bench");
            bench.SetActive(false);
            bench.transform.SetParent(root.transform, false);
            return root;
        }
    }
}
