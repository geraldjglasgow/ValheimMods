using BundlePrefabs;
using EliteCreaturesPack.Core;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesPack.Swamp
{
    /// <summary>Build the same saved-world identities on every peer, including dedicated servers.</summary>
    public static class SwampPrefabs
    {
        private static AssetBundle? bundle;

        public static void Install(Harmony harmony)
        {
            NetPrefabs.OnSceneAwake(harmony, scene => Guard.Run("SwampPrefabs.Build", () => Build(scene, harmony)));
            Settings.Changed += () => SafeCall.Run("swamp settings", Refresh);
        }

        private static void Build(ZNetScene scene, Harmony harmony)
        {
            bundle = bundle != null ? bundle : EmbeddedBundle.Load(typeof(SwampPrefabs).Assembly, "ecp_swamp");
            foreach (SwampKind kind in SwampKind.All)
                SafeCall.Run("build " + kind.Creature, () => Create(kind, scene));
            foreach (GameObject item in SwampAttacks.Items)
            {
                NetPrefabs.Register(scene, item);
                ItemPrefabs.Register(harmony, item);
            }
            SwampWords.Add(Localization.instance);
            Refresh();
        }

        private static void Create(SwampKind kind, ZNetScene scene)
        {
            if (scene.GetPrefab(kind.Creature) != null) return;
            GameObject source = scene.GetPrefab(kind.Base);
            if (source == null) { Log.Error("Missing swamp base: " + kind.Base); return; }
            if (kind.Prefab == null) kind.Prefab = SwampCreature.Build(kind, source, bundle!, scene);
            NetPrefabs.Register(scene, kind.Prefab);
            Log.Info("Swamp creature ready: " + kind.Creature);
        }

        private static void Refresh()
        {
            foreach (SwampKind kind in SwampKind.All)
                if (kind.Prefab != null) kind.Prefab.GetComponent<Character>().m_health = SwampSettings.For(kind).Health;
            SwampAttacks.Refresh();
            SwampSpawns.Refresh();
        }
    }
}
