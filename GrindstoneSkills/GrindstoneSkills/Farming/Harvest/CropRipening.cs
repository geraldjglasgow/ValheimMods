using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// A crop ripens. Plant.Grow runs on the plant's owner: it instantiates the grown pickable (this machine owns its new
    /// ZDO), scales it and destroys the plant, ZDO and all. The prefix reads what the plant brings (planter level, seed
    /// stars, compost) while its ZDO still exists; the postfix, given the new pickable, rolls its stars
    /// (<see cref="CropRoll"/>, companions counted where it stands) and whether it is a giant, and writes them with the
    /// plant it grew from (<see cref="CropKeys"/>), and sizes it (<see cref="CropLook"/>). Only crops a player with Farming
    /// planted roll anything.
    /// </summary>
    [HarmonyPatch(typeof(Plant), nameof(Plant.Grow))]
    public static class CropRipening
    {
        [HarmonyPrefix]
        private static void Prefix(Plant __instance, out CropRoll.Grower? __state)
        {
            __state = null;
            ZDO zdo = PlantKeys.Of(__instance);
            if (FarmSkill.Active && PlantKeys.IsPlanted(zdo) && CropCatalog.OfPlant(__instance) != null)
                __state = CropRoll.Grower.From(zdo);
        }

        [HarmonyPostfix]
        private static void Postfix(Plant __instance, GameObject __result, CropRoll.Grower? __state)
        {
            if (__result != null && __state.HasValue)
                HookGuard.Run("Farming ripening", () => Ripen(__instance, __result, __state.Value));
        }

        private static void Ripen(Plant plant, GameObject grown, CropRoll.Grower grower)
        {
            CropPlant crop = CropCatalog.OfPlant(plant);
            ZNetView nview = grown.GetComponent<ZNetView>();
            if (crop == null || nview == null || !nview.IsValid())
                return;
            bool rolls = CropRoll.Rolls(crop);
            int stars = rolls ? StarOdds.Roll(CropRoll.Effective(grower, Companions.Count(grown.transform.position, crop))) : 0;
            bool giant = CropRoll.RollGiant(grower.Level);
            CropKeys.Write(nview.GetZDO(), stars, giant, crop.PrefabHash);
            float size = CropLook.Size(stars, giant);
            if (size > 1f)
                nview.SetLocalScale(grown.transform.localScale * size);
        }
    }
}
