using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Stars on what a star mill makes, on its owner. Smelter.QueueProcessed takes each finished item: a mill that spawns
    /// stacks (the windmill, m_spawnStack) gathers them in s_spawnOre / s_spawnAmount and spawns the stack when the item
    /// changes or it is full. A stack keeps one star count: when the next product's stars differ, the gathered stack is
    /// spawned first (Smelter.SpawnProcessed), and <see cref="Keys.MillSpawnStars"/> holds the gathering stack's stars.
    /// Smelter.Spawn instantiates the product and calls ItemDrop.OnCreateNew; while it runs, a product that can carry stars
    /// gets them (the gathered stack's, or for a mill that spawns one at a time the item just taken). When the mill
    /// breaks, Smelter.DropAllItems drops its queued items one by one, each with the stars <see cref="MillQueue"/> took.
    /// </summary>
    internal static class MillSpawn
    {
        private static readonly int SpawnHash = Keys.MillSpawnStars.GetStableHashCode();

        private static int spawning = -1;
        private static bool dropping;

        [HarmonyPatch(typeof(Smelter), nameof(Smelter.QueueProcessed))]
        private static class Processed
        {
            [HarmonyPrefix]
            private static void Prefix(Smelter __instance, string ore)
            {
                ZDO zdo = __instance.m_spawnStack ? MillQueue.Tracked(__instance) : null;
                if (zdo == null)
                    return;
                int stars = MillQueue.LastTaken;
                int gathered = zdo.GetInt(ZDOVars.s_spawnAmount);
                if (gathered > 0 && (zdo.GetInt(SpawnHash) != stars || zdo.GetString(ZDOVars.s_spawnOre) != ore))
                    __instance.SpawnProcessed();
                zdo.Set(SpawnHash, stars);
            }
        }

        [HarmonyPatch(typeof(Smelter), nameof(Smelter.Spawn))]
        private static class Spawned
        {
            [HarmonyPrefix]
            private static void Prefix(Smelter __instance)
            {
                ZDO zdo = MillQueue.Tracked(__instance);
                spawning = zdo == null ? -1 : __instance.m_spawnStack ? zdo.GetInt(SpawnHash) : MillQueue.LastTaken;
            }

            [HarmonyFinalizer]
            private static void Finalizer() => spawning = -1;
        }

        [HarmonyPatch(typeof(Smelter), nameof(Smelter.DropAllItems))]
        private static class Dropping
        {
            [HarmonyPrefix]
            private static void Prefix(Smelter __instance) => dropping = MillQueue.Tracked(__instance) != null;

            [HarmonyFinalizer]
            private static void Finalizer() => dropping = false;
        }

        /// <summary>A new item, from <see cref="ItemCreated"/> (ItemDrop.OnCreateNew).</summary>
        internal static void OnCreated(ItemDrop item)
        {
            int stars = spawning >= 0 ? spawning : dropping ? MillQueue.LastTaken : 0;
            if (stars > 0 && item?.m_itemData != null)
                HookGuard.Run("Farming mill stars", static drop => CropSpawn.Apply(drop.item, drop.stars), (item, stars));
        }
    }
}
