using System.Collections.Generic;
using System.Globalization;
using EarthWright.Core;
using UnityEngine;

namespace EarthWright.Preview
{
    /// <summary>
    /// Draws EarthWright's HUD with IMGUI while a terrain tool is out and its build menu is closed: the text block next
    /// to the crosshair (out of reach, why the click would be refused, every module's <see cref="HudText"/> line, the
    /// cursor readout), the "settings locked by the server" badge above it, and the controls hints
    /// (<see cref="HintText"/>) at the bottom of the screen above the build bar. Long lines wrap within the screen.
    /// Nothing is drawn while a menu, the inventory, the map, the console or a text input is open, or while the player
    /// has hidden the game's HUD.
    /// </summary>
    internal static class HudOverlay
    {
        private const float Margin = 16f;
        private const float MinWidth = 240f;

        public static void OnGui()
        {
            if (Event.current.type != EventType.Repaint || !HudSettings.ShowHud.Value || !CanDraw())
                return;
            float scale = HudSettings.HudScale.Value;
            HudStyles.Ensure(scale);
            DrawBlock(scale);
        }

        public static bool CanDraw() => GeneralSettings.Active && LocalTool.InTerrainTool && !GameUiOpen();

        /// <summary>A game window that the HUD must not cover is open.</summary>
        public static bool GameUiOpen()
        {
            return InventoryGui.IsVisible() || Minimap.IsOpen() || global::Menu.IsVisible() || Console.IsVisible() || TextInput.IsVisible()
                || StoreGui.IsVisible() || Hud.IsUserHidden() || Hud.InRadial() || GameCamera.InFreeFly();
        }

        private static void DrawBlock(float scale)
        {
            float x = Screen.width * 0.5f + HudSettings.HudOffsetX.Value * scale;
            float y = Screen.height * 0.5f + HudSettings.HudOffsetY.Value * scale;
            float width = Mathf.Max(MinWidth, Screen.width - x - Margin);
            if (HudSettings.ShowLockedBadge.Value && Plugin.Synced != null && Plugin.Synced.IsLocked)
            {
                string badge = Language.Localize("$ew_preview_locked");
                float height = HudStyles.Badge.CalcHeight(new GUIContent(badge), width);
                HudStyles.Shadowed(new Rect(x, y - height, width, height), badge, HudStyles.Badge, HudStyles.BadgeShadow, scale);
            }
            foreach (string line in BlockLines())
                y += Draw(line, x, y, width, HudStyles.Line, HudStyles.Shadow, scale);
        }

        /// <summary>Draws one wrapped line and returns its height.</summary>
        private static float Draw(string text, float x, float y, float width, GUIStyle style, GUIStyle shadow, float scale)
        {
            float height = style.CalcHeight(new GUIContent(text), width);
            HudStyles.Shadowed(new Rect(x, y, width, height), text, style, shadow, scale);
            return height + scale;
        }

        private static List<string> BlockLines()
        {
            List<string> lines = new List<string>();
            List<string> module = HudText.Current();
            if (PreviewFrame.BrushVisible && PreviewFrame.Tone == PreviewTone.OutOfReach)
                lines.Add(string.Format(CultureInfo.InvariantCulture, "<color=#b8b8b8>{0} ({1:0.0} m / {2:0.0} m)</color>",
                    Language.Localize("$ew_preview_outofreach"), PreviewFrame.Distance, PreviewFrame.Reach));
            string reason = PreviewStatus.FirstReason;
            if (!string.IsNullOrEmpty(reason) && !module.Exists(line => line.Contains(reason)))
                lines.Add("<color=#ff5a4a>" + reason + "</color>");
            lines.AddRange(module);
            if (CursorReadout.Text != null)
                lines.Add("<color=#c8c8c8>" + CursorReadout.Text + "</color>");
            return lines;
        }
    }
}
