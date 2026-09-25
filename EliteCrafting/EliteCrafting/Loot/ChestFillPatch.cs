using System;
using EliteCrafting.Core;
using HarmonyLib;

namespace EliteCrafting.Loot
{
    /// <summary>
    /// The game fills a world container with its default items in <c>Container.AddDefaultItems</c>, called once from the
    /// container's <c>Awake</c> on the ZDO owner, only while the ZDO's "default items added" flag is unset (verified in
    /// the decompile); the flag is set right after. A postfix there rolls our loot into the same container
    /// (<see cref="ChestRoller"/>). A failure is logged and swallowed: a throw here would abort the container's
    /// <c>Awake</c> and leave the chest broken.
    /// </summary>
    [HarmonyPatch(typeof(Container), nameof(Container.AddDefaultItems))]
    internal static class ChestFillPatch
    {
        private static void Postfix(Container __instance)
        {
            try
            {
                ChestRoller.OnDefaultItems(__instance);
            }
            catch (Exception e)
            {
                Log.Error($"chest drop roll failed: {e}");
            }
        }
    }
}
