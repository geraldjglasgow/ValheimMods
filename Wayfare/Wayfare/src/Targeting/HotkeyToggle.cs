using HarmonyLib;
using Wayfare.Core;

namespace Wayfare.Targeting
{
    /// <summary>Display-only preference: <see cref="Core.WayfareConfig.ToggleIconsKey"/> (default P) shows or
    /// hides portal icons on the ordinary (non-targeting) large map. Bound unsynced - each player decides for
    /// themselves - and only reacts while the large map is open and not currently in a targeting session, so the
    /// key does nothing unexpected outside the map or while targeting (icons are already always shown there).</summary>
    public static class HotkeyToggle
    {
        public static bool IconsOn { get; private set; }

        public static void Toggle() => IconsOn = !IconsOn;
    }

    [HarmonyPatch(typeof(Player), "Update")]
    public static class HotkeyTogglePatch
    {
        [HarmonyPostfix]
        public static void Postfix(Player __instance)
        {
            if (__instance != Player.m_localPlayer || !WayfareConfig.Enabled.Value)
                return;
            if (!Minimap.IsOpen() || TargetingSession.Active)
                return;
            if (Keys.Pressed(WayfareConfig.ToggleIconsKey))
                HotkeyToggle.Toggle();
        }
    }
}
