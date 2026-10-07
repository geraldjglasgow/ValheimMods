using HarmonyLib;

namespace OpenKeep.Mimir
{
    /// <summary>
    /// A Mímir's Chest fills from the top. The game puts a new stack in the first empty cell from the top for weapons,
    /// tools and the like, and from the bottom row up for everything else (Inventory.TopFirst, FindEmptySlot). The chest
    /// keeps two empty rows below its lowest item (<see cref="MimirRows"/>), so every material added bottom-first landed
    /// in a new bottom row and grew the chest by two rows, to the 64-row ceiling within a few dozen stacks. Here every
    /// empty-cell search of a Mímir inventory runs top-first, so the chest grows only as it really fills.
    /// </summary>
    [HarmonyPatch(typeof(Inventory), "FindEmptySlot")]
    public static class MimirFill
    {
        [HarmonyPrefix]
        private static void Prefix(Inventory __instance, ref bool topFirst)
        {
            if (!topFirst && __instance.m_name == MimirPrefab.ContainerName)
                topFirst = true;
        }
    }
}
