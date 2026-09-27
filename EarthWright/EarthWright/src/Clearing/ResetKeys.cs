using System.Linq;
using BepInEx.Configuration;
using EarthWright.Core;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Clearing
{
    /// <summary>
    /// The reset keys, only while a terrain tool is out with its menu closed and EarthWright is on for this player:
    /// U resets the brush area, Shift+U a circle of "Reset Around Radius" around the player. Both are free and go
    /// through the dispatcher like any stroke (wards and locks refuse them there).
    /// </summary>
    public static class ResetKeys
    {
        internal static void Initialize()
        {
            Ticker.OnUpdate("EarthWright reset keys", Update);
        }

        private static void Update()
        {
            if (!GeneralSettings.Active || !LocalTool.InTerrainTool || Keys.InventoryOpen || Minimap.IsOpen() || global::Menu.IsVisible())
                return;
            if (Keys.Pressed(ClearingSettings.ResetKey))
                ResetBrush();
            else if (Keys.Pressed(ClearingSettings.ResetAroundKey))
                ResetAround();
        }

        private static void ResetBrush()
        {
            if (!BrushAim.TryCenter(out Vector3 center))
            {
                Messages.Center(ClearingWords.NoAim);
                return;
            }
            Dispatcher.Submit(ResetEdits.Brush(center));
        }

        private static void ResetAround()
        {
            Player player = LocalTool.Player;
            float radius = ClearingSettings.ResetAroundRadius.Value;
            if (Dispatcher.Submit(ResetEdits.Around(player.transform.position, radius)))
                Messages.TopLeft(ClearingWords.Format(ClearingWords.ResetAround, ClearingWords.Metres(radius)));
        }
    }
}
