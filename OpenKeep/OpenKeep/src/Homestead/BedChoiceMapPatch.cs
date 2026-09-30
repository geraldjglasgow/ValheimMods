using HarmonyLib;
using UnityEngine;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// <c>Minimap.Update</c> prefix on the local client: while the choice of bed is open, the map is updated by
    /// <see cref="BedChoiceMap"/> instead of the game's update, which would close it for the dead player. A choice
    /// that ended from elsewhere (a respawn, a revival, a quit) is closed here. Everything else, and the first frames
    /// before the map is generated, keep the game's update.
    /// </summary>
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.Update))]
    public static class BedChoiceMapPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Minimap __instance)
        {
            if (!BedChoice.Active)
            {
                BedChoice.Close();
                return true;
            }
            if (!__instance.m_hasGenerated || ZInput.VirtualKeyboardOpen || Utils.GetMainCamera() == null)
                return true;
            BedChoiceMap.Update(__instance, Player.m_localPlayer, Time.deltaTime);
            return false;
        }
    }
}
