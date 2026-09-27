using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Heirloom seeds: a plant remembers the stars of the seed it was planted from. Player.UpdatePlacement places the
    /// plant (<see cref="PlantPlacing"/> marks it waiting) and then pays with Player.ConsumeResources, which removes the
    /// seed by name. While that payment runs for a waiting plant, <see cref="CraftRecord"/> records what is taken (the
    /// player's Ingredient Order picks the stack, <see cref="IngredientTakeOrder"/>), and the rounded average stars go on
    /// the plant. A placement that never pays (no-cost mode, a free-build world) leaves 0 stars: the wait ends with the
    /// frame's UpdatePlacement.
    /// </summary>
    public static class SeedStars
    {
        private static Plant waiting;

        public static void Await(Plant plant) => waiting = plant;

        /// <summary>The rounded average stars of what the open recording took.</summary>
        public static int Recorded() => Mathf.RoundToInt(CraftRecord.AverageStars());

        [HarmonyPatch(typeof(Player), nameof(Player.ConsumeResources))]
        private static class Paying
        {
            [HarmonyPrefix]
            private static void Prefix(out bool __state)
            {
                __state = waiting != null && !CraftRecord.Recording;
                if (__state)
                    CraftRecord.Start();
            }

            [HarmonyFinalizer]
            private static void Finalizer(bool __state)
            {
                if (!__state)
                    return;
                int stars = Recorded();
                CraftRecord.Clear();
                ZDO zdo = PlantKeys.Of(waiting);
                waiting = null;
                if (zdo != null)
                    PlantKeys.WriteSeed(zdo, stars);
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacement))]
        private static class FrameEnd
        {
            [HarmonyFinalizer]
            private static void Finalizer() => waiting = null;
        }
    }
}
