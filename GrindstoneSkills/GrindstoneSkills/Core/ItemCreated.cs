using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// The one patch on ItemDrop.OnCreateNew, which the game calls for every item it makes in the world (drops, picks,
    /// station products) on every machine. Each feature that gives new items stars opens a scope around the game's
    /// spawn and tests it first thing, so outside a scope every new item costs one test per feature and nothing more.
    /// </summary>
    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.OnCreateNew), typeof(ItemDrop), typeof(bool))]
    public static class ItemCreated
    {
        [HarmonyPostfix]
        private static void Postfix(ItemDrop item)
        {
            if (item == null)
                return;
            CropSpawn.OnCreated(item);
            MillSpawn.OnCreated(item);
            ForageSpawn.OnCreated(item);
            FermenterSpawnStars.OnCreated(item);
            StationSpawnStars.OnCreated(item);
        }
    }
}
