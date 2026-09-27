using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Legendary fish are born, never made. The game spawns fish with SpawnSystem.Spawn on the machine that runs the
    /// zone's spawning, instantiating the prefab and then setting its level (item quality) for levels above 1. The prefix
    /// opens a scope, Fish.Awake notes every fish instantiated inside it, and the postfix makes each one legendary with
    /// Legendary Chance: level 6, three times the size (the game's own +40% per level). The item data reaches the ZDO at
    /// once, so every client loads it legendary. Fish dropped back into the water are instantiated elsewhere
    /// (ItemDrop.DropItem) and never roll.
    /// </summary>
    public static class LegendarySpawn
    {
        private static readonly List<Fish> born = new List<Fish>();
        private static bool spawning;

        [HarmonyPatch(typeof(SpawnSystem), nameof(SpawnSystem.Spawn))]
        private static class Spawning
        {
            [HarmonyPrefix]
            private static void Prefix(out bool __state)
            {
                __state = !spawning;
                spawning = true;
                born.Clear();
            }

            [HarmonyPostfix]
            private static void Postfix(bool __state)
            {
                if (__state && FishSkill.Active && born.Count > 0)
                    HookGuard.Run("legendary fish", Roll);
            }

            [HarmonyFinalizer]
            private static void Finalizer(bool __state)
            {
                if (!__state)
                    return;
                spawning = false;
                born.Clear();
            }
        }

        [HarmonyPatch(typeof(Fish), nameof(Fish.Awake))]
        private static class Born
        {
            [HarmonyPostfix]
            private static void Postfix(Fish __instance)
            {
                if (spawning)
                    born.Add(__instance);
            }
        }

        private static void Roll()
        {
            float chance = FishSkill.Percent(FishingBigFishSettings.LegendaryChance.Value);
            foreach (Fish fish in born)
            {
                if (fish != null && Random.value < chance)
                    MakeLegendary(fish);
            }
        }

        private static void MakeLegendary(Fish fish)
        {
            ItemDrop item = FishInfo.Item(fish);
            if (item == null || fish.m_nview == null || !fish.m_nview.IsValid())
                return;
            item.SetQuality(FishInfo.LegendaryLevel);
            ItemDrop.SaveToZDO(item.m_itemData, fish.m_nview.GetZDO());
        }
    }
}
