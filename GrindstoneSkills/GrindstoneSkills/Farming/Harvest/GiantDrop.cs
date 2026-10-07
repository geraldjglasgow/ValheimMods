using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// A giant's extra crop, on the crop's owner. Pickable.RPC_Pick spawns the pick (the item, the game's bonus and the
    /// extra drops). For a giant (<see cref="CropKeys"/>) the extra crop, (Giant Crop Yield - 1) times the crop's amount,
    /// drops as one stack in a postfix, and only when the game's body really picked the crop (m_picked turned true: the
    /// owner runs RPC_SetPicked at once), so a pick another mod skips or the game's body throws drops nothing extra. The
    /// giant flag is read in the prefix: the pick destroys the crop's ZDO.
    /// </summary>
    public static class GiantDrop
    {
        private static bool open;

        [HarmonyPatch(typeof(Pickable), nameof(Pickable.RPC_Pick))]
        private static class Pick
        {
            [HarmonyPrefix]
            private static void Prefix(Pickable __instance, out bool __state)
            {
                __state = false;
                ZNetView nview = __instance.m_nview;
                if (open || !FarmSkill.Active || nview == null || !nview.IsValid() || !nview.IsOwner() || __instance.m_picked)
                    return;
                if (!CropKeys.Giant(nview) || CropCatalog.OfPickable(__instance) == null)
                    return;
                __state = true;
                open = true;
            }

            [HarmonyPostfix]
            private static void Postfix(Pickable __instance, bool __state)
            {
                if (__state && __instance.m_picked)
                    HookGuard.Run("Farming giant", static pickable => Drop(pickable), __instance);
            }

            [HarmonyFinalizer]
            private static void Finalizer(bool __state)
            {
                if (__state)
                    open = false;
            }
        }

        /// <summary>
        /// The giant's extra crop: (Giant Crop Yield - 1) times the amount the game drops (scaled by the world's resource
        /// rate, as RPC_Pick scales it), in stacks no bigger than the item's own.
        /// </summary>
        private static void Drop(Pickable pickable)
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
    }
}
