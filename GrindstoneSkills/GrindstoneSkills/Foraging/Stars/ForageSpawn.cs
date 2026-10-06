using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Stars on picked forage, on the plant's owner. Pickable.RPC_Pick spawns the pick (the item, the game's extra
    /// item and the plant's extra drops) through Drop, which instantiates each item and calls ItemDrop.OnCreateNew on
    /// it; the item saves itself to its ZDO a frame later. While RPC_Pick runs with a mark from its sender
    /// (<see cref="ForageMarks"/>), every new item the Forage file gives stars rolls its own from the odds table at the
    /// mark's level (<see cref="StarOdds"/>) and is saved at once, as the cooking stations do
    /// (<see cref="StationSpawnStars"/>). Without a mark (a vanilla client, a lost mark) the pick is plain.
    /// </summary>
    public static class ForageSpawn
    {
        private static bool open;
        private static float level;

        [HarmonyPatch(typeof(Pickable), nameof(Pickable.RPC_Pick))]
        private static class Pick
        {
            [HarmonyPrefix]
            private static void Prefix(Pickable __instance, long sender, out bool __state)
            {
                __state = false;
                if (open || !__instance.m_nview.IsOwner() || __instance.m_picked || !ForageMarks.TryTake(__instance, sender, out float marked))
                    return;
                open = __state = true;
                level = marked;
            }

            [HarmonyFinalizer]
            private static void Finalizer(bool __state)
            {
                if (__state)
                    open = false;
            }
        }

        /// <summary>A new item, from <see cref="ItemCreated"/> (ItemDrop.OnCreateNew).</summary>
        internal static void OnCreated(ItemDrop item)
        {
            if (open && item?.m_itemData?.m_dropPrefab != null)
                HookGuard.Run("Foraging stars", static drop => Apply(drop), item);
        }

        private static void Apply(ItemDrop item)
        {
            ForageEntry entry = ForageFile.Find(item.m_itemData.m_dropPrefab.name);
            if (entry == null || !entry.Stars || !Kitchen.IsKitchenItem(item.m_itemData))
                return;
            int stars = StarOdds.Roll(level);
            if (stars <= 0)
                return;
            Stars.Set(item.m_itemData, stars);
            item.SetQuality(item.m_itemData.m_quality);
            item.Save();
        }
    }
}
