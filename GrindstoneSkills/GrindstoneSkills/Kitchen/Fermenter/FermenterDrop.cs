using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// The game's Fermenter.DropAllItems drops the base while fermenting, or the meads when Ready, then clears the
    /// content. Nothing in the game calls it today (a destroyed barrel's OnDestroyed does nothing, so the batch is
    /// lost), but a mod or a later game version may. The barrel's <see cref="FermenterBase"/> level is cleared with the
    /// content.
    /// </summary>
    [HarmonyPatch(typeof(Fermenter), nameof(Fermenter.DropAllItems))]
    internal static class FermenterDrop
    {
        [HarmonyPostfix]
        private static void Postfix(Fermenter __instance)
        {
            if (__instance.GetContent() == 0)
                FermenterBase.Clear(__instance.m_nview.GetZDO());
        }
    }
}
