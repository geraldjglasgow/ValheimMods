using BundlePrefabs;
using EliteCreaturesPack.Core;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesPack.Mimic
{
    /// <summary>
    /// Builds and registers the crypt mimic's three prefabs when ZNetScene wakes, identically on the server and every
    /// client: the creature (a copy of the Black Forest crypt skeleton, so it keeps that skeleton's health, resistances,
    /// faction and effects, wearing our rig and the game's own crypt chest), its bite (a copy of the skeleton's sword,
    /// stripped of its model), and the corpse it leaves when it dies. The rig, parts and animations come from the
    /// embedded asset bundle built by AssetWorkshop; the chest meshes and every material are borrowed from the game.
    /// </summary>
    public static class MimicPrefabs
    {
        public const string Creature = "ECP_CryptMimic";
        public const string Corpse = "ECP_CryptMimic_ragdoll";
        public const string Bite = "ECP_CryptMimic_bite";
        public const string Chest = "TreasureChest_forestcrypt";
        private const string Bundle = "ecr_cryptmimic";
        private const string Model = "crypt_mimic";

        /// <summary>Builds on every ZNetScene wake; a rule reload puts the new bite numbers on every mimic at once.</summary>
        public static void Install(Harmony harmony)
        {
            NetPrefabs.OnSceneAwake(harmony, scene => Guard.Run("MimicPrefabs.Build", () => Build(scene, harmony)));
            Settings.Changed += () => SafeCall.Run("mimic settings", MimicBite.ApplySettings);
        }

        private static void Build(ZNetScene scene, Harmony harmony)
        {
            if (scene.GetPrefab(Creature) != null)
            {
                return;
            }
            GameObject model = EmbeddedBundle.Prefab(EmbeddedBundle.Load(typeof(MimicPrefabs).Assembly, Bundle), Model);
            GameObject chest = scene.GetPrefab(Chest);
            GameObject bite = MimicBite.Build(scene.GetPrefab("skeleton_sword"));
            ItemPrefabs.Register(harmony, bite);
            NetPrefabs.Register(scene, bite);
            GameObject corpse = MimicCorpse.Build(scene.GetPrefab("Draugr_ragdoll"), MimicBody.Build(scene, model, chest));
            NetPrefabs.Register(scene, corpse);
            GameObject creature = MimicCreature.Build(scene.GetPrefab("Skeleton"), MimicBody.Build(scene, model, chest), bite, corpse);
            NetPrefabs.Register(scene, creature);
            MimicWords.Add(Localization.instance);
            Log.Info($"Crypt mimic ready: {Creature}, {Bite}, {Corpse}.");
        }
    }
}
