using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// A crop ripens. Plant.Grow runs on the plant's owner: it instantiates the grown pickable (this machine owns its new
    /// ZDO), scales it and destroys the plant, ZDO and all. The prefix reads the planter's level while the plant's ZDO
    /// still exists; the postfix, given the new pickable, rolls whether it is a giant, writes that with the plant it grew
    /// from (<see cref="CropKeys"/>), and sizes a giant (<see cref="CropLook"/>). Only crops a player with Farming planted
    /// roll anything.
    /// </summary>
    [HarmonyPatch(typeof(Plant), nameof(Plant.Grow))]
    public static class CropRipening
    {
        [HarmonyPrefix]
        private static void Prefix(Plant __instance, out float? __state)
        {
            __state = null;
            ZDO zdo = PlantKeys.Of(__instance);
            if (FarmSkill.Active && PlantKeys.IsPlanted(zdo) && CropCatalog.OfPlant(__instance) != null)
                __state = PlantKeys.Level(zdo);
        }

        [HarmonyPostfix]
        private static void Postfix(Plant __instance, GameObject __result, float? __state)
        {
            if (__result != null && __state.HasValue)
                HookGuard.Run("Farming ripening", static grow => Ripen(grow.plant, grow.grown, grow.level),
                    (plant: __instance, grown: __result, level: __state.Value));
        }

        private static void Ripen(Plant plant, GameObject grown, float planterLevel)
        {
            CropPlant crop = CropCatalog.OfPlant(plant);
            ZNetView nview = grown.GetComponent<ZNetView>();
            if (crop == null || nview == null || !nview.IsValid())
                return;
            bool giant = RollGiant(planterLevel);
            CropKeys.Write(nview.GetZDO(), giant, crop.PrefabHash);
            if (giant && CropLook.GiantSize > 1f)
                nview.SetLocalScale(grown.transform.localScale * CropLook.GiantSize);
        }

        /// <summary>Whether a crop planted at this level ripens into a giant, rolled once.</summary>
        private static bool RollGiant(float planterLevel) =>
            Random.value < FarmSkill.Share(FarmingSettings.GiantChanceAt100.Value, planterLevel);
    }
}
