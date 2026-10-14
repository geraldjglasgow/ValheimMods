using System.Collections.Generic;
using System.Linq;
using BundlePrefabs;
using EliteCreaturesReborn.Patches;
using EliteCreaturesReborn.Util;
using HarmonyLib;
using UnityEngine;

namespace EliteCreaturesReborn.Creatures.BatFiend
{
    /// <summary>
    /// The Bat-fiend, one of Hel's creatures (ValheimAssets Assets/Creatures/HelsCreatures/BatFiend, v004): its prefabs
    /// built once and registered whenever ZNetScene wakes, identically on the server and every client, so its ZDOs load
    /// into the same prefab everywhere: the creature <c>ECR_BatFiend</c> (<see cref="BatFiendCreature"/>), its corpse
    /// <c>ECR_BatFiend_corpse</c>, its five attacks (copies of the Wolf's attack item, kept in every ObjectDB) and its
    /// pitched-down voices. Everything happens inside a guarded scene builder, so a failure leaves the Bat-fiend out and the
    /// world loads. Spawnable for testing only (<c>spawn ECR_BatFiend</c>, <c>elite spawn ECR_BatFiend 2 Mad</c>); in no
    /// spawn list, raid or arena wave.
    /// </summary>
    internal static class BatFiendPrefabs
    {
        public const string Creature = "ECR_BatFiend", Corpse = "ECR_BatFiend_corpse";
        private const string GameWolf = "Wolf", GameBat = "Bat";

        /// <summary>The creature prefab, once built; null until the first ZNetScene wakes, or when it could not be built.</summary>
        public static GameObject? Prefab { get; private set; }

        private static readonly List<GameObject> net = new List<GameObject>();
        private static bool _failed;

        /// <summary>Plugin start: build and register on every scene wake.</summary>
        public static void Install(Harmony harmony) =>
            NetPrefabs.OnSceneAwake(harmony, scene => SafeCall.Run("Bat-fiend prefabs", () => Build(scene, harmony)));

        private static void Build(ZNetScene scene, Harmony harmony)
        {
            if (Prefab == null && !Create(scene, harmony))
            {
                return;
            }
            foreach (GameObject prefab in net)
            {
                NetPrefabs.Register(scene, prefab);
            }
            BatFiendWords.Add(Localization.instance);
        }

        /// <summary>Builds every prefab, or logs what is missing and builds none (once: a missing bundle stays missing).</summary>
        private static bool Create(ZNetScene scene, Harmony harmony)
        {
            if (_failed)
            {
                return false;
            }
            GameObject? wolf = scene.GetPrefab(GameWolf);
            GameObject? attack = BatFiendAttacks.WolfAttack(wolf);
            string? missing = BatFiendCreature.Missing(wolf, attack);
            Material? skin = wolf != null ? BatFiendModel.WolfSkin(wolf) : null;
            if (missing != null || skin == null || !BatFiendModel.Load(skin, Layer(wolf!)))
            {
                Log.Error($"Bat-fiend not built: the game's {GameWolf} {missing ?? "has no creature body material, or the model did not load"}.");
                _failed = true;
                return false;
            }
            Assemble(scene, harmony, wolf!, attack!);
            return true;
        }

        private static void Assemble(ZNetScene scene, Harmony harmony, GameObject wolf, GameObject attack)
        {
            BatFiendVoice.Build(scene, scene.GetPrefab(GameBat));
            BatFiendStatuses.Register(harmony);
            BatFiendAim.Install(harmony);
            GameObject[] attacks = BatFiendAttacks.Build(attack);
            BatFiendAttacks.Voice(attacks, BatFiendVoice.Of("attack") ?? BatFiendVoice.Of("alert"), BatFiendVoice.Scream);
            foreach (GameObject item in attacks)
            {
                ItemPrefabs.Register(harmony, item);
            }
            GameObject corpse = BatFiendCorpse.Build(wolf);
            Prefab = BatFiendCreature.Build(wolf, attacks, corpse, scene);
            net.AddRange(new[] { corpse, Prefab });
            net.AddRange(BatFiendVoice.Prefabs);
            Log.Info($"Bat-fiend ready: {string.Join(", ", net.Select(p => p.name))}; attacks {string.Join(", ", attacks.Select(a => a.name))}.");
        }

        /// <summary>The layer the game Wolf's body is drawn on (the model and corpse take it).</summary>
        private static int Layer(GameObject wolf)
        {
            Transform? visual = wolf.transform.Find("Visual");
            return visual != null ? visual.gameObject.layer : wolf.layer;
        }
    }
}
