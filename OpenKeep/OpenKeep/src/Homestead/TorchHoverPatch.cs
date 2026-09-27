using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// <c>Fireplace.GetHoverText</c>: a torch put out for the day says so ("Lights at nightfall"), since fuel added by
    /// hand does not light it and vanilla torches have no switch; a torch the schedule switches shows Torch Switch Key
    /// and what it does ("Keep lit", or "Light at night only" on a torch kept lit), unless a ward refuses the player, as
    /// the game's own hovers check without flashing. Every client reads the replicated ZDO; hover only.
    /// </summary>
    [HarmonyPatch(typeof(Fireplace), nameof(Fireplace.GetHoverText))]
    public static class TorchHoverPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Fireplace __instance, ref string __result)
        {
            if (string.IsNullOrEmpty(__result))
                return;
            if (TorchSwitch.OutForDay(__instance))
                __result += "\n" + Language.Localize(TorchFeature.NightfallWord);
            __result += SwitchLine(__instance);
        }

        private static string SwitchLine(Fireplace fire)
        {
            KeyboardShortcut key = TorchSettings.SwitchKey.Value;
            if (key.MainKey == KeyCode.None || !TorchKeep.Switchable(fire))
                return "";
            if (!PrivateArea.CheckAccess(fire.transform.position, 0f, false))
                return "";
            string action = TorchKeep.IsKept(fire.m_nview.GetZDO()) ? TorchFeature.NightOnlyWord : TorchFeature.KeepLitWord;
            return $"\n[<color=yellow><b>{Label(key)}</b></color>] {Language.Localize(action)}";
        }

        /// <summary>Modifiers first, as the README writes keys ("LeftAlt + O"); BepInEx's own text puts the main key first.</summary>
        private static string Label(KeyboardShortcut key)
        {
            return string.Join(" + ", key.Modifiers.Select(k => k.ToString()).Append(key.MainKey.ToString()));
        }
    }
}
