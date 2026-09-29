using BundlePrefabs;
using EliteCreaturesPack.Core;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesPack.Mountains
{
    public static class MountainPrefabs
    {
        private static AssetBundle? bundle;
        public static void Install(Harmony harmony)
        {
            NetPrefabs.OnSceneAwake(harmony, scene => Guard.Run("MountainPrefabs.Build", () => Build(scene, harmony)));
            Settings.Changed += () => SafeCall.Run("mountain settings", Refresh);
        }
        private static void Build(ZNetScene scene, Harmony harmony)
        {
            foreach (MountainKind kind in MountainKind.All)
            {
                if (kind.Prefab == null)
                    Create(kind, scene, harmony);
                if (kind.Prefab != null) NetPrefabs.Register(scene, kind.Prefab);
            }
            foreach (GameObject attack in MountainAttacks.Items) NetPrefabs.Register(scene, attack);
            foreach (GameObject corpse in MountainCorpses.Prefabs) NetPrefabs.Register(scene, corpse);
            MountainWords.Add(Localization.instance);
            Refresh();
            Log.Info("Mountain creatures ready: Frostfang, Rimeback, Scree Wing, Cairn Wight, Ice Crawler.");
        }
        private static void Create(MountainKind kind, ZNetScene scene, Harmony harmony)
        {
            GameObject source = scene.GetPrefab(kind.Base);
            if (source == null || source.GetComponent<Humanoid>() == null)
            {
                Log.Warn(kind.Name + " not built: missing " + kind.Base);
                return;
            }
            bundle = bundle != null ? bundle : EmbeddedBundle.Load(typeof(MountainPrefabs).Assembly, "ecp_mountain_set");
            GameObject art = EmbeddedBundle.Prefab(bundle, kind.Asset);
            GameObject[] attacks = MountainAttacks.Build(kind, source.GetComponent<Humanoid>(), harmony);
            kind.Prefab = MountainCreature.Build(kind, source, attacks, art, scene);
        }
        private static void Refresh()
        {
            foreach (MountainKind kind in MountainKind.All)
                if (kind.Prefab != null) kind.Prefab.GetComponent<Humanoid>().m_health = MountainSettings.For(kind).Health;
            MountainAttacks.Refresh();
            MountainSpawns.Refresh();
        }
    }
}
