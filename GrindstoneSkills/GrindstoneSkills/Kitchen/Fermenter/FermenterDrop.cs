using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// The game's Fermenter.DropAllItems drops the base while fermenting, or m_producedItems meads when Ready, through
    /// ItemDrop.OnCreateNew, then clears the content. Nothing in the game calls it today (a destroyed barrel's
    /// OnDestroyed does nothing, so the batch is lost), but a mod or a later game version may. It runs inside a
    /// <see cref="FermenterSpawnStars"/> scope, so whatever drops keeps the base's stars, and the barrel's keys are
    /// cleared with the content.
    /// </summary>
    [HarmonyPatch(typeof(Fermenter), nameof(Fermenter.DropAllItems))]
    internal static class FermenterDrop
    {
        [HarmonyPrefix]
        private static void Prefix(Fermenter __instance, out int? __state)
        {
            __state = FermenterSpawnStars.Begin(FermenterBase.GetStars(__instance.m_nview.GetZDO()));
        }

        [HarmonyPostfix]
        private static void Postfix(Fermenter __instance)
        {
            if (__instance.GetContent() == 0)
                FermenterBase.Clear(__instance.m_nview.GetZDO());
        }

        [HarmonyFinalizer]
        private static void Finalizer(int? __state) => FermenterSpawnStars.End(__state);
    }
}
