using System.Collections.Generic;
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

        private static string badge, badgeShadow;
        private static int badgeFor = -1;

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
                DrawBadge(x, y, width, scale);
            HudBlock.Refresh();
            List<string> lines = HudBlock.Lines;
            List<string> shadows = HudBlock.Shadows;
            for (int i = 0; i < lines.Count; i++)
                y += Draw(lines[i], shadows[i], x, y, width, scale);
        }

        private static void DrawBadge(float x, float y, float width, float scale)
        {
            if (badgeFor != Language.Version)
            {
                badgeFor = Language.Version;
                badge = Language.Localize("$ew_preview_locked");
                badgeShadow = HudStyles.Plain(badge);
            }
            float height = HudStyles.Height(HudStyles.Badge, badge, width);
            HudStyles.Shadowed(new Rect(x, y - height, width, height), badge, badgeShadow, HudStyles.Badge, HudStyles.BadgeShadow, scale);
        }

        /// <summary>Draws one wrapped line and returns its height.</summary>
        private static float Draw(string text, string shadowText, float x, float y, float width, float scale)
        {
            float height = HudStyles.Height(HudStyles.Line, text, width);
            HudStyles.Shadowed(new Rect(x, y, width, height), text, shadowText, HudStyles.Line, HudStyles.Shadow, scale);
            return height + scale;
        }
    }
}
