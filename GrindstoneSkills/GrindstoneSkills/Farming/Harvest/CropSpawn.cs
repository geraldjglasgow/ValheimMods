using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Stars on picked crops, on the crop's owner. Pickable.RPC_Pick spawns the pick (the item, the game's bonus and the
    /// extra drops) through Drop, which instantiates each item and calls ItemDrop.OnCreateNew on it; the item saves itself
    /// to its ZDO a frame later. While RPC_Pick runs for a crop with stars (<see cref="CropKeys"/>), every new item that
    /// can carry stars gets the crop's, is rescaled and saved at once, as the cooking stations do
    /// (<see cref="StationSpawnStars"/>). A giant also drops its extra crop, (Giant Crop Yield - 1) times the crop's
    /// amount, as one stack, in a postfix and only when the game's body really picked the crop (m_picked turned true:
    /// the owner runs RPC_SetPicked at once), so a pick another mod skips or the game's body throws drops nothing extra.
    /// The giant flag is read in the prefix: the pick destroys the crop's ZDO.
    /// </summary>
    public static class CropSpawn
    {
        private static int open = -1;
        private static bool giant;

        [HarmonyPatch(typeof(Pickable), nameof(Pickable.RPC_Pick))]
        private static class Pick
        {
            [HarmonyPrefix]
            private static void Prefix(Pickable __instance, out bool __state)
            {
                __state = false;
                ZNetView nview = __instance.m_nview;
                if (open >= 0 || !FarmSkill.Active || nview == null || !nview.IsValid() || !nview.IsOwner() || __instance.m_picked)
                    return;
                if (CropCatalog.OfPickable(__instance) == null)
                    return;
                __state = true;
                open = CropKeys.Stars(nview);
                giant = CropKeys.Giant(nview);
            }

            [HarmonyPostfix]
            private static void Postfix(Pickable __instance, bool __state)
            {
                if (__state && giant && __instance.m_picked)
                    HookGuard.Run("Farming giant", static pickable => DropGiant(pickable), __instance);
            }

            [HarmonyFinalizer]
            private static void Finalizer(bool __state)
            {
                if (__state)
                {
                    open = -1;
                    giant = false;
                }
            }
        }

        /// <summary>
        /// The giant's extra crop: (Giant Crop Yield - 1) times the amount the game drops (scaled by the world's resource
        /// rate, as RPC_Pick scales it), in stacks no bigger than the item's own.
        /// </summary>
        private static void DropGiant(Pickable pickable)
        {
            GameObject prefab = pickable.m_itemPrefab;
            ItemDrop item = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            if (item == null)
                return;
            int amount = pickable.m_dontScale ? pickable.m_amount : Mathf.Max(pickable.m_minAmountScaled, Game.instance.ScaleDrops(prefab, pickable.m_amount));
            int extra = (FarmingSettings.GiantYield.Value - 1) * Mathf.Max(1, amount);
            int stack = Mathf.Max(1, item.m_itemData.m_shared.m_maxStackSize);
            for (int offset = 0; extra > 0; offset++)
            {
                pickable.Drop(prefab, offset, Mathf.Min(stack, extra));
                extra -= stack;
            }
        }

        /// <summary>A new item, from <see cref="ItemCreated"/> (ItemDrop.OnCreateNew).</summary>
        internal static void OnCreated(ItemDrop item)
        {
            if (open > 0 && item?.m_itemData != null)
                HookGuard.Run("Farming crop stars", static drop => Apply(drop, open), item);
        }

        /// <summary>Gives a new item stars, when it can carry them, and saves it at once.</summary>
        public static void Apply(ItemDrop item, int stars)
        {
            if (stars <= 0 || !Kitchen.IsKitchenItem(item.m_itemData))
                return;
            Stars.Set(item.m_itemData, stars);
            item.SetQuality(item.m_itemData.m_quality);
            item.Save();
        }
    }
}
