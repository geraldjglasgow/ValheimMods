using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Tending: the use key on a growing plant. The game hovers plants (Plant is Hoverable) but gives them nothing to do,
    /// so Player.Interact finds no Interactable and does nothing. Then, on the local player's client, every growing plant
    /// a player planted within Tending Radius of it (the plant itself included) that was not tended today and that the
    /// player may access (ward) is sent <see cref="Keys.RpcTend"/> with today's in-game day; its owner adds the growth
    /// (<see cref="PlantRpcs"/>). The player sees how many plants were tended and earns Tending Experience for each.
    /// </summary>
    public static class Tending
    {
        private static readonly Collider[] Hits = new Collider[128];
        private static readonly HashSet<Plant> found = new HashSet<Plant>();
        private static readonly HashSet<ZDOID> sentToday = new HashSet<ZDOID>();
        private static int sentDay = -1;

        public static bool Enabled => FarmSkill.Active && FarmingPerkSettings.TendingBonus.Value > 0f;

        [HarmonyPatch(typeof(Player), nameof(Player.Interact))]
        private static class Use
        {
            [HarmonyPrefix]
            private static void Prefix(Player __instance, GameObject go, bool hold)
            {
                if (hold || go == null || __instance != Player.m_localPlayer || !Enabled || __instance.InAttack() || __instance.InDodge())
                    return;
                if (go.GetComponentInParent<Interactable>() != null)
                    return;
                Plant plant = go.GetComponentInParent<Plant>();
                if (plant != null)
                    Guarded(__instance, plant);
            }

            // Its own method: a lambda capturing the prefix's parameters would allocate on every use of anything.
            private static void Guarded(Player player, Plant plant) => HookGuard.Run("Farming tending", () => Tend(player, plant));
        }

        /// <summary>
        /// Whether the plant can be tended today: planted by a player, growing and healthy, not tended since, and not sent a
        /// tend by this client today (the owner's record takes a moment to come back, and a second press must not pay
        /// experience again).
        /// </summary>
        public static bool CanTend(Plant plant, ZDO zdo, int day)
        {
            if (!PlantKeys.IsPlanted(zdo) || plant.GetStatus() != Plant.Status.Healthy || PlantKeys.TendedDay(zdo) >= day)
                return false;
            return day != sentDay || !sentToday.Contains(zdo.m_uid);
        }

        private static void Tend(Player player, Plant center)
        {
            int day = EnvMan.instance.GetDay();
            if (day != sentDay)
            {
                sentDay = day;
                sentToday.Clear();
            }
            int tended = 0;
            foreach (Plant plant in Around(center))
            {
                ZDO zdo = PlantKeys.Of(plant);
                if (!CanTend(plant, zdo, day) || !PrivateArea.CheckAccess(plant.transform.position, 0f, false))
                    continue;
                plant.m_nview.InvokeRPC(Keys.RpcTend, day);
                sentToday.Add(zdo.m_uid);
                tended++;
            }
            Report(player, center, tended, day);
        }

        private static IEnumerable<Plant> Around(Plant center)
        {
            found.Clear();
            found.Add(center);
            float radius = Mathf.Max(0f, FarmingPerkSettings.TendingRadius.Value);
            int count = radius > 0f ? Physics.OverlapSphereNonAlloc(center.transform.position, radius, Hits, ~0, QueryTriggerInteraction.Collide) : 0;
            for (int i = 0; i < count; i++)
            {
                Plant plant = Hits[i] != null ? Hits[i].GetComponentInParent<Plant>() : null;
                if (plant != null)
                    found.Add(plant);
            }
            return new List<Plant>(found);
        }

        private static void Report(Player player, Plant center, int tended, int day)
        {
            if (tended <= 0)
            {
                ZDO zdo = PlantKeys.Of(center);
                if (PlantKeys.TendedDay(zdo) >= day || (zdo != null && sentToday.Contains(zdo.m_uid)))
                    player.Message(MessageHud.MessageType.Center, "Already tended today");
                return;
            }
            player.DoInteractAnimation(center.gameObject);
            player.Message(MessageHud.MessageType.Center, tended == 1 ? "Tended 1 plant" : $"Tended {tended} plants");
            FarmXp.RaiseScaled(player, Mathf.Max(0f, FarmingExperienceSettings.TendingExperience.Value) * tended);
        }
    }
}
