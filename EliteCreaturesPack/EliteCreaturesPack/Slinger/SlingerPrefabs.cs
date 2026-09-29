using System.Linq;
using BundlePrefabs;
using EliteCreaturesPack.Core;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesPack.Slinger
{
    /// <summary>
    /// Builds the Greydwarf Slinger's three prefabs once and registers them whenever ZNetScene wakes, identically on the
    /// server and every client: the creature (a copy of the game's Greydwarf wearing the slingshot and satchel from the
    /// embedded bundle, playing the bundle's shot in place of its throw), its shot (a copy of the Greydwarf's thrown
    /// rock) and the stone it fires (a copy of the rock's projectile). The bundle comes from AssetWorkshop
    /// (assets/ecr_slinger); every material is the game's own Greydwarf material wearing the bundle's textures.
    /// </summary>
    public static class SlingerPrefabs
    {
        public const string Creature = "ECP_GreydwarfSlinger";
        public const string Shot = "ECP_GreydwarfSlinger_shot";
        public const string Stone = "ECP_GreydwarfSlinger_stone";
        public const string Greydwarf = "Greydwarf";
        private const string Bundle = "ecr_slinger";

        /// <summary>The creature prefab, once built; null until the first ZNetScene wakes.</summary>
        public static GameObject? Prefab { get; private set; }

        private static GameObject? shot, stone;

        public static void Install(Harmony harmony)
        {
            NetPrefabs.OnSceneAwake(harmony, scene => Guard.Run("SlingerPrefabs.Build", () => Build(scene, harmony)));
            Settings.Changed += () => SafeCall.Run("slinger settings", SlingerShot.ApplyToAll);
        }

        private static void Build(ZNetScene scene, Harmony harmony)
        {
            if (scene.GetPrefab(Creature) != null)
            {
                return;
            }
            if (Prefab == null && !Create(scene, harmony))
            {
                return;
            }
            NetPrefabs.Register(scene, stone!);
            NetPrefabs.Register(scene, shot!);
            NetPrefabs.Register(scene, Prefab!);
            SlingerWords.Add(Localization.instance);
            Log.Info($"Greydwarf slinger ready: {Creature}, {Shot}, {Stone}.");
        }

        /// <summary>
        /// Builds the three prefabs, or logs what is missing and builds none (no slinger then, but nothing else is
        /// held up). The thrown rock is not a network prefab: it is found among the Greydwarf's own default items.
        /// </summary>
        private static bool Create(ZNetScene scene, Harmony harmony)
        {
            GameObject? greydwarf = scene.GetPrefab(Greydwarf);
            GameObject? rock = greydwarf?.GetComponent<Humanoid>()?.m_defaultItems.FirstOrDefault(item => item != null && item.name == SlingerCreature.ThrownRock);
            GameObject? projectile = scene.GetPrefab("Greydwarf_throw_projectile");
            if (greydwarf == null || rock == null || projectile == null)
            {
                Log.Error($"Greydwarf slinger not built: the game has no {(greydwarf == null ? Greydwarf : rock == null ? SlingerCreature.ThrownRock : "Greydwarf_throw_projectile")}.");
                return false;
            }
            AssetBundle bundle = EmbeddedBundle.Load(typeof(SlingerPrefabs).Assembly, Bundle);
            stone = SlingerStone.Build(projectile);
            shot = SlingerShot.Build(rock, stone, scene.GetPrefab("sfx_bow_fire"));
            ItemPrefabs.Register(harmony, shot);
            Prefab = SlingerCreature.Build(greydwarf, shot, stone, bundle, scene.GetPrefab("Stone"));
            return true;
        }
    }
}
