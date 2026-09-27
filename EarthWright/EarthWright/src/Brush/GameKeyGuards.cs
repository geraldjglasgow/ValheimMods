using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace EarthWright.Brush
{
    /// <summary>
    /// Keeps the game's own hotkeys from firing together with a brush key while a terrain entry is selected:
    /// <list type="bullet">
    /// <item>F9 (the hard level key) is the game's "next controller layout" key in <c>KeyHints.Update</c>: on a frame
    /// the brush's hard level key goes down, that update only refreshes its hints.</item>
    /// <item>In the game's debug mode (devcommands, debugmode) Z, B, K and L are the game's fly, no-cost, kill-enemies
    /// and remove-drops keys in <c>Player.Update</c>: on a frame one of them goes down as a brush key, debug mode is
    /// switched off for that one update (it is read nowhere else there) and back on right after.</item>
    /// </list>
    /// Local player only; nothing changes while no terrain entry is selected.
    /// </summary>
    public static class GameKeyGuards
    {
        private static readonly KeyCode[] DebugKeys = { KeyCode.Z, KeyCode.B, KeyCode.K, KeyCode.L };

        internal static bool HardLevelKeyDown()
        {
            KeyboardShortcut key = ControlSettings.HardLevelKey?.Value ?? KeyboardShortcut.Empty;
            return BrushState.Active && key.MainKey != KeyCode.None && Input.GetKeyDown(key.MainKey);
        }

        /// <summary>A brush key that is also one of the game's debug-mode keys went down this frame.</summary>
        internal static bool DebugKeyDown()
        {
            if (!BrushState.Active || ControlSettings.NextValueKey == null)
                return false;
            foreach (KeyCode code in DebugKeys)
            {
                if (Input.GetKeyDown(code) && IsBrushKey(code))
                    return true;
            }
            return false;
        }

        private static bool IsBrushKey(KeyCode code)
        {
            return Main(ControlSettings.NextValueKey) == code || Main(ControlSettings.StyleKey) == code || Main(ControlSettings.SnapKey) == code
                || Main(TargetKeys.Lock) == code || Main(ControlSettings.ShapeKey) == code || Main(ControlSettings.PaintKey) == code;
        }

        private static KeyCode Main(ConfigEntry<KeyboardShortcut> entry) => entry != null ? entry.Value.MainKey : KeyCode.None;
    }

    /// <summary>Prefix on <c>KeyHints.Update</c>: the hard level key does not also switch the controller layout.</summary>
    [HarmonyPatch(typeof(KeyHints), nameof(KeyHints.Update))]
    public static class HardLevelKeyGuard
    {
        [HarmonyPrefix]
        public static bool Prefix(KeyHints __instance)
        {
            try
            {
                if (!GameKeyGuards.HardLevelKeyDown())
                    return true;
                __instance.UpdateHints();
                return false;
            }
            catch (System.Exception e)
            {
                BrushLog.Error("hard level key guard", e);
                return true;
            }
        }
    }

    /// <summary>Prefix and finalizer on <c>Player.Update</c>: the game's debug-mode hotkeys stay quiet on a brush key press.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    public static class DebugKeyGuard
    {
        private static bool suppressed;

        [HarmonyPrefix]
        public static void Prefix(Player __instance)
        {
            try
            {
                if (!Player.m_debugMode || __instance != Player.m_localPlayer || !GameKeyGuards.DebugKeyDown())
                    return;
                Player.m_debugMode = false;
                suppressed = true;
            }
            catch (System.Exception e)
            {
                BrushLog.Error("debug key guard", e);
            }
        }

        [HarmonyFinalizer]
        public static void Finalizer()
        {
            if (!suppressed)
                return;
            suppressed = false;
            Player.m_debugMode = true;
        }
    }
}
