using EarthWright.Actions;
using HarmonyLib;

namespace EarthWright.Brush
{
    /// <summary>
    /// Limited gamepad control of the brush: while the gamepad modifier (the game's JoyAltKeys by default) is held, four
    /// buttons change the selected value, select the next value and cycle the shape. The game uses the D-pad with
    /// JoyAltKeys for camera zoom (up/down) and minimap zoom (left/right): the camera zoom is held still meanwhile
    /// (see <see cref="GamepadZoomPatch"/>); the small minimap still zooms one step on left/right. Local only.
    /// </summary>
    public static class GamepadInput
    {
        /// <summary>The gamepad is in use and the modifier is held with a terrain entry selected.</summary>
        public static bool ModifierHeld
        {
            get
            {
                if (GamepadSettings.Enabled == null || !GamepadSettings.Enabled.Value || !BrushState.Active || !ZInput.IsGamepadActive())
                    return false;
                return Held(GamepadSettings.Modifier.Value);
            }
        }

        public static void Update(ToolAction action, BrushValues values)
        {
            if (!ModifierHeld)
                return;
            if (Down(GamepadSettings.ValueUp.Value))
                ValueAdjuster.Step(action, values, 1, false, false);
            if (Down(GamepadSettings.ValueDown.Value))
                ValueAdjuster.Step(action, values, -1, false, false);
            if (Down(GamepadSettings.NextValue.Value))
                ValueSelector.Next(action, values);
            if (Down(GamepadSettings.Shape.Value) && !EntryKinds.IsPath(action))
                ShapeCycle.Next(values);
        }

        private static bool Held(string button) => !string.IsNullOrEmpty(button) && ZInput.GetButton(button.Trim());

        private static bool Down(string button) => !string.IsNullOrEmpty(button) && ZInput.GetButtonDown(button.Trim());
    }

    /// <summary>
    /// Prefix/postfix on <c>GameCamera.UpdateCamera</c>: while the gamepad modifier is held with a terrain entry
    /// selected, the camera distance is put back after the game's update, so the D-pad changes the brush instead of
    /// zooming (the game zooms with JoyAltKeys + D-pad up/down in place mode).
    /// </summary>
    [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.UpdateCamera))]
    public static class GamepadZoomPatch
    {
        [HarmonyPrefix]
        public static void Prefix(GameCamera __instance, out float __state)
        {
            __state = __instance.m_distance;
        }

        [HarmonyPostfix]
        public static void Postfix(GameCamera __instance, float __state)
        {
            try
            {
                if (GamepadInput.ModifierHeld && InputGate.Open)
                    __instance.m_distance = __state;
            }
            catch (System.Exception e)
            {
                BrushLog.Error("gamepad zoom block", e);
            }
        }
    }
}
